using AutoMapper;
using Common.Dto;
using Microsoft.AspNetCore.Http;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Service.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Car> _carRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;

        public OrderService(IRepository<Order> orderRepository, IRepository<Car> carRepository, IRepository<User> userRepository, IHttpContextAccessor httpContextAccessor, IEmailService emailService,IMapper mapper)
        {
            _orderRepository = orderRepository;
            _carRepository = carRepository;
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _mapper = mapper;
        }
        public async Task<OrderDto> Add(OrderDto item)
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = int.Parse(userIdStr ?? "0");
            if (currentUserId == 0) return null;
            var isCarBusy = IsCarBusy(item);
            if (isCarBusy) return null;
            var basePrice = CalculateOrderPrice(item);
            if (basePrice == 0) return null;
            Order newOrder = _mapper.Map<Order>(item);
            var car = _carRepository.GetById(item.CarId);
            var user = _userRepository.GetById(item.UserId);
            bool IsBlocked = user.IsBlocked || car.Status != Repository.Entities.CarStatus.Available;
            if (IsBlocked) { return null; }
            if (Enum.TryParse(item.PricingType, out PricingType pType))
            {
                newOrder.PricingType = pType;
            }
            else
            {
                newOrder.PricingType = PricingType.ByHour;
            }
            int age = DateTime.Now.Year - user.DateOfBirth.Year;
            if (age < 24) { newOrder.User.IsNewDriver = true; }
            newOrder.UserId = currentUserId;
            newOrder.BasePrice = basePrice;
            newOrder.Status = OrderStatus.Pending;
            newOrder.IsPaid = false;
            var saved = _orderRepository.Add(newOrder);
            if (saved != null)
            {
                try
                {
                    await _emailService.SendOrderConfirmationAsync(user.Email, saved.Id, car.Model);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Email failed for order {saved.Id}: {ex.Message}");
                }
            }
            return _mapper.Map<OrderDto>(saved);
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

        public async Task<bool>  FinishOrder(int orderId, int reportedMileage, int fuelTime)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null) return false;
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled) return false;
            var user = _userRepository.GetById(order.UserId);
            var car = _carRepository.GetById(order.CarId);

            // עדכון נתוני סיום נסיעה
            order.EndTime = DateTime.Now;
            order.EndMileage = reportedMileage;
            order.DistanceDrivenKm = reportedMileage - order.StartMileage;
            order.Status = OrderStatus.Completed;

            //  חישוב קנס איחור
            order.LateFee = order.EndTime > order.ExpectedEndTime
                ? (decimal)((order.EndTime.Value - order.ExpectedEndTime).TotalMinutes * 1)
                : 0;
            user.CompletedOrdersCount++;
            user.Rank = CalculateNewRank(user.CompletedOrdersCount);

            // בונוס תדלוק
            if (order.DidCustomerRefuel)
            {
                int effectiveFuelTimes = Math.Min(fuelTime, 2);
                decimal fuelBonus = effectiveFuelTimes * car.PricePerHour;
                user.AccountBalance += fuelBonus;
            }

            if (user.IsNewDriver)
            {
                order.TotalPrice += 50;
            }

            if (order.WantsInsuranceUpgrade)
            {
                if (order.PricingType == 0) // שעתי
                {
                    order.TotalPrice += car.Seats > 5 ? 10 : 5;
                }
                else // יומי
                {
                    order.TotalPrice += car.Seats > 5 ? 50 : 40;
                }
            }
            //  חישוב מחיר סופי ושקלול יתרה/הנחת דרגה
            order.TotalPrice += order.BasePrice + (decimal)(order.DistanceDrivenKm * 1.5) + order.LateFee - user.AccountBalance;

            decimal discount = GetRankDiscount(user.Rank);
            if (discount > 0)
            {
                order.TotalPrice -= discount * order.TotalPrice;
            }
            car.TotalOrdersCount++;
            car.Kilometers = car.Kilometers + (int)order.DistanceDrivenKm;
            car.Status = Repository.Entities.CarStatus.Available;
            user.AccountBalance = 0;

            _orderRepository.Update(order.Id, order);
            _carRepository.Update(car.Id, car);
            _userRepository.Update(user.Id, user);
            // שליחת קבלה סופית למייל המשתמש
            var orderDto = _mapper.Map<OrderDto>(order);
            orderDto.UserFullName = $"{user.FirstName} {user.LastName}";
            orderDto.CarModel = car.Model;
            try
            {
                await _emailService.SendFinalReceiptAsync(user.Email, orderDto);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("שגיאה בשליחת מייל: " + ex.Message);
            }
            return false;
        }
        private UserRank CalculateNewRank(int completedOrders)
        {
            if (completedOrders >= 50) return UserRank.PurpleBadge;
            if (completedOrders >= 30) return UserRank.Gold;
            if (completedOrders >= 20) return UserRank.Silver;
            if (completedOrders >= 10) return UserRank.Bronze;
            return UserRank.Regular;
        }
        public IEnumerable<OrderDto> GetAll()
        {
            var orders = _orderRepository.GetAll();
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public OrderDto? GetById(int id)
        {
            var order = _orderRepository.GetById(id);
            if (order == null) return null;
            return _mapper.Map<OrderDto>(order);
        }

        public bool Update(int id, OrderDto item)
        {
            var existing = _orderRepository.GetById(id);
            if (existing == null || !IsUserAuthorized(existing)) return false;

            _mapper.Map(item, existing);
            return _orderRepository.Update(id, existing);
        }
        //בדיקה האם הרכב תפוס בזמנים המבוקשים
        public bool IsCarBusy(OrderDto item)
        {
            return _orderRepository.GetAll().Any(o =>
                o.CarId == item.CarId &&
                o.Status != OrderStatus.Completed &&
                o.Status != OrderStatus.Canceled && 
                ((item.StartTime >= o.StartTime && item.StartTime < o.ExpectedEndTime) ||
                 (item.ExpectedEndTime > o.StartTime && item.ExpectedEndTime <= o.ExpectedEndTime)));
        }

        //חישוב מחיר הזמנה בסיסי לפי סוג תמחור וזמן השכרה
        public decimal CalculateOrderPrice(OrderDto item)
        {
            //חישוב מחיר בסיסי 
            var car = _carRepository.GetById(item.CarId);
            if (car == null) return 0;
            TimeSpan duration = item.ExpectedEndTime - item.StartTime;
            decimal calculatedPrice = 0;
            var pType = Enum.TryParse(item.PricingType, out PricingType type) ? type : PricingType.ByHour;
            if (pType == PricingType.ByDay)
            {
                int days = (int)Math.Ceiling(duration.TotalDays);
                calculatedPrice = days * car.PricePerDay;
            }
            else
            {
                calculatedPrice = (decimal)duration.TotalHours * car.PricePerHour;
            }
            return calculatedPrice;
        }
        //חישוב הנחה לפי דרגת משתמש
        private decimal GetRankDiscount(UserRank rank)
        {
            return rank switch
            {
                UserRank.PurpleBadge => 0.15m,
                UserRank.Gold => 0.10m,
                _ => 0m      //DEFULT             
            };
        }
        //סימולציה של נסיעה - להוסיף קילומטרים ולצרוך דלק בהתאם לאורך הנסיעה
        public bool SimulateDrive(int orderId, int kmToAdd)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status != OrderStatus.Active) return false;

            var car = _carRepository.GetById(order.CarId);

            // סימולציה של צריכת דלק: כל 10 ק"מ יורד 5% דלק
            int fuelConsumed = (kmToAdd / 10) * 5;
            car.FuelLevel = Math.Max(0, car.FuelLevel - fuelConsumed);
            car.Kilometers += kmToAdd;
            _carRepository.Update(car.Id, car);
            return true;
        }
        //פעולת פתחיחת רכב
        public bool UnlockCar(int orderId)
        {
      
            //  שליפת המשתמש המחובר מה-Token
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
            car.IsLocked = false;
            _carRepository.Update(car.Id, car);
            return true;
        }
        //פעולת נעילת רכב
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
        
        //עדכון התקדמות נסיעה - כל דקה להוסיף קילומטרים ולצרוך דלק
        public void UpdateTripProgress(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status != OrderStatus.Active) return;
            var car = _carRepository.GetById(order.CarId);
            if (car.IsLocked)
            {
                return;
            }
            Random rnd = new Random();
            if (rnd.Next(1, 11) > 2)
            {
                int kmThisMinute = rnd.Next(1, 3);
                car.Kilometers += kmThisMinute;
                int fuelConsumptionRate = 2;
                int totalFuelToDrop = kmThisMinute * fuelConsumptionRate;
                if (car.FuelLevel >= totalFuelToDrop)
                {
                    car.FuelLevel -= totalFuelToDrop;
                }
                else
                {
                    car.FuelLevel = 0;
                }
                _carRepository.Update(car.Id, car);
            }
        }
        //דיווח על מצב הרכב בתחילת נסיעה - אם הרכב מלוכלך או פגום, להטיל קנס על המשתמש הקודם
        public async Task<bool> ReportStartCondition(int orderId, bool isDirty, bool isDamaged, string comments)
        {
            // 1. שליפת ההזמנה הנוכחית
            var order = _orderRepository.GetById(orderId);
            if (order == null) return false;

            // 2. בדיקה אם יש דיווח על בעיה מצד הנהג שנכנס עכשיו לרכב
            if (isDirty || isDamaged)
            {
                // מחפשים את ההזמנה האחרונה שהסתיימה לפני זו, כדי למצוא את הנהג שהשאיר את הרכב כך
                var lastCompletedOrder = _orderRepository.GetAll()
                    .Where(o => o.CarId == order.CarId && o.Id != orderId)
                    .OrderByDescending(o => o.EndTime)
                    .FirstOrDefault();

                if (lastCompletedOrder != null)
                {
                    var previousUser = _userRepository.GetById(lastCompletedOrder.UserId);
                    if (previousUser != null)
                    {
                        decimal fine = 0;
                        string fineReason = "";

                        if (isDirty)
                        {
                            fine += 50; // קנס על השארת רכב מלוכלך
                            previousUser.DirtyReportsCount++;
                            fineReason += "אי-ניקיון הרכב; ";
                        }
                        if (isDamaged)
                        {
                            fine += 150; // קנס על נזק שלא דווח
                            fineReason += "נזק שלא דווח בסיום נסיעה; ";
                        }

                        // עדכון המאזן הכספי של הנהג הקודם
                        previousUser.AccountBalance -= fine;
                        _userRepository.Update(previousUser.Id, previousUser);

                        //  שליחת הודעה  במייל לנהג הקודם על החיוב 
                        try
                        {
                            await _emailService.SendFineNotificationAsync(previousUser.Email, fine, fineReason);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"שגיאה בשליחת מייל חיוב: {ex.Message}");
                        }
                    }
                }
            }

            order.ConditionNotes = comments;
            order.Status = OrderStatus.Active;
            _orderRepository.Update(order.Id, order);

            return  UnlockCar(orderId);
        }

        public int GetTotalOrdersCount()
        {
           return _orderRepository.GetAll().Count();
        }
        //קבלת הזמנה פעילה של משתמש - אם יש
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
           var orders=_orderRepository.GetAll()
                .Where(o=>o.StartTime.Date == date.Date);
           return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public bool MarkAsPaid(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || !IsUserAuthorized(order)) return false;

            order.IsPaid = true;
            return _orderRepository.Update(order.Id, order);
        }

        public bool CancelOrder(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || !IsUserAuthorized(order) || order.Status != OrderStatus.Pending)
                return false;

            order.Status = OrderStatus.Canceled;
            return _orderRepository.Update(orderId, order);
        }

        public IEnumerable<OrderDto> GetOrdersByDateRange(DateTime start, DateTime end)
        {
            var orders = _orderRepository.GetAll()
                .Where(o => o.StartTime >= start && o.EndTime <= end);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public IEnumerable<OrderDto> GetOrdersByUserEmail(string email)
        {
            var orders= _orderRepository.GetAll().Where(o => o.User.Email == email);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public IEnumerable<OrderDto> GetOrdersByCarNumber(string carNumber)
        {
            var orders = _orderRepository.GetAll().Where(o => o.Car.LicensePlate == carNumber);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public IEnumerable<OrderDto> GetOrdersByUserId(int userId)
        {
            var orders = _orderRepository.GetAll().Where(o => o.UserId == userId);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        //בדיקה האם המשתמש מורשה לבצע פעולה על הזמנה מסוימת (בעל ההזמנה או אדמין)
        private bool IsUserAuthorized(Order order)
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

            if (userIdClaim == null || order == null) return false;

            int currentUserId = int.Parse(userIdClaim);

            return userRole == "admin" || order.UserId == currentUserId;
        }
    }
}

