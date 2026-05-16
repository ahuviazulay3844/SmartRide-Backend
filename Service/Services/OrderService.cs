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
            // 1. זיהוי המשתמש
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = int.Parse(userIdStr ?? "0");
            if (currentUserId == 0) return null;
      
            // 2. תיקון זמנים לזמן מקומי
            item.StartTime = item.StartTime.ToLocalTime();
            item.ExpectedEndTime = item.ExpectedEndTime.ToLocalTime();
            if (IsTimeInShabbat(item.StartTime) || IsTimeInShabbat(item.ExpectedEndTime))
            {
                // מחזירים null או זורקים שגיאה כדי שההזמנה לא תתבצע
                return null;
            }
            // 3. שליפת ישויות ובדיקות חסימה
            var car = _carRepository.GetById(item.CarId);
            var user = _userRepository.GetById(currentUserId);

            if (car == null || user == null || user.IsBlocked) return null;

            // בדיקה אם הרכב מושבת לתיקונים או כבר בנסיעה פעילה
            if (car.NeedsMaintenance || car.Status == CarStatus.Occupied) return null;

            // בדיקת חפיפה בלו"ז (האם הרכב תפוס בזמנים האלו)
            if (IsCarBusy(item)) return null;

            // 4. חישוב מחיר כולל שדרוגים
            var basePrice = CalculateOrderPrice(item);
            if (basePrice == 0) return null;

            if (item.WantsInsuranceUpgrade)
            {
                basePrice += (item.TotalDays * 50) + (item.TotalHours * 3);
                if (item.TotalDays == 0 && item.TotalHours == 0) basePrice += 3;
            }
            // 5. יצירת הזמנה חדשה
            Order newOrder = _mapper.Map<Order>(item);
            newOrder.CarId = item.CarId;
            newOrder.UserId = currentUserId;
            newOrder.StartMileage = car.Kilometers;
            newOrder.BasePrice = basePrice;
            newOrder.Status = OrderStatus.Pending; // הזמנה ממתינה
            newOrder.IsPaid = false;
            newOrder.DistanceDrivenKm = 0;
            // 6. טיפול בסטטוס נהג חדש
            int age = DateTime.Now.Year - user.DateOfBirth.Year;
            if (DateTime.Now < user.DateOfBirth.AddYears(age)) age--;
            if (age < 24 && !user.IsNewDriver)
            {
                user.IsNewDriver = true;
                _userRepository.Update(user.Id, user);
            }
            if (Enum.TryParse(item.PricingType, out PricingType pType))
                newOrder.PricingType = pType;

            // 7. שמירה ועדכון סטטוס הרכב
            var saved = _orderRepository.Add(newOrder);
            if (saved != null)
            {
                // עדכון סטטוס הרכב ל-PartiallyBooked (יש הזמנה במערכת, אך הוא לא בנסיעה כרגע)
                if (car.Status == CarStatus.Available)
                {
                    car.Status = CarStatus.PartiallyBooked;
                    _carRepository.Update(car.Id, car);
                }

              //  try { await _emailService.SendOrderConfirmationAsync(user.Email, saved.Id, car.Model); }
              //  catch { /* כשל במייל לא עוצר הזמנה */ }
            }

            return _mapper.Map<OrderDto>(saved);
        }

        //public bool IsCarBusy(OrderDto item)
        //{
        //    // בודק אם קיימת הזמנה (Pending/Active) שחופפת בזמנים לרכב הספציפי
        //    return _orderRepository.GetAll().Any(o =>
        //        o.CarId == item.CarId &&
        //        (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Active) &&
        //        item.StartTime < o.ExpectedEndTime &&
        //        item.ExpectedEndTime > o.StartTime);
        //}
        public bool IsCarBusy(OrderDto item)
        {
            // 1. בדיקה אם הרכב פיזית בנסיעה כרגע
            bool isCurrentlyInUse = _orderRepository.GetAll().Any(o =>
                o.CarId == item.CarId &&
                o.Status == OrderStatus.Active);

            // אם מישהו בתוך הרכב עכשיו - הוא תפוס.
            if (isCurrentlyInUse && item.StartTime < DateTime.Now)
                return true;

            // 2. בדיקת חפיפה רגילה בלו"ז - בלי להוסיף 15 דקות "מתנה" בבדיקה!
            return _orderRepository.GetAll().Any(o =>
                o.CarId == item.CarId &&
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Active) &&
                item.StartTime < o.ExpectedEndTime && // התחלה לפני סיום קודמת
                item.ExpectedEndTime > o.StartTime);  // סיום אחרי התחלה קודמת
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
        public async Task<bool> UpdateStatusAsync(int id, Common.Dto.OrderStatus newStatus)
        {
            var existing = _orderRepository.GetById(id);
            if (existing == null) return false;

            // המרה בין ה-Enum של ה-DTO ל-Enum של ה-Entity
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

        //public async Task<bool>  FinishOrder(int orderId, int reportedMileage, int fuelTime)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null) return false;
        //    // שליפת הקילומטרים שנצברו במונה בשרת
        //    TripTracker.TotalAccumulatedKm.TryGetValue(orderId, out int trackedKm);
        //    if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled) return false;
        //    var user = _userRepository.GetById(order.UserId);
        //    var car = _carRepository.GetById(order.CarId);

        //    // עדכון נתוני סיום נסיעה
        //    order.EndTime = DateTime.Now;
        //    order.EndMileage = reportedMileage;
        //    order.DistanceDrivenKm = reportedMileage - order.StartMileage;
        //    order.Status = OrderStatus.Completed;

        //    //  חישוב קנס איחור
        //    order.LateFee = order.EndTime > order.ExpectedEndTime
        //        ? (decimal)((order.EndTime.Value - order.ExpectedEndTime).TotalMinutes * 1)
        //        : 0;
        //    user.CompletedOrdersCount++;
        //    user.Rank = CalculateNewRank(user.CompletedOrdersCount);

        //    // בונוס תדלוק
        //    if (order.DidCustomerRefuel)
        //    {
        //        int effectiveFuelTimes = Math.Min(fuelTime, 2);
        //        decimal fuelBonus = effectiveFuelTimes * car.PricePerHour;
        //        user.AccountBalance += fuelBonus;
        //    }

        //    if (user.IsNewDriver)
        //    {
        //        order.TotalPrice += 50;
        //    }

        //    if (order.WantsInsuranceUpgrade)
        //    {
        //        if (order.PricingType == 0) // שעתי
        //        {
        //            order.TotalPrice += car.Seats > 5 ? 10 : 5;
        //        }
        //        else // יומי
        //        {
        //            order.TotalPrice += car.Seats > 5 ? 50 : 40;
        //        }
        //    }
        //    //  חישוב מחיר סופי ושקלול יתרה/הנחת דרגה
        //    order.TotalPrice += order.BasePrice + (decimal)(order.DistanceDrivenKm * 1.5) + order.LateFee - user.AccountBalance;

        //    decimal discount = GetRankDiscount(user.Rank);
        //    if (discount > 0)
        //    {
        //        order.TotalPrice -= discount * order.TotalPrice;
        //    }
        //    car.TotalOrdersCount++;
        //    car.Kilometers = car.Kilometers + (int)order.DistanceDrivenKm;
        //    car.Status = Repository.Entities.CarStatus.Available;
        //    user.AccountBalance = 0;

        //    _orderRepository.Update(order.Id, order);
        //    _carRepository.Update(car.Id, car);
        //    _userRepository.Update(user.Id, user);
        //    // שליחת קבלה סופית למייל המשתמש
        //    var orderDto = _mapper.Map<OrderDto>(order);
        //    orderDto.UserFullName = $"{user.FirstName} {user.LastName}";
        //    orderDto.CarModel = car.Model;
        //    try
        //    {
        //        await _emailService.SendFinalReceiptAsync(user.Email, orderDto);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("שגיאה בשליחת מייל: " + ex.Message);
        //    }
        //    return false;
        //}
        public async Task<bool> FinishOrder(int orderId, int reportedMileage, int fuelTime)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled) return false;

            var user = _userRepository.GetById(order.UserId);
            var car = _carRepository.GetById(order.CarId);

            // 1. קביעת זמן הסיום
            order.EndTime = DateTime.Now;

            // 2. חישוב קנס איחור (פעם אחת בלבד!)
            if (order.EndTime.Value > order.ExpectedEndTime)
            {
                var lateMinutes = (order.EndTime.Value - order.ExpectedEndTime).TotalMinutes;
                order.LateFee += (decimal)(lateMinutes * 1);
            }
           
            // 3. עדכון נתונים
            // חשוב: נחשב את המרחק לפי ההפרש האמיתי במונה הרכב (EndMileage - StartMileage)
            // אם ה-reportedMileage שהגיע מהלקוח קטן מההתחלה, נשתמש ב-trackedKm שקיים ב-DB
            int actualEndMileage = Math.Max(reportedMileage, car.Kilometers);
            order.EndMileage = actualEndMileage;
            order.DistanceDrivenKm = actualEndMileage - order.StartMileage;
            order.Status = OrderStatus.Completed;

            // 4. בונוס תדלוק
            if (order.DidCustomerRefuel)
            {
                int effectiveFuelTimes = Math.Min(fuelTime, 2);
                decimal fuelBonus = effectiveFuelTimes * car.PricePerHour;
                user.AccountBalance += fuelBonus;
            }

            // 5. חישוב מחיר סופי
            decimal totalPriceCalculated = order.BasePrice + (decimal)(order.DistanceDrivenKm * 1.5) + order.LateFee;
            if (user.IsNewDriver) totalPriceCalculated += 50;

            // 6. הנחת דרגה
            decimal discount = GetRankDiscount(user.Rank);
            if (discount > 0) totalPriceCalculated -= (discount * totalPriceCalculated);

            totalPriceCalculated = Math.Max(0, totalPriceCalculated - order.DiscountAmount);
            //// 7. קיזוז יתרה ותשלום
            //order.TotalPrice = Math.Max(0, order.TotalPrice - user.AccountBalance);
            //order.IsPaid = true;
            //// 8. עדכון ישויות
            //user.CompletedOrdersCount++;
            //user.Rank = CalculateNewRank(user.CompletedOrdersCount);
            ////user.AccountBalance = 0;

            if (user.AccountBalance >= totalPriceCalculated)
            {
                user.AccountBalance -= totalPriceCalculated; // מורידים מהיתרה רק את מה ששולם
                order.TotalPrice = 0; // הכל שולם מהיתרה
            }
            else
            {
                order.TotalPrice = totalPriceCalculated - user.AccountBalance; // היתרה מקטינה את החוב
                user.AccountBalance = 0; // כל היתרה נוצלה
            }

            order.IsPaid = true;

            car.TotalOrdersCount++;
            car.Kilometers = actualEndMileage; // עדכון הקילומטראז' הסופי ברכב
            car.Status = Repository.Entities.CarStatus.Available;

            _orderRepository.Update(order.Id, order);
            _carRepository.Update(car.Id, car);
            _userRepository.Update(user.Id, user);

            // 9. שליחת מייל
            //try
            //{
            //    var orderDto = _mapper.Map<OrderDto>(order);
            //    orderDto.UserFullName = $"{user.FirstName} {user.LastName}";
            //    orderDto.CarModel = car.Model;
            //    await _emailService.SendFinalReceiptAsync(user.Email, orderDto);
            //}
            //catch { }

            return true;
        }
        private UserRank CalculateNewRank(int completedOrders)
        {
            if (completedOrders >= 50) return UserRank.PurpleBadge;
            if (completedOrders >= 30) return UserRank.Gold;
            if (completedOrders >= 20) return UserRank.Silver;
            if (completedOrders >= 10) return UserRank.Bronze;
            return UserRank.Regular;
        }



        //חישוב מחיר הזמנה בסיסי לפי סוג תמחור וזמן השכרה
        //public decimal CalculateOrderPrice(OrderDto item)
        //{
        //    //חישוב מחיר בסיסי 
        //    var car = _carRepository.GetById(item.CarId);
        //    if (car == null) return 0;
        //    TimeSpan duration = item.ExpectedEndTime - item.StartTime;
        //    decimal calculatedPrice = 0;
        //    var pType = Enum.TryParse(item.PricingType, out PricingType type) ? type : PricingType.ByHour;
        //    if (pType == PricingType.ByDay)
        //    {
        //        int days = (int)Math.Ceiling(duration.TotalDays);
        //        calculatedPrice = days * car.PricePerDay;
        //    }
        //    else
        //    {
        //        calculatedPrice = (decimal)duration.TotalHours * car.PricePerHour;
        //    }
        //    return calculatedPrice;
        //}
        public decimal CalculateOrderPrice(OrderDto item)
        {
            var car = _carRepository.GetById(item.CarId);
            if (car == null) return 0;

            // חישוב פשוט לפי השדות שקיבלנו מהלקוח
            decimal daysCost = item.TotalDays * car.PricePerDay;
            decimal hoursCost = item.TotalHours * car.PricePerHour;

            // מינימום שעה אחת אם הכל אפס
            if (item.TotalDays == 0 && item.TotalHours == 0) return car.PricePerHour;

            decimal total = daysCost + hoursCost;

            // הגנה: שעות עודפות לא יעלו יותר מיום שלם
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
        ////פעולת פתחיחת רכב
        //public bool UnlockCar(int orderId)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null) return false;

        //    // 1. עדכון זמן פתיחה ראשוני - קורה רק פעם אחת!
        //    if (order.ActualOpeningTime == null)
        //    {
        //        order.ActualOpeningTime = DateTime.Now;
        //        _orderRepository.Update(order.Id, order);
        //    }

        //    // 2. פתיחה פיזית של הרכב - תמיד קורה!
        //    var car = _carRepository.GetById(order.CarId);
        //    if (car != null)
        //    {
        //        car.IsLocked = false;
        //        _carRepository.Update(car.Id, car);
        //        return true; // תמיד מחזיר אמת אם הרכב נפתח
        //    }

        //    return false;
        //}
        public bool UnlockCar(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null) return false;

            // --- התיקון הקריטי: חסימת פתיחה אם הנהג הקודם עדיין בנסיעה (Active) ---
            bool isCarBusyByAnother = _orderRepository.GetAll()
                .Any(o => o.CarId == order.CarId && o.Id != orderId && o.Status == OrderStatus.Active);

            if (isCarBusyByAnother) return false; // השרת מסרב לפתוח את הרכב!
                                                  // -----------------------------------------------------------------------

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
        //public bool UnlockCar(int orderId)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null) return false;

        //    // הגנה: אם יש נהג אחר שכרגע בנסיעה (Active) על הרכב הזה - חסום פתיחה!
        //    bool isCarBusyByAnother = _orderRepository.GetAll()
        //        .Any(o => o.CarId == order.CarId && o.Id != orderId && o.Status == OrderStatus.Active);

        //    if (isCarBusyByAnother) return false; // חוסם פתיחה!

        //    if (order.ActualOpeningTime == null)
        //    {
        //        order.ActualOpeningTime = DateTime.Now;
        //        _orderRepository.Update(order.Id, order);
        //    }

        //    var car = _carRepository.GetById(order.CarId);
        //    if (car != null)
        //    {
        //        car.IsLocked = false;
        //        _carRepository.Update(car.Id, car);
        //        return true;
        //    }
        //    return false;
        //}

        //public bool UnlockCar(int orderId)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null) return false;

        //    // הגנה קריטית: האם יש מישהו אחר שכרגע בסטטוס "Active" (בנסיעה) על הרכב הזה?
        //    bool isCarBusyByAnother = _orderRepository.GetAll()
        //        .Any(o => o.CarId == order.CarId && o.Id != orderId && o.Status == OrderStatus.Active);

        //    if (isCarBusyByAnother) return false; // חוסם פתיחה!

        //    if (order.ActualOpeningTime == null)
        //    {
        //        order.ActualOpeningTime = DateTime.Now;
        //        _orderRepository.Update(order.Id, order);
        //    }

        //    var car = _carRepository.GetById(order.CarId);
        //    if (car != null)
        //    {
        //        car.IsLocked = false;
        //        _carRepository.Update(car.Id, car);
        //        return true;
        //    }
        //    return false;
        //}
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
        //public void UpdateTripProgress(int orderId)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null || order.Status != OrderStatus.Active) return;
        //    var car = _carRepository.GetById(order.CarId);
        //    if (car.IsLocked)
        //    {
        //        return;
        //    }
        //    Random rnd = new Random();
        //    if (rnd.Next(1, 11) > 2)
        //    {
        //        int kmThisMinute = rnd.Next(1, 3);
        //        car.Kilometers += kmThisMinute;
        //        int fuelConsumptionRate = 2;
        //        int totalFuelToDrop = kmThisMinute * fuelConsumptionRate;
        //        if (car.FuelLevel >= totalFuelToDrop)
        //        {
        //            car.FuelLevel -= totalFuelToDrop;
        //        }
        //        else
        //        {
        //            car.FuelLevel = 0;
        //        }
        //        _carRepository.Update(car.Id, car);
        //    }
        //}
        //public int UpdateTripProgress(int orderId)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null || order.Status != OrderStatus.Active) return 0;

        //    var car = _carRepository.GetById(order.CarId);

        //    // אם הרכב נעול - אין התקדמות
        //    if (car == null || car.IsLocked) return 0;

        //    Random rnd = new Random();
        //    // 80% סיכוי שהרכב נוסע (rnd > 2), 20% שהוא עומד (rnd <= 2)
        //    if (rnd.Next(1, 11) > 2)
        //    {
        //        int kmToAdd = rnd.Next(1, 3); // מגריל 1 או 2 ק"מ

        //        // ישיר ב-DB של ההזמנה ושל הרכב
        //        order.DistanceDrivenKm = (order.DistanceDrivenKm ?? 0) + kmToAdd;

        //        //  דלק  2% דלק לכל ק"מ
        //        car.FuelLevel = Math.Max(0, car.FuelLevel - (kmToAdd * 2));

        //        _orderRepository.Update(order.Id, order);
        //        _carRepository.Update(car.Id, car);

        //        return kmToAdd;
        //    }

        //    return 0; 
        //}
        public async Task<int> UpdateTripProgress(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status != OrderStatus.Active) return 0;

            if (DateTime.Now > order.ExpectedEndTime.AddMinutes(15))
            {
                // 2. חייב await! כדי לוודא שהשינויים נשמרים ב-DB לפני שה-Request נסגר
                await ProcessLateCustomerConflict(order.CarId);
            }

            var car = _carRepository.GetById(order.CarId);
            if (car == null || car.IsLocked) return 0;

            Random rnd = new Random();
            if (rnd.Next(1, 11) > 2)
            {
                int kmToAdd = rnd.Next(1, 3);


                // עדכון ישירות - וודאי שה-Repository משתמש ב-SaveChanges()
                order.DistanceDrivenKm = (order.DistanceDrivenKm ?? 0) + kmToAdd;
                car.Kilometers += kmToAdd;
                car.FuelLevel = Math.Max(0, car.FuelLevel - (kmToAdd * 2));

                // כאן הקריטי: אם ה-Repository שלך עושה Update בלי SaveChanges, תוסיפי אותו:
                _orderRepository.Update(order.Id, order);
                _carRepository.Update(car.Id, car);

                // אם ה-Repository הוא מעטפת ל-DbContext, וודאי שיש פה שמירה:
                // _context.SaveChanges(); 

                return kmToAdd;
            }
            return 0;
        }
        //////דיווח על מצב הרכב בתחילת נסיעה - אם הרכב מלוכלך או פגום, להטיל קנס על המשתמש הקודם
        //public async Task<bool> ReportStartCondition(int orderId, bool isDirty, bool isDamaged, string comments)
        //{
        //    // 1. שליפת ההזמנה הנוכחית
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null) return false;
        //    if (order.ActualOpeningTime == null)
        //    {
        //        order.ActualOpeningTime = DateTime.Now;
        //    }
        //    // 2. בדיקה אם יש דיווח על בעיה מצד הנהג שנכנס עכשיו לרכב
        //    if (isDirty || isDamaged)
        //    {
        //        // מחפשים את ההזמנה האחרונה שהסתיימה לפני זו, כדי למצוא את הנהג שהשאיר את הרכב כך
        //        var lastCompletedOrder = _orderRepository.GetAll()
        //            .Where(o => o.CarId == order.CarId && o.Id != orderId)
        //            .OrderByDescending(o => o.EndTime)
        //            .FirstOrDefault();

        //        if (lastCompletedOrder != null)
        //        {
        //            var previousUser = _userRepository.GetById(lastCompletedOrder.UserId);
        //            if (previousUser != null)
        //            {
        //                decimal fine = 0;
        //                string fineReason = "";

        //                if (isDirty)
        //                {
        //                    fine += 50; // קנס על השארת רכב מלוכלך
        //                    previousUser.DirtyReportsCount++;
        //                    fineReason += "אי-ניקיון הרכב; ";
        //                }
        //                if (isDamaged)
        //                {
        //                    fine += 150; // קנס על נזק שלא דווח
        //                    fineReason += "נזק שלא דווח בסיום נסיעה; ";
        //                }

        //                // עדכון המאזן הכספי של הנהג הקודם
        //                previousUser.AccountBalance -= fine;
        //                _userRepository.Update(previousUser.Id, previousUser);

        //                //  שליחת הודעה  במייל לנהג הקודם על החיוב 
        //                try
        //                {
        //                    await _emailService.SendFineNotificationAsync(previousUser.Email, fine, fineReason);
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine($"שגיאה בשליחת מייל חיוב: {ex.Message}");
        //                }
        //            }
        //        }
        //    }
        //    order.ConditionNotes = comments;
        //    order.Status = OrderStatus.Active;
        //    _orderRepository.Update(order.Id, order);
        //    return UnlockCar(orderId);
        //}
        public async Task<bool> ReportStartCondition(int orderId, CarInspectionDto dto)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null) return false;

            // 1. שמירת הדיווח בדאטה-בייס (טבלת CarInspections)
            var inspection = _mapper.Map<CarInspection>(dto);
            inspection.OrderId = orderId;
            inspection.CarId = order.CarId;
            inspection.UserId = order.UserId;
            inspection.InspectionDate = DateTime.Now;
            _inspectionRepository.Add(inspection);

            // 2. איתור ההזמנה האחרונה שהסתיימה עבור רכב זה (הנהג הקודם)
            var lastOrder = _orderRepository.GetAll()
                .Where(o => o.CarId == order.CarId && o.Status == OrderStatus.Completed && o.Id != orderId)
                .OrderByDescending(o => o.EndTime)
                .FirstOrDefault();

            // 3. הטלת קנסות על הנהג הקודם תוך התחשבות בביטוח שלו
            if (lastOrder != null)
            {
                var previousUser = _userRepository.GetById(lastOrder.UserId);
                if (previousUser != null)
                {
                    decimal fineAmount = 0;
                    string fineReasons = "";
                    bool wasSavedByInsurance = false;

                    // א. לכלוך - ביטוח בדרך כלל לא מכסה ניקיון
                    if (!dto.IsCleanInside || !dto.IsCleanOutside)
                    {
                        fineAmount += 45;
                        fineReasons += "השארת רכב מלוכלך; ";
                        previousUser.DirtyReportsCount++;
                    }

                    // ב. נזק חיצוני - נבדוק אם יש ביטול השתתפות עצמית
                    if (dto.AnyNewDamage)
                    {
                        if (lastOrder.WantsInsuranceUpgrade)
                        {
                            fineReasons += "נזק חיצוני (כוסה ע'י ביטול השתתפות) ";
                            wasSavedByInsurance = true;
                        }
                        else
                        {
                            fineAmount += 250;
                            fineReasons += "נזק חיצוני שלא דווח; ";
                        }
                    }

                    // ג. פנצ'ר - נבדוק אם יש ביטול השתתפות עצמית
                    if (dto.HasFlatTire)
                    {
                        if (lastOrder.WantsInsuranceUpgrade)
                        {
                            fineReasons += "פנצ'ר (כוסה ע'י ביטול השתתפות) ";
                            wasSavedByInsurance = true;
                        }
                        else
                        {
                            fineAmount += 100;
                            fineReasons += "פנצ'ר שלא דווח; ";
                        }
                    }

                    // עדכון המאזן רק אם יש קנס בפועל
                    if (fineAmount > 0)
                    {
                        previousUser.AccountBalance -= fineAmount;
                        _userRepository.Update(previousUser.Id, previousUser);

                        // שליחת מייל פירוט הקנס (וציון מה שכוסה ע"י הביטוח אם היה כזה)
                        try { await _emailService.SendFineNotificationAsync(previousUser.Email, fineAmount, fineReasons); }
                        catch { }
                    }
                    // אם לא היה קנס כי הביטוח כיסה הכל, אפשר לשלוח מייל "התראה/עדכון" ללא חיוב
                    else if (wasSavedByInsurance)
                    {
                        try { await _emailService.SendFineNotificationAsync(previousUser.Email, 0, fineReasons + " לכן לא חויבת."); }
                        catch { }
                    }
                }
            }

            // 4. בונוס לנהג הנוכחי על הדיווח
            var currentUser = _userRepository.GetById(order.UserId);
            if (currentUser != null)
            {
                currentUser.AccountBalance += 5; // זיכוי קבוע של 5 ש"ח כבונוס על עירנות ודיווח
                _userRepository.Update(currentUser.Id, currentUser);
            }

            // 5. עדכון סטטוס ההזמנה
            order.IsInspectionSubmitted = true;
            _orderRepository.Update(order.Id, order);

            return true;
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
            var orders = _orderRepository.GetAll()
                 .Where(o => o.StartTime.Date == date.Date);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<bool> MarkAsPaid(int orderId) // הפכנו ל-async כדי לשלוח מייל
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null) return false;

            // 1. עדכון הסטטוס
            order.IsPaid = true;
            var updated = _orderRepository.Update(orderId, order);

            if (updated)
            {
                // 2. שליפת נתונים לצורך המייל (חייבים את זה בשביל הקבלה המפורטת)
                var user = _userRepository.GetById(order.UserId);
                var car = _carRepository.GetById(order.CarId);

                var orderDto = _mapper.Map<OrderDto>(order);
                orderDto.UserFullName = $"{user.FirstName} {user.LastName}";
                orderDto.CarModel = car.Model;

                // 3. שליחת המייל!
                try
                {
                    await _emailService.SendFinalReceiptAsync(user.Email, orderDto);
                }
                catch { /* טיפול בשגיאה שקטה כדי שהתשלום לא ייכשל בגלל תקלת מייל */ }
            }

            return updated;
        }

        //public bool CancelOrder(int orderId)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null || !IsUserAuthorized(order) || order.Status != OrderStatus.Pending)
        //        return false;

        //    order.Status = OrderStatus.Canceled;
        //    return _orderRepository.Update(orderId, order);
        //}
        public bool CancelOrder(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            // מותר לבטל רק הזמנה שקיימת, שייכת למשתמש, ובסטטוס "ממתינה"
            if (order == null || !IsUserAuthorized(order) || order.Status != OrderStatus.Pending)
                return false;

            var car = _carRepository.GetById(order.CarId);
            var now = DateTime.Now;
            var timeToStart = order.StartTime - now;

            // --- חישוב הקנס לפי המדיניות שלך ---
            if (timeToStart.TotalHours >= 24)
            {
                order.TotalPrice = 0; // ביטול חינם
            }
            else if (timeToStart.TotalHours >= 2)
            {
                order.TotalPrice = car.PricePerHour; // קנס של שעה אחת
            }
            else
            {
                order.TotalPrice = order.BasePrice * 0.5m; // קנס 50% מההזמנה
            }

            order.Status = OrderStatus.Canceled;
            order.IsPaid = order.TotalPrice > 0; // אם יש קנס, הוא נחשב כחוב ששולם/יגבה

            // עדכון הרכב לזמין אם אין לו הזמנות אחרות (אופציונלי אך מומלץ)
            if (car != null && car.Status == Repository.Entities.CarStatus.PartiallyBooked)
            {
                bool hasOtherPending = _orderRepository.GetAll()
                    .Any(o => o.CarId == car.Id && o.Id != orderId && o.Status == OrderStatus.Pending);
                if (!hasOtherPending) car.Status = Repository.Entities.CarStatus.Available;
            }

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
            var orders = _orderRepository.GetAll().Where(o => o.User.Email == email);
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

        public object GetCarAvailabilityInfo(int carId)
        {
            throw new NotImplementedException();
        }

        //public bool IsUserOverlap(int userId, DateTime start, DateTime end)
        //{
        //    var startTime = start;
        //    var endTime = end;
        //    var now = DateTime.Now;

        //    // אנחנו בודקים האם קיימת הזמנה ש:
        //    // 1. שייכת למשתמש
        //    // 2. לא בוטלה ולא הסתיימה
        //    // 3. הסטטוס שלה הוא Active או Pending
        //    // 4. חשוב מאוד: היא עדיין רלוונטית (כלומר, זמן הסיום הצפוי שלה לא עבר לפני יותר משעה)
        //    bool hasTimeOverlap = _orderRepository.GetAll()
        //        .Any(o => o.UserId == userId &&
        //                  o.Status != OrderStatus.Canceled &&
        //                  o.Status != OrderStatus.Completed &&
        //                  o.StartTime < endTime &&
        //                  o.ExpectedEndTime > startTime);

        //    return hasTimeOverlap;
        //}
        public bool IsUserOverlap(int userId, DateTime start, DateTime end)
        {
            // 1. בדיקת חפיפה ביומן
            bool hasTimeOverlap = _orderRepository.GetAll()
                .Any(o => o.UserId == userId &&
                          o.Status != OrderStatus.Canceled &&
                          o.Status != OrderStatus.Completed &&
                          o.StartTime < end &&
                          o.ExpectedEndTime > start);

            if (hasTimeOverlap) return true;

            // 2. חסימת משתמש שמאחר כרגע עם רכב אקטיבי
            bool isCurrentlyLate = _orderRepository.GetAll()
                .Any(o => o.UserId == userId &&
                          o.Status == OrderStatus.Active &&
                          DateTime.Now > o.ExpectedEndTime);

            return isCurrentlyLate;
        }

        // 1. פונקציית הארכה לנהג המאחר (עבור המודאל שלו)
        public async Task<bool> RequestExtension(int orderId)
        {
    var order = _orderRepository.GetById(orderId);
    if (order == null || order.Status != OrderStatus.Active) return false;

    // 1. האם יש מישהו שמחכה לרכב הזה ממש עכשיו?
    var conflictOrder = _orderRepository.GetAll().FirstOrDefault(o =>
        o.CarId == order.CarId && o.Id != orderId &&
        o.Status == OrderStatus.Pending &&
        o.StartTime < order.ExpectedEndTime.AddHours(1));

    if (conflictOrder != null)
    {
        // 2. ניסיון אקטיבי להעביר את הלקוח הבא לרכב אחר
        bool reassignSuccess = await ProcessLateCustomerConflict(order.CarId);
        
        // אם לא הצלחנו למצוא חלופי, רק אז נחסום את ההארכה
        if (!reassignSuccess) return false;
    }

    // 3. אם אין קונפליקט או שהעברנו את המשתמש הבא בהצלחה - מאריכים
    var car = _carRepository.GetById(order.CarId);
    order.ExpectedEndTime = order.ExpectedEndTime.AddHours(1);
    order.TotalPrice += car.PricePerHour;

    return _orderRepository.Update(orderId, order);
}

        // 2. פונקציית הקונפליקט - מחפשת רכב חלופי ללקוח הבא (User B)
        //public async Task<bool> ProcessLateCustomerConflict(int carId)
        //{
        //    // 1. איתור הלקוח הבא שעלול להיפגע (שמזמין ב-15 דקות הקרובות)
        //    var nextOrder = _orderRepository.GetAll()
        //        .FirstOrDefault(o => o.CarId == carId &&
        //                             o.Status == OrderStatus.Pending &&
        //                             o.StartTime <= DateTime.Now.AddMinutes(15));

        //    // אם אין לקוח שמחכה, או שההזמנה שלו כבר טופלה - אין קונפליקט
        //    if (nextOrder == null || nextOrder.IsReassigned) return false;

        //    var originalCar = _carRepository.GetById(carId);

        //    // 2. חיפוש רכב חלופי פנוי באותו אזור
        //    var replacementCar = _carRepository.GetAll()
        //        .Where(c => c.Id != carId &&
        //                    c.RegionId == originalCar.RegionId &&
        //                    c.Status == Repository.Entities.CarStatus.Available &&
        //                    c.Seats >= originalCar.Seats)
        //        .AsEnumerable()
        //        .OrderBy(c => Math.Sqrt(Math.Pow(c.Latitude - originalCar.Latitude, 2) + Math.Pow(c.Longitude - originalCar.Longitude, 2)))
        //        .FirstOrDefault();

        //    if (replacementCar != null)
        //    {
        //        nextOrder.CarId = replacementCar.Id;
        //        nextOrder.Car.Model = replacementCar.Model; 
        //        nextOrder.IsReassigned = true;
        //        nextOrder.DiscountAmount = originalCar.PricePerHour;
        //        nextOrder.TotalPrice = Math.Max(0, nextOrder.BasePrice - nextOrder.DiscountAmount);

        //        _orderRepository.Update(nextOrder.Id, nextOrder);

        //        // 4. עדכון סטטוס הרכב החדש ל-Occupied (כי הוא כבר לא פנוי לאחרים)
        //        replacementCar.Status = Repository.Entities.CarStatus.Occupied;
        //        _carRepository.Update(replacementCar.Id, replacementCar);

        //        // 5. שליחת הודעה ללקוח
        //        try
        //        {
        //            var nextUser = _userRepository.GetById(nextOrder.UserId);
        //            await _emailService.SendOrderConfirmationAsync(nextUser.Email, nextOrder.Id,
        //                $"שלום, עקב עיכוב של הנהג הקודם, העברנו אותך לרכב חלופי מדגם {replacementCar.Model}. " +
        //                $"כפיצוי, קיבלת הנחה של ₪{nextOrder.DiscountAmount} על ההזמנה!");
        //        }
        //        catch { /* שגיאת מייל לא צריכה לעצור את התהליך */ }

        //        return true; // ההחלפה הצליחה!
        //    }
        //    else
        //    {
        //        // אם אין רכב חלופי והנהג הקודם עדיין לא נעל את הרכב (עדיין בנסיעה)
        //        if (!originalCar.IsLocked)
        //        {
        //            nextOrder.Status = OrderStatus.Canceled;
        //            _orderRepository.Update(nextOrder.Id, nextOrder);

        //            try
        //            {
        //                var nextUser = _userRepository.GetById(nextOrder.UserId);
        //                await _emailService.SendOrderConfirmationAsync(nextUser.Email, nextOrder.Id,
        //                    "מתנצלים, אך הזמנתך בוטלה עקב עיכוב של הנהג הקודם וחוסר ברכב חלופי.");
        //            }
        //            catch { }
        //        }
        //    }
        //    return false; // לא הצלחנו להחליף
        //}
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var d1 = lat1 * (Math.PI / 180.0);
            var num1 = lon1 * (Math.PI / 180.0);
            var d2 = lat2 * (Math.PI / 180.0);
            var num2 = lon2 * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6371 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3))); // מחזיר מרחק בקילומטרים
        }
        //public async Task<bool> ProcessLateCustomerConflict(int carId)
        //{
        //    // מוצא את הלקוח הבא שצריך את הרכב כרגע
        //    var nextOrder = _orderRepository.GetAll()
        //        .FirstOrDefault(o => o.CarId == carId && o.Status == OrderStatus.Pending &&
        //                             !o.HasConflict && DateTime.Now > o.StartTime.AddMinutes(15));

        //    if (nextOrder == null) return false;

        //    var originalCar = _carRepository.GetById(carId);

        //    // חישוב רכב חלופי הכי קרוב (לפחות אותו מספר מושבים)
        //    var replacementCar = _carRepository.GetAll()
        //        .Where(c => c.Id != carId && c.RegionId == originalCar.RegionId &&
        //                    c.Status == Repository.Entities.CarStatus.Available && c.Seats >= originalCar.Seats)
        //        .ToList().OrderBy(c => CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude))
        //        .FirstOrDefault();

        //    if (replacementCar != null)
        //    {
        //        nextOrder.HasConflict = true; // דגל שיקפיץ את הבחירה ב-React
        //        nextOrder.SuggestedReplacementCarId = replacementCar.Id; // שמירת ההצעה
        //        _orderRepository.Update(nextOrder.Id, nextOrder);
        //        return true;
        //    }
        //    return false;
        //}

        // א. חסימת פתיחת רכב תפוס
        //public bool UnlockCar(int orderId)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null) return false;

        //    // הגנה קריטית: אם יש נהג אחר שכרגע בסטטוס "Active" (בנסיעה) על הרכב הזה - חסום פתיחה!
        //    bool isCarBusyByAnother = _orderRepository.GetAll()
        //        .Any(o => o.CarId == order.CarId && o.Id != orderId && o.Status == OrderStatus.Active);

        //    if (isCarBusyByAnother) return false;

        //    if (order.ActualOpeningTime == null)
        //    {
        //        order.ActualOpeningTime = DateTime.Now;
        //        _orderRepository.Update(order.Id, order);
        //    }
        //    var car = _carRepository.GetById(order.CarId);
        //    if (car != null)
        //    {
        //        car.IsLocked = false;
        //        _carRepository.Update(car.Id, car);
        //        return true;
        //    }
        //    return false;
        //}

        // ב. פונקציית האישור של הלקוח הבא (User B) - נקראת מה-React
        public async Task<bool> ConfirmCarSwitch(int orderId, bool accept)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null) return false;

            if (accept)
            {
                // מחפשים רכב חלופי פנוי רק עכשיו - כשהלקוח אישר
                var originalCar = _carRepository.GetById(order.CarId);
                var replacementCar = _carRepository.GetAll()
                    .Where(c => c.Id != order.CarId && c.RegionId == originalCar.RegionId &&
                                c.Status == Repository.Entities.CarStatus.Available && c.Seats >= originalCar.Seats)
                    .ToList().OrderBy(c => CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude))
                    .FirstOrDefault();

                if (replacementCar != null)
                {
                    order.CarId = replacementCar.Id;
                    order.IsReassigned = true;
                    order.DiscountAmount = originalCar.PricePerHour; // פיצוי: שעה חינם
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
                // המשתמש סירב - ביטול הזמנה
                order.Status = OrderStatus.Canceled;
                _orderRepository.Update(orderId, order);
                return true;
            }
        }

        //// --- פונקציית האישור המעודכנת ---
        //public bool ConfirmReplacement(int orderId, bool accept)
        //{
        //    // 1. שליפת ההזמנה המקורית כולל כל הנתונים
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null || !order.HasConflict || !order.SuggestedReplacementCarId.HasValue) return false;

        //    if (accept)
        //    {
        //        // 2. שליפת הרכב החדש והישן מה-DB
        //        var newCarId = order.SuggestedReplacementCarId.Value;
        //        var newCar = _carRepository.GetById(newCarId);
        //        var oldCar = _carRepository.GetById(order.CarId);

        //        if (newCar == null) return false;

        //        // 3. החלפה פיזית של מזהה הרכב (זה מה שמשנה ב-DB!)
        //        order.CarId = newCarId;

        //        // ניקוי אובייקט ה-Navigation כדי למנוע בלבול של ה-ORM
        //        order.Car = null;

        //        // עדכון שם הדגם בתוך המחרוזת של ההזמנה (לצורך ה-UI)
        //        order.Car.Model = newCar.Model;

        //        // 4. עדכון סטטוסים
        //        order.Status = OrderStatus.Active; // הנסיעה מתחילה עכשיו!
        //        order.IsReassigned = true;
        //        order.HasConflict = false; // הקונפליקט נפתר
        //        order.SuggestedReplacementCarId = null;

        //        // 5. חישוב פיצוי (שעה חינם)
        //        decimal compensation = oldCar?.PricePerHour ?? 30;
        //        order.DiscountAmount = compensation;
        //        order.TotalPrice = Math.Max(0, order.BasePrice - order.DiscountAmount);

        //        // 6. שמירת ההזמנה המעודכנת
        //        _orderRepository.Update(orderId, order);

        //        // 7. עדכון סטטוס הרכב החדש ב-DB ל"תפוס"
        //        newCar.Status = CarStatus.Occupied;
        //        _carRepository.Update(newCar.Id, newCar);

        //        Console.WriteLine($"[DB Update] Order {orderId} moved to Car {newCarId} and set to Active.");
        //        return true;
        //    }
        //    else
        //    {
        //        // אם המשתמש ביטל - הופכים את ההזמנה למבוטלת ב-DB
        //        order.Status = OrderStatus.Canceled;
        //        order.HasConflict = false;
        //        _orderRepository.Update(orderId, order);
        //        return true;
        //    }
        //}
        // --- תוסיפי את פונקציית המרחק בתוך המחלקה OrderService ---

        // א. חסימת פתיחת רכב אם הנהג הקודם עוד לא סיים (Active)
        //public bool UnlockCar(int orderId)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null) return false;

        //    // בדיקה: האם מישהו אחר בנסיעה פעילה על הרכב הזה ממש עכשיו?
        //    bool isLockedByOther = _orderRepository.GetAll()
        //        .Any(o => o.CarId == order.CarId && o.Id != orderId && o.Status == OrderStatus.Active);

        //    if (isLockedByOther) return false; // השרת מסרב לפתוח את הרכב!


        //    // הגנה: אם יש נהג אחר שעדיין בנסיעה פעילה על הרכב הזה - אל תפתח!
        //    bool isCarCurrentlyActive = _orderRepository.GetAll()
        //        .Any(o => o.CarId == order.CarId && o.Id != orderId && o.Status == OrderStatus.Active);

        //    if (isCarCurrentlyActive) return false;

        //    if (order.ActualOpeningTime == null)
        //    {
        //        order.ActualOpeningTime = DateTime.Now;
        //        _orderRepository.Update(order.Id, order);
        //    }
        //    var car = _carRepository.GetById(order.CarId);
        //    if (car != null)
        //    {
        //        car.IsLocked = false;
        //        _carRepository.Update(car.Id, car);
        //        return true;
        //    }
        //    return false;
        //}

        //public async Task<bool> ProcessLateCustomerConflict(int carId)
        //{
        //    // 1. זיהוי המשתמש שנתקע (User B) - ישירות מה-DB
        //    var waitingOrder = _orderRepository.GetAll()
        //        .FirstOrDefault(o => o.CarId == carId && o.Status == OrderStatus.Pending);

        //    if (waitingOrder == null) return false;

        //    // 2. שליפת פרטי הרכב המקורי (רק מה שצריך)
        //    var originalCar = _carRepository.GetById(carId);
        //    if (originalCar == null) return false;

        //    // הגדרת זמנים עם ה-Buffer
        //    DateTime startWithBuffer = waitingOrder.StartTime.AddMinutes(-5);
        //    DateTime endWithBuffer = waitingOrder.ExpectedEndTime.AddMinutes(5);

        //    // 3. מציאת רכבים פוטנציאליים - סינון ראשוני ב-DB (IQueryable)
        //    // אנחנו מחפשים רכבים עם מספיק מושבים, שלא בטיפול, ושהם לא הרכב המקורי
        //    var potentialCarsQuery = _carRepository.GetAll()
        //        .Where(c => c.Id != carId &&
        //                    !c.NeedsMaintenance &&
        //                    c.Seats >= originalCar.Seats);

        //    // 4. סינון רכבים תפוסים ב-DB
        //    // במקום להביא את כל ההזמנות, נשאל את ה-DB: "מי מהרכבים האלו מופיע בהזמנה חופפת?"
        //    var busyCarIds = _orderRepository.GetAll()
        //        .Where(o => o.Status != OrderStatus.Completed &&
        //                    o.Status != OrderStatus.Canceled &&
        //                    o.StartTime < endWithBuffer &&
        //                    o.ExpectedEndTime > startWithBuffer)
        //        .Select(o => o.CarId)
        //        .Distinct()
        //        .ToList();

        //    // 5. שליפת הרכבים הפנויים בלבד לזיכרון
        //    var availableCars = potentialCarsQuery
        //        .Where(c => !busyCarIds.Contains(c.Id))
        //        .ToList();

        //    // 6. חישוב מרחק ודירוג בזיכרון (כי CalculateDistance היא פונקציית C#)
        //    var replacementCar = availableCars
        //        .OrderBy(c => CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude))
        //        .ThenByDescending(c => c.FuelLevel)
        //        .FirstOrDefault();

        //    // 7. עדכון הקונפליקט
        //    if (replacementCar != null)
        //    {
        //        waitingOrder.HasConflict = true;
        //        waitingOrder.SuggestedReplacementCarId = replacementCar.Id;
        //        waitingOrder.Car.Model = replacementCar.Model;

        //        // חשוב: אם יש לך Notification Service, זה הזמן לשלוח פוש למשתמש
        //        _orderRepository.Update(waitingOrder.Id, waitingOrder);

        //        Console.WriteLine($"[Success] Order {waitingOrder.Id} re-routed to {replacementCar.Model}");
        //        return true;
        //    }

        //    return false;
        //}
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

            // עדכון ל-100% בדאטה-בייס באמת
            car.FuelLevel = 100;
            _carRepository.Update(car.Id, car);

            order.DidCustomerRefuel = true;
            _orderRepository.Update(orderId, order);
            return true;
        }
        //public async Task<bool> ProcessLateCustomerConflict(int carId)
        //{
        //    // 1. זיהוי המשתמש שנתקע (User B) - רק אם הוא Pending ועוד לא קיבל הצעה
        //    var waitingOrder = _orderRepository.GetAll()
        //        .FirstOrDefault(o => o.CarId == carId && o.Status == OrderStatus.Pending && !o.HasConflict);

        //    if (waitingOrder == null) return false;

        //    // 2. שליפת פרטי הרכב המקורי
        //    var originalCar = _carRepository.GetById(carId);
        //    if (originalCar == null) return false;

        //    DateTime start = waitingOrder.StartTime;
        //    DateTime end = waitingOrder.ExpectedEndTime;
        //    int buffer = 15;

        //    // 3. מציאת IDs של רכבים שתפוסים ביומן בטווח הזמן הזה
        //    var busyCarIds = _orderRepository.GetAll()
        //        .Where(o => o.Status != OrderStatus.Canceled &&
        //                    o.Status != OrderStatus.Completed &&
        //                    o.StartTime < end.AddMinutes(buffer) &&
        //                    o.ExpectedEndTime > start.AddMinutes(-buffer))
        //        .Select(o => o.CarId).Distinct().ToList();

        //    // 4. חיפוש הרכב החלופי הכי מתאים (באזור, פנוי בלו"ז, מספיק מושבים)
        //    var replacementCar = _carRepository.GetAll()
        //        .Where(c => c.Id != carId &&
        //                    !c.NeedsMaintenance &&
        //                    c.RegionId == originalCar.RegionId &&
        //                    c.Seats >= originalCar.Seats)
        //        .ToList()
        //        .Where(c => !busyCarIds.Contains(c.Id))
        //        .OrderBy(c => CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude))
        //        .FirstOrDefault();

        //    if (replacementCar != null)
        //    {
        //        // 5. עדכון ההצעה ל-User B בלבד
        //        waitingOrder.HasConflict = true;
        //        waitingOrder.SuggestedReplacementCarId = replacementCar.Id;

        //        // --- השינוי הקריטי: שומרים את הנתונים בשדות ההצעה ולא משנים את הרכב המקורי! ---
        //        waitingOrder.SuggestedCarModel = replacementCar.Model;
        //        waitingOrder.SuggestedCarLocation = replacementCar.StartParking;

        //        _orderRepository.Update(waitingOrder.Id, waitingOrder);
        //        Console.WriteLine($"[Conflict] Offer sent to Order {waitingOrder.Id}: {replacementCar.Model}");
        //        return true;
        //    }
        //    return false;
        //}

        //public bool ConfirmReplacement(int orderId, bool accept)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null || !order.HasConflict || !order.SuggestedReplacementCarId.HasValue) return false;

        //    if (accept)
        //    {
        //        var newCar = _carRepository.GetById(order.SuggestedReplacementCarId.Value);
        //        var oldCar = _carRepository.GetById(order.CarId);

        //        // 1. החלפה פיזית של הרכב ב-Database
        //        order.CarId = newCar.Id;
        //        order.Car = null; // מנתקים את הקשר הישן כדי שה-ORM יטען את החדש

        //        // 2. אם יש שדה CarModel מחרוזתי בהזמנה (לצורך ה-UI), מעדכנים אותו
        //        // order.CarModel = newCar.Model; 

        //        // 3. שינוי סטטוס ל-Active (מתחילים נסיעה ועוצרים את ה-Worker)
        //        order.Status = OrderStatus.Active;
        //        order.HasConflict = false;
        //        order.IsReassigned = true;

        //        // 4. פיצוי (שעה חינם)
        //        decimal compensation = oldCar?.PricePerHour ?? 30;
        //        order.DiscountAmount = compensation;
        //        order.TotalPrice = Math.Max(0, order.BasePrice - order.DiscountAmount);

        //        _orderRepository.Update(orderId, order);

        //        // 5. עדכון סטטוס הרכב החדש ב-DB
        //        newCar.Status = Repository.Entities.CarStatus.Occupied;
        //        _carRepository.Update(newCar.Id, newCar);

        //        return true;
        //    }
        //    else
        //    {
        //        order.Status = OrderStatus.Canceled;
        //        order.HasConflict = false;
        //        _orderRepository.Update(orderId, order);
        //        return true;
        //    }
        //public async Task<bool> ProcessLateCustomerConflict(int carId)
        //{
        //    var waitingOrder = _orderRepository.GetAll()
        //        .FirstOrDefault(o => o.CarId == carId && o.Status == OrderStatus.Pending && !o.HasConflict);

        //    if (waitingOrder == null) return false;

        //    var originalCar = _carRepository.GetById(carId);
        //    if (originalCar == null) return false;

        //    waitingOrder.HasConflict = true;

        //    // 1. נמצא רכבים פנויים ביומן (באפר של 15 דק')
        //    int buffer = 15;
        //    var busyCarIds = _orderRepository.GetAll()
        //        .Where(o => o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed && o.Id != waitingOrder.Id &&
        //                    o.StartTime < waitingOrder.ExpectedEndTime.AddMinutes(buffer) &&
        //                    o.ExpectedEndTime.AddMinutes(buffer) > waitingOrder.StartTime)
        //        .Select(o => o.CarId).Distinct().ToList();

        //    // 2. חיפוש רכב חלופי לפי מרחק (רדיוס של 10 ק"מ) - מתעלמים מה-RegionId!
        //    var replacementCar = _carRepository.GetAll()
        //        .Where(c => c.Id != carId && !c.NeedsMaintenance && c.Seats >= originalCar.Seats)
        //        .ToList() // עוברים לזיכרון לחישוב מרחק
        //        .Where(c => !busyCarIds.Contains(c.Id) &&
        //                    CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude) <= 10)
        //        .OrderBy(c => CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude))
        //        .FirstOrDefault();

        //    if (replacementCar != null)
        //    {
        //        waitingOrder.SuggestedReplacementCarId = replacementCar.Id;
        //        waitingOrder.SuggestedCarModel = replacementCar.Model;
        //        waitingOrder.SuggestedCarLocation = replacementCar.StartParking;
        //        waitingOrder.SuggestedCarSeats = replacementCar.Seats;
        //        waitingOrder.DiscountAmount = originalCar.PricePerHour;
        //    }
        //    else
        //    {
        //        waitingOrder.SuggestedCarModel = "לא נמצא רכב חלופי זמין כרגע";
        //    }

        //    _orderRepository.Update(waitingOrder.Id, waitingOrder);
        //    return true;
        //}
        //public async Task<bool> ProcessLateCustomerConflict(int carId)
        //{
        //    // 1. מציאת ההזמנה הממתינה שמושפעת מהעיכוב
        //    // הוספנו בדיקה שהיא אכן Pending ועדיין לא קיבלה הצעה
        //    var waitingOrder = _orderRepository.GetAll()
        //        .FirstOrDefault(o => o.CarId == carId &&
        //                             o.Status == OrderStatus.Pending &&
        //                             !o.HasConflict);

        //    if (waitingOrder == null) return false;

        //    var originalCar = _carRepository.GetById(carId);
        //    if (originalCar == null) return false;

        //    // 2. חישוב רכבים תפוסים ביומן - תיקון קריטי!
        //    // הוספנו o.Id != waitingOrder.Id כדי שההזמנה לא תחסום את עצמה בטעות
        //    var busyCarIds = _orderRepository.GetAll()
        //        .Where(o => o.Id != waitingOrder.Id &&
        //                    o.Status != OrderStatus.Canceled &&
        //                    o.Status != OrderStatus.Completed &&
        //                    o.StartTime < waitingOrder.ExpectedEndTime &&
        //                    o.ExpectedEndTime > waitingOrder.StartTime)
        //        .Select(o => o.CarId)
        //        .Distinct()
        //        .ToList();

        //    // 3. מציאת הרכב הכי מתאים - הגמשת חיפוש
        //    var replacementCar = _carRepository.GetAll()
        //        .Where(c => c.Id != carId &&
        //                    !c.NeedsMaintenance &&
        //                    c.Status != Repository.Entities.CarStatus.Maintenance && // הגנה נוספת
        //                    c.Seats >= originalCar.Seats &&
        //                    c.RegionId == originalCar.RegionId) // וודא שיש רכבים באותו Region ב-DB
        //        .ToList()
        //        .Where(c => !busyCarIds.Contains(c.Id))
        //        .OrderBy(c => CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude))
        //        .FirstOrDefault();

        //    if (replacementCar != null)
        //    {
        //        // 4. עדכון ההצעה (רק עבור המשתמש הממתין!)
        //        waitingOrder.HasConflict = true;
        //        waitingOrder.SuggestedReplacementCarId = replacementCar.Id;
        //        waitingOrder.SuggestedCarModel = replacementCar.Model;
        //        waitingOrder.SuggestedCarLocation = replacementCar.StartParking;
        //        waitingOrder.SuggestedCarSeats = replacementCar.Seats;

        //        // בונוס: פיצוי אוטומטי על העיכוב (למשל הנחה של 30 ש"ח)
        //        waitingOrder.DiscountAmount = 30;

        //        _orderRepository.Update(waitingOrder.Id, waitingOrder);

        //        Console.WriteLine($"[Success] Found replacement car {replacementCar.Id} for Order {waitingOrder.Id}");
        //        return true;
        //    }

        //    // אם הגענו לכאן, המערכת לא מצאה רכב חלופי פנוי
        //    Console.WriteLine($"[Warning] No replacement found for car {carId} in Region {originalCar.RegionId}");
        //    return false;
        //}
        //public bool ConfirmReplacement(int orderId, bool accept)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null || !order.HasConflict) return false;

        //    if (accept)
        //    {
        //        var newCar = _carRepository.GetById(order.SuggestedReplacementCarId.Value);

        //        // החלפה פיזית ב-DB
        //        order.CarId = newCar.Id;
        //        order.Car = null; // ניקוי ל-ORM

        //        // עדכון סטטוס ל-Active (חשוב: זה מפסיק את הלולאה ב-Worker וב-React!)
        //        order.Status = OrderStatus.Active;
        //        order.HasConflict = false;
        //        order.IsReassigned = true;

        //        _orderRepository.Update(orderId, order);

        //        newCar.Status = Repository.Entities.CarStatus.Occupied;
        //        _carRepository.Update(newCar.Id, newCar);
        //        return true;
        //    }
        //    else
        //    {
        //        order.Status = OrderStatus.Canceled;
        //        _orderRepository.Update(orderId, order);
        //        return true;
        //    }
        //}
        //public bool ConfirmReplacement(int orderId, bool accept)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null || !order.HasConflict) return false;

        //    if (accept && order.SuggestedReplacementCarId.HasValue)
        //    {
        //        var newCarId = order.SuggestedReplacementCarId.Value;
        //        var newCar = _carRepository.GetById(newCarId);

        //        // 1. החלפה פיזית של הרכב וניתוק קשר קודם
        //        order.CarId = newCarId;
        //        order.Car = null;

        //        // 2. *** התיקון הקריטי: עדכון זמנים ***
        //        // כדי שהנסיעה לא תהיה "באיחור" מהרגע הראשון
        //        var duration = order.ExpectedEndTime - order.StartTime;
        //        order.StartTime = DateTime.Now;
        //        order.ExpectedEndTime = DateTime.Now.Add(duration);

        //        // 3. עדכון סטטוסים
        //        order.Status = OrderStatus.Active;
        //        order.HasConflict = false;
        //        order.IsReassigned = true;
        //        order.ActualOpeningTime = DateTime.Now;

        //        // 4. עדכון מחיר סופי
        //        order.TotalPrice = Math.Max(0, order.BasePrice - order.DiscountAmount);

        //        _orderRepository.Update(orderId, order);

        //        // 5. עדכון סטטוס הרכב החדש ב-DB
        //        if (newCar != null)
        //        {
        //            newCar.Status = Repository.Entities.CarStatus.Occupied;
        //            newCar.IsLocked = true;
        //            _carRepository.Update(newCar.Id, newCar);
        //        }
        //        return true;
        //    }
        //    else
        //    {
        //        order.Status = OrderStatus.Canceled;
        //        order.HasConflict = false;
        //        _orderRepository.Update(orderId, order);
        //        return true;
        //    }
        //}
        // פונקציית עזר פרטית בתוך ה-OrderService
        private bool IsTimeInShabbat(DateTime date)
        {
            var day = date.DayOfWeek; // Sunday = 0, Friday = 5, Saturday = 6
            var hour = date.Hour;

            // כניסת שבת: יום שישי החל מ-16:00
            if (day == DayOfWeek.Friday && hour >= 16) return true;

            // במהלך השבת: יום שבת עד השעה 20:00
            if (day == DayOfWeek.Saturday && hour < 20) return true;

            return false;
        }
        public bool ConfirmReplacement(int orderId, bool accept)
        {
            var oldOrder = _orderRepository.GetById(orderId);
            if (oldOrder == null || !oldOrder.HasConflict) return false;

            if (accept && oldOrder.SuggestedReplacementCarId.HasValue)
            {
                var newCar = _carRepository.GetById(oldOrder.SuggestedReplacementCarId.Value);
                if (newCar == null) return false;

                // --- שלב חדש: עדכון חוב ללקוח המאחר ---
                // מוצאים את ההזמנה של הלקוח שכרגע נמצא על הרכב המקורי ומאחר
                var lateOrder = _orderRepository.GetAll()
                    .FirstOrDefault(o => o.CarId == oldOrder.CarId && o.Status == OrderStatus.Active);

                if (lateOrder != null)
                {
                    // גביית מחיר השעה כקנס איחור (זה ה"איום" שלך)
                    decimal penalty = oldOrder.DiscountAmount;
                    lateOrder.LateFee += penalty;
                    lateOrder.TotalPrice += penalty;
                    _orderRepository.Update(lateOrder.Id, lateOrder);
                }
                // ---------------------------------------

                // 1. ביטול ההזמנה המקורית
                oldOrder.Status = OrderStatus.Canceled;
                oldOrder.HasConflict = false;
                _orderRepository.Update(orderId, oldOrder);

                // 2. יצירת הזמנה חדשה (Active) עם שעה ראשונה חינם (הנחה)
                var duration = oldOrder.ExpectedEndTime - oldOrder.StartTime;
                var newOrder = new Order
                {
                    UserId = oldOrder.UserId,
                    CarId = newCar.Id,
                    StartTime = DateTime.Now,
                    ExpectedEndTime = DateTime.Now.Add(duration),
                    Status = OrderStatus.Active,
                    ActualOpeningTime = DateTime.Now,
                    BasePrice = oldOrder.BasePrice,
                    DiscountAmount = oldOrder.DiscountAmount, // זו השעה חינם
                    TotalPrice = Math.Max(0, oldOrder.BasePrice - oldOrder.DiscountAmount),
                    IsReassigned = true,
                    PricingType = oldOrder.PricingType
                };

                _orderRepository.Add(newOrder);

                // 3. עדכון סטטוס הרכב החדש
                newCar.Status = CarStatus.Occupied;
                newCar.IsLocked = true;
                _carRepository.Update(newCar.Id, newCar);

                return true;
            }
            else
            {
                // סירוב - ביטול רגיל
                oldOrder.Status = OrderStatus.Canceled;
                oldOrder.HasConflict = false;
                _orderRepository.Update(orderId, oldOrder);
                return true;
            }
        }
        public async Task<bool> ProcessLateCustomerConflict(int carId)
        {
            var waitingOrder = _orderRepository.GetAll()
                .FirstOrDefault(o => o.CarId == carId && o.Status == OrderStatus.Pending && !o.HasConflict);

            if (waitingOrder == null) return false;

            var originalCar = _carRepository.GetById(carId);
            if (originalCar == null) return false;

            waitingOrder.HasConflict = true;

            // 1. זיהוי רכבים תפוסים ביומן
            int buffer = 15;
            var busyCarIds = _orderRepository.GetAll()
                .Where(o => o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed && o.Id != waitingOrder.Id &&
                            o.StartTime < waitingOrder.ExpectedEndTime.AddMinutes(buffer) &&
                            o.ExpectedEndTime.AddMinutes(buffer) > waitingOrder.StartTime)
                .Select(o => o.CarId).Distinct().ToList();

            // 2. חיפוש לפי מרחק אווירי בלבד (עד 10 ק"מ)
            var replacementCar = _carRepository.GetAll()
                .Where(c => c.Id != carId && !c.NeedsMaintenance && c.Seats >= originalCar.Seats)
                .ToList()
                .Where(c => !busyCarIds.Contains(c.Id))
                .Select(c => new { Car = c, Distance = CalculateDistance(originalCar.Latitude, originalCar.Longitude, c.Latitude, c.Longitude) })
                .Where(x => x.Distance <= 10) // הגבלה ל-10 קילומטר
                .OrderBy(x => x.Distance)
                .Select(x => x.Car)
                .FirstOrDefault();

            if (replacementCar != null)
            {
                waitingOrder.SuggestedReplacementCarId = replacementCar.Id;
                waitingOrder.SuggestedCarModel = replacementCar.Model;
                waitingOrder.SuggestedCarLocation = replacementCar.StartParking;
                waitingOrder.SuggestedCarSeats = replacementCar.Seats;
                waitingOrder.DiscountAmount = originalCar.PricePerHour; // פיצוי
            }
            else
            {
                waitingOrder.SuggestedCarModel = "לא נמצא רכב חלופי פנוי בקרבת מקום";
            }

            _orderRepository.Update(waitingOrder.Id, waitingOrder);
            return replacementCar != null;
        }
    }
    }


