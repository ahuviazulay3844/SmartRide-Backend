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

                try { await _emailService.SendOrderConfirmationAsync(user.Email, saved.Id, car.Model); }
                catch { /* כשל במייל לא עוצר הזמנה */ }
            }

            return _mapper.Map<OrderDto>(saved);
        }

        public bool IsCarBusy(OrderDto item)
        {
            // בודק אם קיימת הזמנה (Pending/Active) שחופפת בזמנים לרכב הספציפי
            return _orderRepository.GetAll().Any(o =>
                o.CarId == item.CarId &&
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Active) &&
                item.StartTime < o.ExpectedEndTime &&
                item.ExpectedEndTime > o.StartTime);
        }
        //{
        //    var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    int currentUserId = int.Parse(userIdStr ?? "0");
        //    if (currentUserId == 0) return null;
        //    var isCarBusy = IsCarBusy(item);
        //    if (isCarBusy) return null;
        //    var basePrice = CalculateOrderPrice(item);
        //    if (basePrice == 0) return null;
        //    Order newOrder = _mapper.Map<Order>(item);
        //    var car = _carRepository.GetById(item.CarId);
        //    var user = _userRepository.GetById(item.UserId);
        //    bool IsBlocked = user.IsBlocked || car.Status != Repository.Entities.CarStatus.Available;
        //    if (IsBlocked) { return null; }
        //    if (Enum.TryParse(item.PricingType, out PricingType pType))
        //    {
        //        newOrder.PricingType = pType;
        //    }
        //    else
        //    {
        //        newOrder.PricingType = PricingType.ByHour;
        //    }
        //    int age = DateTime.Now.Year - user.DateOfBirth.Year;
        //    if (age < 24) { newOrder.User.IsNewDriver = true; }
        //    newOrder.UserId = currentUserId;
        //    newOrder.BasePrice = basePrice;
        //    newOrder.Status = OrderStatus.Pending;
        //    newOrder.IsPaid = false;
        //    var saved = _orderRepository.Add(newOrder);
        //    if (saved != null)
        //    {
        //        try
        //        {
        //            await _emailService.SendOrderConfirmationAsync(user.Email, saved.Id, car.Model);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Email failed for order {saved.Id}: {ex.Message}");
        //        }
        //    }
        //    return _mapper.Map<OrderDto>(saved);
        //}
        //public async Task<OrderDto> Add(OrderDto item)
        //{
        //    // 1. בדיקה שהרכב קיים
        //    var car = _carRepository.GetById(item.CarId);
        //    if (car == null) return null;

        //    // 2. תיקון זמנים (UTC לזמן מקומי)
        //    item.StartTime = item.StartTime.ToLocalTime();
        //    item.ExpectedEndTime = item.ExpectedEndTime.ToLocalTime();

        //    // 3. בדיקת זמינות
        //    if (IsCarBusy(item)) return null;

        //    // 4. חישוב מחיר בסיסי (משתמש ב-TotalDays ו-TotalHours שהגיעו מה-React)
        //    var basePrice = CalculateOrderPrice(item);
        //    if (basePrice == 0) return null;

        //    // 5. חישוב עלות ביטוח (Waiver) לפי הנתונים שהגיעו
        //    if (item.WantsInsuranceUpgrade)
        //    {
        //        // 50 ש"ח ליום + 3 ש"ח לכל שעה עודפת
        //        basePrice += (item.TotalDays * 50) + (item.TotalHours * 3);

        //        // הגנת מינימום: אם ההזמנה קצרה משעה, נחייב לפחות 3 ש"ח ביטוח
        //        if (item.TotalDays == 0 && item.TotalHours == 0) basePrice += 3;
        //    }

        //    // 6. מיפוי ושמירה (ה-Automapper יעביר את TotalDays ו-TotalHours ל-Entity)
        //    Order newOrder = _mapper.Map<Order>(item);
        //    newOrder.Car = null;
        //    newOrder.CarId = item.CarId;
        //    newOrder.StartMileage = car.Kilometers;

        //    // 7. קישור למשתמש (זמני עד הלוגין)
        //    var user = _userRepository.GetAll().FirstOrDefault();
        //    if (user == null) return null;
        //    newOrder.UserId = user.Id;

        //    // 8. הגדרות סופיות
        //    newOrder.BasePrice = basePrice;
        //    newOrder.Status = OrderStatus.Pending;
        //    newOrder.IsPaid = false;

        //    var saved = _orderRepository.Add(newOrder);

        //    // 9. שליחת מייל
        //    if (saved != null)
        //    {
        //        try { await _emailService.SendOrderConfirmationAsync(user.Email, saved.Id, car.Model); }
        //        catch (Exception ex) { Console.WriteLine($"Email failed: {ex.Message}"); }
        //    }

        //    return _mapper.Map<OrderDto>(saved);
        //}
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
                order.LateFee = (decimal)(lateMinutes * 1);
            }
            else
            {
                order.LateFee = 0;
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
                car.FuelLevel = 100;
            }

            // 5. חישוב מחיר סופי
            order.TotalPrice = order.BasePrice + (decimal)(order.DistanceDrivenKm * 1.5) + order.LateFee;
            if (user.IsNewDriver) order.TotalPrice += 50;

            // 6. הנחת דרגה
            decimal discount = GetRankDiscount(user.Rank);
            if (discount > 0) order.TotalPrice -= (discount * order.TotalPrice);

            // 7. קיזוז יתרה ותשלום
            order.TotalPrice = Math.Max(0, order.TotalPrice - user.AccountBalance);
            order.IsPaid = true;

            // 8. עדכון ישויות
            user.CompletedOrdersCount++;
            user.Rank = CalculateNewRank(user.CompletedOrdersCount);
            user.AccountBalance = 0;

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

            if (order == null)
                return false;

            // פתיחה ראשונה בלבד
            if (order.ActualOpeningTime == null)
            {
                order.ActualOpeningTime = DateTime.Now;

                _orderRepository.Update(order.Id, order);
            }

            // פתיחת רכב תמיד
            var car = _carRepository.GetById(order.CarId);

            if (car != null)
            {
                car.IsLocked = false;

                _carRepository.Update(car.Id, car);

                return true;
            }

            return false;
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
        public int UpdateTripProgress(int orderId)
        {
            var order = _orderRepository.GetById(orderId);
            if (order == null || order.Status != OrderStatus.Active) return 0;

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

        //public async Task<bool> ProcessStartInspection(int orderId, CarInspectionDto reportDto)
        //{
        //    var order = _orderRepository.GetById(orderId);
        //    if (order == null) return false;

        //    // יצירת הישות לשמירה בדאטה-בייס (מיפוי ידני)
        //    var inspection = new CarInspection
        //    {
        //        OrderId = orderId,
        //        UserId = order.UserId,
        //        CarId = order.CarId,
        //        InspectionDate = DateTime.Now,
        //        IsCleanInside = reportDto.IsCleanInside,
        //        IsCleanOutside = reportDto.IsCleanOutside,
        //        IsAicConditionWorking = reportDto.IsAicConditionWorking,
        //        AnyNewDamage = reportDto.AnyNewDamage,
        //        DamageDescription = reportDto.DamageDescription
        //    };

        //    // שמירה לטבלת הבדיקות
        //    _inspectionRepository.Add(inspection);

        //    // עדכון סטטוס ההזמנה ל"פעיל"
        //    order.Status = (Repository.Entities.OrderStatus)1;
        //    _orderRepository.Update(orderId, order);

        //    // פתיחת נעילת הרכב
        //    var car = _carRepository.GetById(order.CarId);
        //    if (car != null)
        //    {
        //        car.IsLocked = false;
        //        _carRepository.Update(car.Id, car);
        //    }

        //    return true;
        //}
        // בתוך OrderService.cs
        //    public bool IsUserOverlap(int userId, DateTime start, DateTime end)
        //    {
        //        // המרה לזמן מקומי כדי להשוות מול נתוני ה-DB בצורה נכונה
        //        var startTime = start.ToLocalTime();
        //        var endTime = end.ToLocalTime();

        //        // הלוגיקה החדשה:
        //        // בודקים האם קיימת הזמנה כלשהי (לא משנה אם היא Pending או Active)
        //        // שמתנגשת ולו בדקה אחת עם חלון הזמן המבוקש.
        //        // אנחנו מוציאים מהכלל הזמנות שבוטלו או הסתיימו כי הן לא תופסות את הנהג פיזית.

        //        bool hasTimeOverlap = _orderRepository.GetAll()
        //            .Any(o => o.UserId == userId &&
        //                      o.Status != OrderStatus.Canceled &&
        //                      o.Status != OrderStatus.Completed &&
        //                      // תנאי החפיפה הלוגי:
        //                      // ההזמנה הקיימת מתחילה לפני שהחדשה מסתיימת
        //                      o.StartTime < endTime &&
        //                      // וההזמנה הקיימת מסתיימת אחרי שהחדשה מתחילה
        //                      o.ExpectedEndTime > startTime);

        //        // אם נמצאה חפיפה - הנהג "תפוס" ולא יכול להזמין רכב נוסף
        //        return hasTimeOverlap;
        //
        //    
        public bool IsUserOverlap(int userId, DateTime start, DateTime end)
        {
            var startTime = start;
            var endTime = end;
            var now = DateTime.Now;

            // אנחנו בודקים האם קיימת הזמנה ש:
            // 1. שייכת למשתמש
            // 2. לא בוטלה ולא הסתיימה
            // 3. הסטטוס שלה הוא Active או Pending
            // 4. חשוב מאוד: היא עדיין רלוונטית (כלומר, זמן הסיום הצפוי שלה לא עבר לפני יותר משעה)
            bool hasTimeOverlap = _orderRepository.GetAll()
                .Any(o => o.UserId == userId &&
                          o.Status != OrderStatus.Canceled &&
                          o.Status != OrderStatus.Completed &&
                          o.StartTime < endTime &&
                          o.ExpectedEndTime > startTime);

            return hasTimeOverlap;
        }
    }
    }

