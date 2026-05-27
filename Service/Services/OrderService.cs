using AutoMapper;
using Common.Dto;
using Microsoft.AspNetCore.Http;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CarStatus = Repository.Entities.CarStatus;
using OrderStatus = Repository.Entities.OrderStatus;

namespace Service.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Car> _carRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IRepository<CarInspection> _inspectionRepository;
        private readonly IMapper _mapper;

        public OrderService(IRepository<Order> orderRepository, IRepository<Car> carRepository, IRepository<User> userRepository, IHttpContextAccessor httpContextAccessor, IEmailService emailService, IRepository<CarInspection> inspectionRepository, IMapper mapper)
        {
            _orderRepository = orderRepository;
            _carRepository = carRepository;
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _inspectionRepository = inspectionRepository;
            _mapper = mapper;

        }
        public async Task<OrderDto> Add(OrderDto item)
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = int.Parse(userIdStr ?? "0");
            if (currentUserId == 0) return null;
          
            item.StartTime = item.StartTime.ToLocalTime();
            item.ExpectedEndTime = item.ExpectedEndTime.ToLocalTime();
            if (IsTimeInShabbat(item.StartTime) || IsTimeInShabbat(item.ExpectedEndTime))  return null; 
            if (IsUserOverlap(currentUserId, item.StartTime, item.ExpectedEndTime))  return null; 
            
            var car = _carRepository.GetById(item.CarId);
            var user = _userRepository.GetById(currentUserId);

            if (car == null || user == null || user.IsBlocked) return null;
            //?
            if (car.NeedsMaintenance) return null;

            // Is the vehicle occupied at these times?
            if (IsCarBusy(item)) return null;

            // price calculation - based on the fields we got from the client, without any hidden fees
            var basePrice = CalculateOrderPrice(item);
            if (basePrice == 0) return null;

            if (item.WantsInsuranceUpgrade)
            {
                basePrice += (item.TotalDays * 50) + (item.TotalHours * 3);
                if (item.TotalDays == 0 && item.TotalHours == 0) basePrice += 3;
            }
            //creating the new order 
            Order newOrder = _mapper.Map<Order>(item);
            newOrder.Id = 0;
            newOrder.CarId = item.CarId;
            newOrder.UserId = currentUserId;
            newOrder.StartMileage = car.Kilometers;
            newOrder.BasePrice = basePrice;
            newOrder.TotalPrice = basePrice;

            newOrder.Status = OrderStatus.Pending;
            newOrder.IsPaid = false;
            newOrder.DistanceDrivenKm = 0;
            // new driver
            int age = DateTime.Now.Year - user.DateOfBirth.Year;
            if (DateTime.Now < user.DateOfBirth.AddYears(age)) age--;
            if (age < 24 && !user.IsNewDriver)
            {
                user.IsNewDriver = true;
                _userRepository.Update(user.Id, user);
            }
            if (Enum.TryParse(item.PricingType, out PricingType pType))
                newOrder.PricingType = pType;

            var saved = _orderRepository.Add(newOrder);

            if (saved != null)
            {
                if (car.Status == CarStatus.Available)
                {
                    car.Status = CarStatus.PartiallyBooked;
                    _carRepository.Update(car.Id, car);
                }
                saved.Car = car;
                saved.User = user;
                try { await _emailService.SendOrderConfirmationAsync(user.Email, saved.Id, car.Model); }
                catch { }
                return MapToDetailedDto(saved);

            }
            return null;
        }
        public async Task<bool> FinishOrder(int orderId, int reportedMileage, int fuelTime)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled) return false;

            var user = _userRepository.GetById(order.UserId);
            var car = _carRepository.GetById(order.CarId);

            order.EndTime = DateTime.Now;

            //late 
            if (order.EndTime.Value > order.ExpectedEndTime)
            {
                var lateMinutes = (order.EndTime.Value - order.ExpectedEndTime).TotalMinutes;
                order.LateFee += (decimal)(lateMinutes * 1);
            }

           
            int actualEndMileage = Math.Max(reportedMileage, car.Kilometers);
            order.EndMileage = actualEndMileage;
            order.DistanceDrivenKm = actualEndMileage - order.StartMileage;
            order.Status = OrderStatus.Completed;

            //  Fueling bonus
            if (order.DidCustomerRefuel)
            {
                int effectiveFuelTimes = Math.Min(fuelTime, 2);
                decimal fuelBonus = effectiveFuelTimes * car.PricePerHour;
                user.AccountBalance += fuelBonus;
            }

            
            decimal totalPriceCalculated = order.BasePrice + (decimal)(order.DistanceDrivenKm * 1.5) + order.LateFee;
            if (user.IsNewDriver) totalPriceCalculated += 50;

            
            decimal discount = GetRankDiscount(user.Rank);
            if (discount > 0) totalPriceCalculated -= (discount * totalPriceCalculated);

            totalPriceCalculated = Math.Max(0, totalPriceCalculated - order.DiscountAmount);
          
           

            if (user.AccountBalance >= totalPriceCalculated)
            {
                user.AccountBalance -= totalPriceCalculated; 
                order.TotalPrice = 0; 
            }
            else
            {
                order.TotalPrice = totalPriceCalculated - user.AccountBalance;
                user.AccountBalance = 0;
            }

            order.IsPaid = true;
            car.TotalOrdersCount++;
            car.Kilometers = actualEndMileage;
            user.CompletedOrdersCount++;
            user.Rank = CalculateNewRank(user.CompletedOrdersCount);
            car.Status = Repository.Entities.CarStatus.Available;

            _orderRepository.Update(order.Id, order);
            _carRepository.Update(car.Id, car);
            _userRepository.Update(user.Id, user);

           // email
            try
            {
                var orderDto = _mapper.Map<OrderDto>(order);
                orderDto.UserFullName = $"{user.FirstName} {user.LastName}";
                orderDto.CarModel = car.Model;
                await _emailService.SendFinalReceiptAsync(user.Email, orderDto);
            }
            catch { }

            return true;
        }
        public async Task<int> UpdateTripProgress(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status != OrderStatus.Active) return 0;

            if (DateTime.Now > order.ExpectedEndTime.AddMinutes(15))
            {         
                await ProcessLateCustomerConflict(order.CarId);
            }

            var car = _carRepository.GetById(order.CarId);
            if (car == null || car.IsLocked) return 0;

            Random rnd = new Random();
            if (rnd.Next(1, 11) > 2)
            {
                int kmToAdd = rnd.Next(1, 3);

                order.DistanceDrivenKm = (order.DistanceDrivenKm ?? 0) + kmToAdd;
                car.Kilometers += kmToAdd;
                car.FuelLevel = Math.Max(0, car.FuelLevel - (kmToAdd * 2));

             
                _orderRepository.Update(order.Id, order);
                _carRepository.Update(car.Id, car);

             
                return kmToAdd;
            }
            return 0;
        }
        public bool SimulateDrive(int orderId, int kmToAdd)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status != OrderStatus.Active) return false;

            var car = _carRepository.GetById(order.CarId);

            //Fuel consumption simulation: every 10 km, fuel consumption decreases by 5%.
            int fuelConsumed = (kmToAdd / 10) * 5;
            car.FuelLevel = Math.Max(0, car.FuelLevel - fuelConsumed);
            car.Kilometers += kmToAdd;
            _carRepository.Update(car.Id, car);
            return true;
        }
        //Travel extension
        public async Task<bool> RequestExtension(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status != OrderStatus.Active) return false;    
            var newProposedEndTime = DateTime.Now.AddHours(1);

            if (DateTime.Now > order.ExpectedEndTime)
            {
                var minutesLate = (DateTime.Now - order.ExpectedEndTime).TotalMinutes;
                order.LateFee += (decimal)minutesLate;
                order.TotalPrice += (decimal)minutesLate;
            }

            // Checking conflicts against the *new* target time 
            var conflictOrder = _orderRepository.GetAll().FirstOrDefault(o =>
                o.CarId == order.CarId && o.Id != orderId &&
                o.Status == OrderStatus.Pending &&
                o.StartTime < newProposedEndTime);

            if (conflictOrder != null)
            {
                bool reassignSuccess = await ProcessLateCustomerConflict(order.CarId);
                if (!reassignSuccess) return false;
            }

            order.ExpectedEndTime = newProposedEndTime;

            var car = _carRepository.GetById(order.CarId);
            if (car != null)
            {
               
                order.BasePrice += car.PricePerHour;     
                order.TotalPrice += car.PricePerHour;
            }

            return _orderRepository.Update(orderId, order);
        }
        //finding replacement for late customer
        public async Task<bool> ProcessLateCustomerConflict(int carId)
        {
            var waitingOrder = _orderRepository.GetAll()
                .FirstOrDefault(o => o.CarId == carId && o.Status == OrderStatus.Pending && !o.HasConflict);

            if (waitingOrder == null) return false;

            var originalCar = _carRepository.GetById(carId);
            if (originalCar == null) return false;

            waitingOrder.HasConflict = true;
            waitingOrder.ConflictReason = "LateDriver";

            //  Identifying occupied vehicles in the logbook
            int buffer = 15;
            var busyCarIds = _orderRepository.GetAll()
                .Where(o => o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed && o.Id != waitingOrder.Id &&
                            o.StartTime < waitingOrder.ExpectedEndTime.AddMinutes(buffer) &&
                            o.ExpectedEndTime.AddMinutes(buffer) > waitingOrder.StartTime)
                .Select(o => o.CarId).Distinct().ToList();

            // Search for the nearest available car
            var replacementCar = _carRepository.GetAll()
                .Where(c => c.Id != carId && !c.NeedsMaintenance && c.Seats >= originalCar.Seats)
                .ToList()
                .Where(c => !busyCarIds.Contains(c.Id))
                .Select(c => new { Car = c, Distance = CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude) })
                .Where(x => x.Distance <= 10) 
                .OrderBy(x => x.Distance)
                .Select(x => x.Car)
                .FirstOrDefault();

            if (replacementCar != null)
            {
                waitingOrder.SuggestedReplacementCarId = replacementCar.Id;
                waitingOrder.SuggestedCarModel = replacementCar.Model;
                waitingOrder.SuggestedCarLocation = replacementCar.StartParking;
                waitingOrder.SuggestedCarSeats = replacementCar.Seats;
                waitingOrder.DiscountAmount = originalCar.PricePerHour;
                waitingOrder.SuggestedCarFuelLevel = replacementCar.FuelLevel;
            }
            else
            {
                waitingOrder.SuggestedCarModel = "לא נמצא רכב חלופי פנוי בקרבת מקום";
            }

            _orderRepository.Update(waitingOrder.Id, waitingOrder);
            return replacementCar != null;
        }
        //panzer
        public async Task<bool> ConfirmCarSwitch(int orderId, bool accept)
        {
            var order = _orderRepository.GetById(orderId);

            var originalCar = _carRepository.GetById(order.CarId);

            if (order == null) return false;

            if (accept)
            {
              
                int buffer = 15;
                var busyCarIds = _orderRepository.GetAll()
                    .Where(o => o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed && o.Id != orderId &&
                                o.StartTime < order.ExpectedEndTime.AddMinutes(buffer) &&
                                o.ExpectedEndTime.AddMinutes(buffer) > order.StartTime)
                    .Select(o => o.CarId).Distinct().ToList();

                var replacementCar = _carRepository.GetAll()
                    .Where(c => c.Id != order.CarId &&
                                c.RegionId == originalCar.RegionId &&
                                !busyCarIds.Contains(c.Id) && 
                                c.Seats >= originalCar.Seats)
                    .ToList()
                    .OrderBy(c => CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude))
                    .FirstOrDefault();

                if (replacementCar != null)
                {
                    order.CarId = replacementCar.Id;
                    order.IsReassigned = true;
                    order.ConflictReason = "FlatTire";
                    order.DiscountAmount = originalCar.PricePerHour; 

                    order.TotalPrice = Math.Max(0, order.BasePrice - order.DiscountAmount);
                    _orderRepository.Update(orderId, order);

                    replacementCar.Status = Repository.Entities.CarStatus.PartiallyBooked;
                    _carRepository.Update(replacementCar.Id, replacementCar);
                    return true;
                }
                return false;
            }
            else
            {
               
                order.Status = OrderStatus.Canceled;
                order.DiscountAmount = originalCar.PricePerHour;
                order.TotalPrice = Math.Max(0, order.BasePrice - order.DiscountAmount);
                _orderRepository.Update(orderId, order);
                return true;
            }
        }
       
        public bool ConfirmReplacement(int orderId, bool accept)
        {
            var oldOrder = _orderRepository.GetById(orderId);
            if (oldOrder == null || !oldOrder.HasConflict)
                return false;

           
            if (accept)
            {
               
                if (!oldOrder.SuggestedReplacementCarId.HasValue)
                {
                  
                    return false;
                }

                var newCarId = oldOrder.SuggestedReplacementCarId.Value;
                var newCar = _carRepository.GetById(newCarId);
                if (newCar == null) return false;

                oldOrder.Status = OrderStatus.Canceled;
                oldOrder.HasConflict = false;
                oldOrder.TotalPrice = 0; //free
                _orderRepository.Update(oldOrder.Id, oldOrder);

         
                var duration = oldOrder.ExpectedEndTime - oldOrder.StartTime;
                var now = DateTime.Now;

                var newOrder = new Order
                {
                    SuggestedCarFuelLevel = newCar.FuelLevel,
                    UserId = oldOrder.UserId,
                    CarId = newCar.Id,
                    StartTime = now,
                    ExpectedEndTime = now.Add(duration),
                    Status = OrderStatus.Active, 

                
                    StartMileage = newCar.Kilometers,
                    DistanceDrivenKm = 0,

                    BasePrice = oldOrder.BasePrice,
                    DiscountAmount = oldOrder.DiscountAmount,
                    TotalPrice = Math.Max(0, oldOrder.BasePrice - oldOrder.DiscountAmount),

                    PricingType = oldOrder.PricingType,
                    IsReassigned = false, 
                    HasConflict = false,
                    IsPaid = false,

                    // ניתוק אובייקטים למניעת DbUpdateException
                    Car = null,
                    User = null
                };

                _orderRepository.Add(newOrder);

                // ---update new car---
                newCar.Status = CarStatus.Occupied;
                newCar.IsLocked = true;
                _carRepository.Update(newCar.Id, newCar);

                return true;
            }

            // if not want (Cancel)
            else
            {
                oldOrder.Status = OrderStatus.Canceled;
                oldOrder.HasConflict = false;
                oldOrder.TotalPrice = 0;
                _orderRepository.Update(oldOrder.Id, oldOrder);

                var user = _userRepository.GetById(oldOrder.UserId);
                if (user != null)
                {
                    user.AccountBalance += 20; //pizue
                    _userRepository.Update(user.Id, user);
                }
                return true;
            }

        }

        public OrderDto MapToDetailedDto(Order order)
        {
            var dto = _mapper.Map<OrderDto>(order);
            dto.PriceBreakdown = new List<PriceBreakdownLine>();

            if (order.Car == null) order.Car = _carRepository.GetById(order.CarId);
            if (order.User == null) order.User = _userRepository.GetById(order.UserId);

            // base price
            dto.PriceBreakdown.Add(new PriceBreakdownLine { Label = "עלות השכרה בסיסית", Amount = order.BasePrice });

            // km cost
            decimal pricePerKm = order.Car?.PricePerKm ?? 1.5m;
            decimal distCost = (decimal)((order.DistanceDrivenKm ?? 0) * (double)pricePerKm);
            if (distCost > 0)
                dto.PriceBreakdown.Add(new PriceBreakdownLine { Label = $"מרחק נסיעה ({order.DistanceDrivenKm} ק\"מ)", Amount = distCost });

            //  new driver surcharge
            if (order.User != null && order.User.IsNewDriver)
                dto.PriceBreakdown.Add(new PriceBreakdownLine { Label = "תוספת נהג חדש (ביטוח)", Amount = 50 });

            // pricing type adjustment
            if (order.LateFee > 0)
                dto.PriceBreakdown.Add(new PriceBreakdownLine { Label = " ( דקות )דמי איחור בהחזרה", Amount = order.LateFee });

            // bunus for refueling
            if (order.DidCustomerRefuel)
                dto.PriceBreakdown.Add(new PriceBreakdownLine { Label = "בונוס מילוי דלק", Amount = 30, IsDiscount = true });

            // cupon discount
            if (order.DiscountAmount > 0)
                dto.PriceBreakdown.Add(new PriceBreakdownLine { Label = "הטבת פיצוי / הנחה", Amount = order.DiscountAmount, IsDiscount = true });

            // sum
            decimal subTotal = dto.PriceBreakdown.Sum(x => x.IsDiscount ? -x.Amount : x.Amount);

            if (subTotal > order.TotalPrice)
            {
                dto.PriceBreakdown.Add(new PriceBreakdownLine
                {
                    Label = "שולם באמצעות יתרה צבורה",
                    Amount = subTotal - order.TotalPrice,
                    IsDiscount = true
                });
            }
            else if (subTotal < order.TotalPrice && order.Status == OrderStatus.Completed)
            {
                dto.PriceBreakdown.Add(new PriceBreakdownLine
                {
                    Label = "חוב קודם / קנס (לכלוך/דיווח)",
                    Amount = order.TotalPrice - subTotal,
                    IsDiscount = false
                });
            }

            return dto;
        }


        public bool IsCarBusy(OrderDto item)
        {
            //Does the car have an active order?
            bool isCurrentlyInUse = _orderRepository.GetAll().Any(o =>
                o.CarId == item.CarId &&
                o.Status == OrderStatus.Active);

            // If anyone is inside the vehicle now, it is occupied.
            if (isCurrentlyInUse && item.StartTime < DateTime.Now)
                return true;

            // future 
            return _orderRepository.GetAll().Any(o =>
                o.CarId == item.CarId &&
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Active) &&
                item.StartTime < o.ExpectedEndTime && // start before the end of an existing order
                item.ExpectedEndTime > o.StartTime);  // end after the start of an existing order
        }
        public bool LockCar(int orderId)
        {
            var currentUserIdClaim = _httpContextAccessor.HttpContext?.User
              .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (currentUserIdClaim == null) return false;
            int currentUserId = int.Parse(currentUserIdClaim);
            var order = _orderRepository.GetById(orderId);

            if (order == null || order.Status != OrderStatus.Active || order.UserId != currentUserId)
            {
                return false;
            }
            var car = _carRepository.GetById(order.CarId);
            if (car == null) return false;
            car.IsLocked = true;
            _carRepository.Update(car.Id, car);
            return true;
        }
        public bool UnlockCar(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null) return false;

            // Blocking opening if the previous driver is still driving
            bool isCarBusyByAnother = _orderRepository.GetAll()
                .Any(o => o.CarId == order.CarId && o.Id != orderId && o.Status == OrderStatus.Active);

            if (isCarBusyByAnother) return false; 

            if (order.ActualOpeningTime == null)
            {
                order.ActualOpeningTime = DateTime.Now;
                _orderRepository.Update(order.Id, order);
            }
            var car = _carRepository.GetById(order.CarId);
            if (car != null)
            {
                car.IsLocked = false;
                _carRepository.Update(car.Id, car);
                return true;
            }
            return false;
        }
        public bool IsUserOverlap(int userId, DateTime start, DateTime end)
        {
            // Checking for overlap in the log
            bool hasTimeOverlap = _orderRepository.GetAll()
                .Any(o => o.UserId == userId &&
                          o.Status != OrderStatus.Canceled &&
                          o.Status != OrderStatus.Completed &&
                          o.StartTime < end &&
                          o.ExpectedEndTime > start);

            if (hasTimeOverlap) return true;

            bool isCurrentlyLate = _orderRepository.GetAll()
                .Any(o => o.UserId == userId &&
                          o.Status == OrderStatus.Active &&
                          DateTime.Now > o.ExpectedEndTime);

            return isCurrentlyLate;
        }

        public bool CancelOrder(int orderId)
        {
            var order = _orderRepository.GetById(orderId);

            if (order == null || !IsUserAuthorized(order) || order.Status != OrderStatus.Pending)
                return false;

            var car = _carRepository.GetById(order.CarId);
            var now = DateTime.Now;
            var timeToStart = order.StartTime - now;
     
            if (timeToStart.TotalHours >= 24)
            {
                order.TotalPrice = 0; 
            }
            else if (timeToStart.TotalHours >= 2)
            {
                order.TotalPrice = car.PricePerHour; 
            }
            else
            {
                order.TotalPrice = order.BasePrice * 0.5m; 
            }

            order.Status = OrderStatus.Canceled;
            order.IsPaid = order.TotalPrice > 0; 

            if (car != null && car.Status == Repository.Entities.CarStatus.PartiallyBooked)
            {
                bool hasOtherPending = _orderRepository.GetAll()
                    .Any(o => o.CarId == car.Id && o.Id != orderId && o.Status == OrderStatus.Pending);
                if (!hasOtherPending) car.Status = Repository.Entities.CarStatus.Available;
            }

            return _orderRepository.Update(orderId, order);
        }

        public bool Update(int id, OrderDto item)
        {
            var existing = _orderRepository.GetById(id);
            if (existing == null || !IsUserAuthorized(existing)) return false;

            _mapper.Map(item, existing);
            return _orderRepository.Update(id, existing);
        }
        public async Task<bool> UpdateStatusAsync(int id, Common.Dto.OrderStatus newStatus)
        {
            var existing = _orderRepository.GetById(id);
            if (existing == null) return false;

            existing.Status = (Repository.Entities.OrderStatus)newStatus;

            return _orderRepository.Update(id, existing);
        }
        public bool Delete(int id)
        {
            var order = _orderRepository.GetById(id);
            if (order == null) return false;
            var user = _httpContextAccessor.HttpContext?.User;
            var currentUserId = int.Parse(user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = user?.FindFirst(ClaimTypes.Role)?.Value;
            if (currentUserRole != "admin")
            {
                if (order.UserId != currentUserId || order.StartTime <= DateTime.Now)
                {
                    return false;
                }
            }
            if (order.Status != OrderStatus.Pending) return false;
            return _orderRepository.Delete(id);
        }

        public bool Exists(int id)
        {
            return _orderRepository.Exists(id);
        }
       
        private UserRank CalculateNewRank(int completedOrders)
        {
            if (completedOrders >= 50) return UserRank.PurpleBadge;
            if (completedOrders >= 30) return UserRank.Gold;
            if (completedOrders >= 20) return UserRank.Silver;
            if (completedOrders >= 10) return UserRank.Bronze;
            return UserRank.Regular;
        }

        public decimal CalculateOrderPrice(OrderDto item)
        {
            var car = _carRepository.GetById(item.CarId);
            if (car == null) return 0;
        
            decimal daysCost = item.TotalDays * car.PricePerDay;
            decimal hoursCost = item.TotalHours * car.PricePerHour;

            if (item.TotalDays == 0 && item.TotalHours == 0) return car.PricePerHour;

            decimal total = daysCost + hoursCost;

            //pricing policy: if the hours exceed the daily price, charge for an extra day instead. This encourages customers to book by the day rather than by the hour for longer rentals.
            if (item.TotalDays > 0 && hoursCost > car.PricePerDay)
            {
                total = (item.TotalDays + 1) * car.PricePerDay;
            }
            else if (item.TotalDays == 0 && total > car.PricePerDay)
            {
                total = car.PricePerDay;
            }

            return total;
        }
        private decimal GetRankDiscount(UserRank rank)
        {
            return rank switch
            {
                UserRank.PurpleBadge => 0.15m,
                UserRank.Gold => 0.10m,
                _ => 0m      //DEFULT             
            };
        }
     
        public int GetTotalOrdersCount()
        {
            return _orderRepository.GetAll().Count();
        }
        public OrderDto GetActiveOrder()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
           .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return null;
            }
            var activeOrder = _orderRepository.GetAll()
                .FirstOrDefault(o => o.UserId == currentUserId &&
                                    (o.Status == OrderStatus.Active || o.Status == OrderStatus.Pending));
            return _mapper.Map<OrderDto>(activeOrder);
        }

        public decimal GetTotalRevenue(DateTime? start, DateTime? end)
        {
            var completedOrders = _orderRepository.GetAll()
                .Where(o => o.Status == OrderStatus.Completed);
            if (start.HasValue)
            {
                completedOrders = completedOrders.Where(o => o.EndTime >= start.Value);
            }
            if (end.HasValue)
            {
                completedOrders = completedOrders.Where(o => o.EndTime <= end.Value);
            }
            return completedOrders.Sum(o => o.TotalPrice);
        }

        public IEnumerable<OrderDto> GetOrdersByDate(DateTime date)
        {
            var orders = _orderRepository.GetAll()
                 .Where(o => o.StartTime.Date == date.Date);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<bool> MarkAsPaid(int orderId) 
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null) return false;

            order.IsPaid = true;
            var updated = _orderRepository.Update(orderId, order);

            if (updated)
            {
                var user = _userRepository.GetById(order.UserId);
                var car = _carRepository.GetById(order.CarId);

                var orderDto = _mapper.Map<OrderDto>(order);
                orderDto.UserFullName = $"{user.FirstName} {user.LastName}";
                orderDto.CarModel = car.Model;

                try
                {
                    await _emailService.SendFinalReceiptAsync(user.Email, orderDto);
                }
                catch {  }
            }

            return updated;
        }

    
    

        public IEnumerable<OrderDto> GetOrdersByDateRange(DateTime start, DateTime end)
        {
            var orders = _orderRepository.GetAll()
                .Where(o => o.StartTime >= start && o.EndTime <= end);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public IEnumerable<OrderDto> GetOrdersByUserEmail(string email)
        {
            var orders = _orderRepository.GetAll().Where(o => o.User.Email == email);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public IEnumerable<OrderDto> GetOrdersByCarNumber(string carNumber)
        {
            var orders = _orderRepository.GetAll().Where(o => o.Car.LicensePlate == carNumber);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
  
        private bool IsUserAuthorized(Order order)
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

            if (userIdClaim == null || order == null) return false;

            int currentUserId = int.Parse(userIdClaim);

            return userRole == "admin" || order.UserId == currentUserId;
        }

      
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var d1 = lat1 * (Math.PI / 180.0);
            var num1 = lon1 * (Math.PI / 180.0);
            var d2 = lat2 * (Math.PI / 180.0);
            var num2 = lon2 * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6371 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3))); // return un kilo..
        }
      

        public bool FinalizeReplacement(int orderId, bool accept)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || !order.HasConflict || !order.SuggestedReplacementCarId.HasValue) return false;

            if (accept)
            {
                var originalCar = _carRepository.GetById(order.CarId);
                // המשתמש אישר - מעבירים רכב ונותנים שעה חינם
                order.CarId = order.SuggestedReplacementCarId.Value;
                order.IsReassigned = true;
                order.DiscountAmount = originalCar.PricePerHour;
                order.TotalPrice = Math.Max(0, order.BasePrice - order.DiscountAmount);
                order.HasConflict = false;
                _orderRepository.Update(orderId, order);
            }
            else
            {
                // ביטול
                order.Status = OrderStatus.Canceled;
                _orderRepository.Update(orderId, order);
            }
            return true;
        }

   
        public bool ReportRefuel(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status != OrderStatus.Active) return false;

            var car = _carRepository.GetById(order.CarId);
            if (car == null) return false;
          
            car.FuelLevel = 100;
            _carRepository.Update(car.Id, car);

            order.DidCustomerRefuel = true;
            _orderRepository.Update(orderId, order);
            return true;
        }
   
        private bool IsTimeInShabbat(DateTime date)
        {
            var day = date.DayOfWeek; // Sunday = 0, Friday = 5, Saturday = 6
            var hour = date.Hour;

            if (day == DayOfWeek.Friday && hour >= 16) return true;

            if (day == DayOfWeek.Saturday && hour < 20) return true;

            return false;
        }
      
    
        public OrderDto? GetById(int id)
        {
            var order = _orderRepository.GetById(id);
            if (order == null) return null;

            // טעינת ישויות קשורות
            order.User = _userRepository.GetById(order.UserId);
            order.Car = _carRepository.GetById(order.CarId);

            return MapToDetailedDto(order);
        }

       
        public IEnumerable<OrderDto> GetOrdersByUserId(int userId)
        {
            var orders = _orderRepository.GetAll()
                .Where(o => o.UserId == userId)
                .ToList();

            // חובה להעביר כל הזמנה דרך המיפוי המפורט לפני שהיא יוצאת לריאקט
            return orders.Select(o => {
                // טעינת ישויות חסרות לצורך חישוב הקילומטראז' בפירוט
                o.Car = _carRepository.GetById(o.CarId);
                o.User = _userRepository.GetById(o.UserId);
                return MapToDetailedDto(o);
            }).ToList();
        }

        public IEnumerable<OrderDto> GetAll()
        {
            var orders = _orderRepository.GetAll().ToList();
            return orders.Select(o => {
                o.Car = _carRepository.GetById(o.CarId);
                o.User = _userRepository.GetById(o.UserId);
                return MapToDetailedDto(o);
            }).ToList();
        }
        public async Task<bool> ReportStartCondition(int orderId, CarInspectionDto dto)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null) return false;

            // save inspection report
            var inspection = _mapper.Map<CarInspection>(dto);
            inspection.OrderId = order.Id;
            inspection.CarId = order.CarId;
            inspection.UserId = order.UserId;
            inspection.InspectionDate = DateTime.Now;        
            inspection.Car = null;
            inspection.User = null;
            inspection.Order = null;

            _inspectionRepository.Add(inspection);
            // find the last completed order for this car (excluding the current one) to check for potential fines
            var lastOrder = _orderRepository.GetAll()
                .Where(o => o.CarId == order.CarId && o.Status == OrderStatus.Completed && o.Id != orderId)
                .OrderByDescending(o => o.EndTime).FirstOrDefault();

            if (lastOrder != null)
            {
                var previousUser = _userRepository.GetById(lastOrder.UserId);
                if (previousUser != null)
                {
                    decimal fineAmount = 0;
                    string fineReasons = "";

                    if (!dto.IsCleanInside || !dto.IsCleanOutside) { fineAmount += 45; fineReasons += "לכלוך; "; }

                    if (dto.AnyNewDamage)
                    {
                        
                        if (!lastOrder.WantsInsuranceUpgrade) { fineAmount += 250; fineReasons += "נזק שלא דווח; "; }
                    }

                    if (dto.HasFlatTire)
                    {
                        
                        if (!lastOrder.WantsInsuranceUpgrade) { fineAmount += 100; fineReasons += "פנצ'ר שלא דווח; "; }
                    }

                    if (fineAmount > 0)
                    {
                        previousUser.AccountBalance -= fineAmount;
                        _userRepository.Update(previousUser.Id, previousUser);
                        try { await _emailService.SendFineNotificationAsync(previousUser.Email, fineAmount, fineReasons); } catch { }
                    }
                }
            }

           
            if (dto.HasFlatTire)
            {
                var car = _carRepository.GetById(order.CarId);
                if (car != null)
                {
                    car.NeedsMaintenance = true;
                    _carRepository.Update(car.Id, car);
                }

                int buffer = 15;
                var busyCarIds = _orderRepository.GetAll()
                    .Where(o => o.Id != order.Id && o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed &&
                                o.StartTime < order.ExpectedEndTime.AddMinutes(buffer) &&
                                o.ExpectedEndTime.AddMinutes(buffer) > order.StartTime)
                    .Select(o => o.CarId).Distinct().ToList();

                var replacementCar = _carRepository.GetAll()
                    .Where(c => c.Id != order.CarId &&
                                !c.NeedsMaintenance &&
                                c.Status == Repository.Entities.CarStatus.Available &&
                                c.FuelLevel >= 20 &&
                                c.Seats >= (car?.Seats ?? 0))
                    .ToList()
                    .Where(c => !busyCarIds.Contains(c.Id))
                    .OrderBy(c => CalculateDistance(car.Latitude, car.Longitude, c.Latitude, c.Longitude))
                    .ThenByDescending(c => c.FuelLevel)
                    .FirstOrDefault();

                if (replacementCar != null)
                {
                    order.HasConflict = true;
                    order.ConflictReason = "FlatTire";
                    order.SuggestedReplacementCarId = replacementCar.Id;
                    order.SuggestedCarModel = replacementCar.Model;
                    order.SuggestedCarLocation = replacementCar.StartParking;
                    order.SuggestedCarSeats = replacementCar.Seats;
                    order.DiscountAmount = car?.PricePerHour ?? 0;
                    order.SuggestedCarFuelLevel = replacementCar.FuelLevel;
                }
                else
                {
                    order.Status = OrderStatus.Canceled;
                    order.TotalPrice = 0;
                    var user = _userRepository.GetById(order.UserId);
                    if (user != null) { user.AccountBalance += 40; _userRepository.Update(user.Id, user); }
                }

                _orderRepository.Update(order.Id, order);
                return true;
            }

            var currentUser = _userRepository.GetById(order.UserId);
            if (currentUser != null) { currentUser.AccountBalance += 5; _userRepository.Update(currentUser.Id, currentUser); }

            order.IsInspectionSubmitted = true;
            _orderRepository.Update(order.Id, order);
            return true;
        }
    }
}
    


