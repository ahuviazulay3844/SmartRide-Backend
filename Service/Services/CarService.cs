//using AutoMapper;
//using Common.Dto;
//using Repository.Entities;
//using Repository.Interfaces;
//using Repository.Repositories;
//using Service.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using CarStatus = Common.Dto.CarStatus;
//using OrderStatus = Repository.Entities.OrderStatus;

//namespace Service.Services
//{
//    public class CarService : ICarService
//    {
//        private readonly IRepository<Car> _carRepository;
//        private readonly IRepository<Order> _orderRepository;
//        private readonly IMapper _mapper;

//        public CarService(IRepository<Car> carRepository, IRepository<Order> orderRepository, IMapper mapper)
//        {
//            _carRepository = carRepository;
//            _orderRepository = orderRepository;
//            _mapper = mapper;
//        }
//        public async Task<CarDto> Add(CarDto item)
//        {
//            var isLicenseTaken = _carRepository.GetAll().Any(c => c.LicensePlate == item.LicensePlate);
//            if (isLicenseTaken)
//            {
//                return null;
//            }
//            Car newCar = _mapper.Map<Car>(item);
//            var saved = _carRepository.Add(newCar);
//            return _mapper.Map<CarDto>(saved);
//        }

//        public bool Delete(int id)
//        {
//            if (!Exists(id)) return false;
//            return _carRepository.Delete(id);
//        }

//        public bool Exists(int id)
//        {
//            return _carRepository.Exists(id);
//        }

//        public IEnumerable<CarDto> GetAll()
//        {
//            var cars = _carRepository.GetAll();
//            return _mapper.Map<IEnumerable<CarDto>>(cars);
//        }

//        public CarDto? GetById(int id)
//        {
//            var c = _carRepository.GetById(id);
//            if (c == null) return null;
//            return _mapper.Map<CarDto>(c);

//        }

//        public bool Update(int id, CarDto item)
//        {
//            var existingCar = _carRepository.GetById(id);
//            if (existingCar == null) return false;
//            var isLicenseTaken = _carRepository.GetAll().Any(c => c.LicensePlate == item.LicensePlate && c.Id != id);
//            if (isLicenseTaken)
//            {
//                return false;
//            }
//            _mapper.Map(item, existingCar);
//            return _carRepository.Update(id, existingCar);
//        }
//        //
//        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon)
//        //{
//        //    var sortedCars = _carRepository.GetAll()
//        //        .AsEnumerable() // עוברים לזיכרון כדי לבצע את חישוב המרחק
//        //        .OrderBy(c => CalculateDistance(userLat, userLon, c.Latitude, c.Longitude));
//        //        return _mapper.Map<IEnumerable<CarDto>>(sortedCars);
//        //}
//        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon)
//        //{
//        //    // 1. שליפת כל הרכבים מהמסד
//        //    var cars = _carRepository.GetAll().ToList();
//        //    var carDtos = cars.Select(c =>
//        //    {
//        //        var dto = _mapper.Map<CarDto>(c);
//        //        dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);
//        //        return dto;
//        //    })
//        //    // 3. עכשיו המיון בטוח - הוא מתבצע על הנתון שיוחזר ל-React
//        //    .OrderBy(d => d.Distance)
//        //    .ToList();

//        //    return carDtos;
//        //}
//        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
//        //{
//        //    // 1. שליפת כל הרכבים מהמסד
//        //    var cars = _carRepository.GetAll().ToList();

//        //    var carDtos = cars.Select(c =>
//        //    {
//        //        var dto = _mapper.Map<CarDto>(c);

//        //        // 2. חישוב המרחק (הלוגיקה הקיימת שלך)
//        //        dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

//        //        // 3. עדכון הסטטוס בזמן אמת - רק אם נשלחו זמנים מה-React
//        //        if (start.HasValue && end.HasValue)
//        //        {
//        //            string statusDescription = GetDetailedAvailabilityStatus(c.Id, start.Value, end.Value);

//        //            // המרה לערך מספרי שה-React שלך יודע לעכל (לפי הקומפוננטה שראינו קודם)
//        //          dto.Status = statusDescription switch
//        //          {
//        //              "פנוי" => CarStatus.Available,
//        //              "פנוי חלקית" => CarStatus.PartiallyBooked,
//        //              "תפוס" => CarStatus.Occupied,
//        //              "לא זמין" => CarStatus.Occupied, // או להוסיף NotAvailable ל-Enum במידת הצורך
//        //              _ => CarStatus.Occupied
//        //          };
//        //        }
//        //        return dto;
//        //    })
//        //    // 4. מיון לפי המרחק (שומר על הדרישה המקורית שלך)
//        //    .OrderBy(d => d.Distance)
//        //    .ToList();

//        //    return carDtos;
//        //}
//        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
//        //{
//        //    var cars = _carRepository.GetAll().ToList();

//        //    var carDtos = cars.Select(c =>
//        //    {
//        //        var dto = _mapper.Map<CarDto>(c);
//        //        dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

//        //        if (start.HasValue && end.HasValue)
//        //        {
//        //            string statusDescription = GetDetailedAvailabilityStatus(c.Id, start.Value, end.Value);

//        //            dto.Status = statusDescription switch
//        //            {
//        //                "פנוי" => CarStatus.Available,
//        //                "פנוי חלקית" => CarStatus.PartiallyBooked,
//        //                "תפוס" => CarStatus.Occupied,
//        //                "לא זמין" => CarStatus.Occupied,
//        //                _ => CarStatus.Occupied
//        //            };
//        //        }
//        //        return dto;
//        //    }).ToList(); 

//        //    return carDtos.OrderBy(d => d.Distance).ToList();
//        //}
//        // נוסחת Haversine לחישוב מרחק אווירי בקילומטרים
//        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
//        {
//            var d1 = lat1 * (Math.PI / 180.0);
//            var num1 = lon1 * (Math.PI / 180.0);
//            var d2 = lat2 * (Math.PI / 180.0);
//            var num2 = (lon2 * (Math.PI / 180.0)) - num1;
//            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
//                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

//            return 6371.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
//        }

//        public bool SendToMaintenance(int carId)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car != null)
//            {
//                car.NeedsMaintenance = true;
//                _carRepository.Update(carId, car);
//                return true;
//            }
//            return false;
//        }

//        public bool ReleaseFromMaintenance(int carId)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car != null)
//            {
//                car.NeedsMaintenance = false;
//                _carRepository.Update(carId, car);
//                return true;
//            }
//            return false;
//        }

//        public IEnumerable<CarDto> GetVehiclesNeedingFuel()
//        {
//            var carsNeedingFuel = _carRepository.GetAll().Where(c => c.FuelLevel < 15).ToList();
//            return _mapper.Map<IEnumerable<CarDto>>(carsNeedingFuel);
//        }

//        public bool UpdateFuelLevel(int carId, int newLevel)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car != null)
//            {
//                car.FuelLevel = newLevel;
//                _carRepository.Update(carId, car);
//                return true;
//            }
//            return false;
//        }

//        public IEnumerable<CarDto> GetAvailableCarsByRegion(int regionId)
//        {
//            return GetAvailableCars(DateTime.Now, DateTime.Now.AddMinutes(30), regionId);
//        }

//        public bool UpdateMileage(int carId, int newMileage)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car == null) return false;
//            car.Kilometers = newMileage;
//            return _carRepository.Update(carId, car);
//        }

//        public bool UpdateStatus(int carId, string status)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car == null) return false;
//            if (!Enum.TryParse<Common.Dto.CarStatus>(status, ignoreCase: true, out var parsedStatus))
//            {
//                return false;
//            }
//            car.Status = (Repository.Entities.CarStatus)parsedStatus;
//            return _carRepository.Update(carId, car);
//        }
//        public bool IsCarFitForRoad(int carId)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car == null) return false;
//            return !car.NeedsMaintenance && car.FuelLevel >= 15;
//        }
//        public IEnumerable<CarDto> GetAvailableCars(DateTime start, DateTime end, int regionId)
//        {
//            double tripDurationHours = (end - start).TotalHours;

//            // 1. שיפור ביצועים: שליפת כל ההזמנות שחופפות לזמן המבוקש פעם אחת בלבד
//            var busyCarIds = _orderRepository.GetAll()
//                .Where(order => order.Status != OrderStatus.Completed &&
//                                start < order.ExpectedEndTime &&
//                                end > order.StartTime)
//                .Select(order => order.CarId)
//                .ToList();

//            // 2. סינון הרכבים על בסיס הרשימה שהכנו
//            var availableCars = _carRepository.GetAll()
//         .Where(c => c.RegionId == regionId && !c.NeedsMaintenance)
//         .AsEnumerable()
//         .Where(car => !busyCarIds.Contains(car.Id) &&
//                       !(tripDurationHours > 4 && car.FuelLevel < 40) &&
//                       !(car.FuelLevel < 15))
//         .ToList();

//            // כאן השינוי: מיפוי ידני שדורס את הסטטוס לפי הזמן המבוקש
//            return availableCars.Select(car =>
//            {
//                var dto = _mapper.Map<CarDto>(car);

//                // כיוון שהרכב עבר את הסינון, אנחנו יודעים שהוא פנוי בטווח הזה
//                // לכן נגדיר לו ידנית סטטוס "פנוי" כדי להבטיח עקביות
//                dto.Status = Common.Dto.CarStatus.Available;

//                return dto;
//            });
//        }

//        public string CheckCarSuitability(int carId, DateTime start, DateTime end)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car == null) return "רכב לא נמצא";
//            if (car.NeedsMaintenance) return "הרכב נמצא בתחזוקה";
//            if (car.FuelLevel < 15) return "אין מספיק דלק לנסיעה מינימלית";

//            double hours = (end - start).TotalHours;
//            if (hours > 4 && car.FuelLevel < 40) return "לנסיעה ארוכה נדרש לפחות 40% דלק";

//            // בדיקה שנוספה: האם הרכב פנוי בזמן הזה?
//            bool isBusy = _orderRepository.GetAll().Any(order =>
//                order.CarId == carId &&
//                order.Status != OrderStatus.Completed &&
//                start < order.ExpectedEndTime &&
//                end > order.StartTime);

//            if (isBusy) return "הרכב כבר מוזמן לזמן המבוקש";

//            return "OK";
//        }
//        public IEnumerable<CarDto> GetAllPopularCars(int count = 5)
//        {
//            var popularCars = _carRepository.GetAll()
//                .OrderByDescending(c => c.TotalOrdersCount)
//                .Take(count)
//                .ToList();
//            return _mapper.Map<IEnumerable<CarDto>>(popularCars);
//        }

//        public IEnumerable<CarDto> GetByStatus(string status)
//        {
//            if (!Enum.TryParse<Repository.Entities.CarStatus>(status, true, out var parsedStatus))
//                return Enumerable.Empty<CarDto>();

//            var cars = _carRepository.GetAll().Where(c => c.Status == parsedStatus).ToList();
//            return _mapper.Map<IEnumerable<CarDto>>(cars);
//        }
//        //public object GetCarAvailabilityInfo(int carId)
//        //{
//        //    var car = _carRepository.GetById(carId);
//        //    var now = DateTime.Now;
//        //    var hourFromNow = now.AddHours(1);

//        //    // שליפת הזמנות רלוונטיות לרכב זה שאינן מבוטלות
//        //    var orders = _orderRepository.GetAll()
//        //        .Where(o => o.CarId == carId && o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed)
//        //        .ToList();

//        //    // בדיקה 1: האם תפוס ממש עכשיו?
//        //    var currentOrder = orders.FirstOrDefault(o => now >= o.StartTime && now < o.ExpectedEndTime);

//        //    // בדיקה 2: האם יש הזמנה שמתחילה בשעה הקרובה?
//        //    var upcomingOrder = orders.FirstOrDefault(o => o.StartTime > now && o.StartTime <= hourFromNow);

//        //    string displayStatus = "פנוי";
//        //    if (currentOrder != null) displayStatus = "תפוס";
//        //    else if (upcomingOrder != null) displayStatus = "פנוי חלקית";

//        //    return new
//        //    {
//        //        CarId = car.Id,
//        //        Model = car.Model,
//        //        Address = car.StartParking,
//        //        Status = displayStatus,
//        //        NextOrderStart = upcomingOrder?.StartTime,
//        //        ImageUrl = car.ImageUrl
//        //    };
//        //}
//        public object GetCarAvailabilityInfo(int carId)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car == null) return null; // הגנה בסיסית

//            var now = DateTime.Now;
//            int bufferMinutes = 15;

//            // אנחנו רוצים לבדוק זמינות לשעה הקרובה, אבל לוקחים בחשבון שצריך 15 דק' נקיות בסוף
//            var hourFromNowWithBuffer = now.AddHours(1).AddMinutes(bufferMinutes);

//            var orders = _orderRepository.GetAll()
//                .Where(o => o.CarId == carId &&
//                            o.Status != OrderStatus.Canceled &&
//                            o.Status != OrderStatus.Completed)
//                .ToList();

//            // בדיקה 1: האם תפוס עכשיו (כולל ה-Buffer של מי שסיים)
//            var currentOrder = orders.FirstOrDefault(o =>
//                now >= o.StartTime &&
//                now < o.ExpectedEndTime.AddMinutes(bufferMinutes));

//            // בדיקה 2: האם יש הזמנה שמתחילה בקרוב ומפריעה לנסיעה של שעה?
//            // הוספנו OrderBy כדי לוודא שזו ההזמנה הכי קרובה
//            var upcomingOrder = orders
//                .Where(o => o.StartTime > now && o.StartTime <= hourFromNowWithBuffer)
//                .OrderBy(o => o.StartTime)
//                .FirstOrDefault();

//            string displayStatus = "פנוי";
//            if (currentOrder != null)
//                displayStatus = "תפוס";
//            else if (upcomingOrder != null)
//                displayStatus = "פנוי חלקית";

//            return new
//            {
//                CarId = car.Id,
//                Model = car.Model,
//                Address = car.StartParking,
//                Status = displayStatus,
//                // הזמן המקסימלי האמיתי שהלקוח יכול להחזיק את הרכב בלי להפריע לבא בתור
//                NextOrderStart = upcomingOrder?.StartTime.AddMinutes(-bufferMinutes),
//                ImageUrl = car.ImageUrl
//            };
//        }
//        public bool UpdateLockStatus(int carId, bool isLocked)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car == null) return false;
//            car.IsLocked = isLocked;

//            car.LastLockTime = DateTime.Now;

//            return _carRepository.Update(carId, car);
//        }
//        //public CarDto GetDetailedAvailability(int carId, DateTime requestedStart, DateTime requestedEnd)
//        //{
//        //    var orders = _orderRepository.GetOrdersByCarId(carId)
//        //        .Where(o => o.Status != OrderStatus.Cancelled && o.StartTime.Date == requestedStart.Date)
//        //        .OrderBy(o => o.StartTime)
//        //        .ToList();

//        //    // בדיקה אם יש חפיפה כלשהי
//        //    var conflictingOrder = orders.FirstOrDefault(o => requestedStart < o.EndTime && requestedEnd > o.StartTime);

//        //    if (conflictingOrder == null)
//        //    {
//        //        return new CarAvailabilityDto { Status = 0, Message = "פנוי לגמרי" };
//        //    }

//        //    // אם יש חפיפה, נבדוק אם אפשר להציע זמן קרוב
//        //    return new CarAvailabilityDto
//        //    {
//        //        Status = 1,
//        //        Message = "פנוי חלקית",
//        //        NextAvailableSlot = orders.FirstOrDefault(o => o.StartTime >= requestedEnd)?.StartTime,
//        //        MaxEndTimePossible = orders.FirstOrDefault(o => o.StartTime > requestedStart)?.StartTime
//        //    };
//        //}
//        // בתוך CarService.cs
//        //public string GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd)
//        //{
//        //    var car = _carRepository.GetById(carId);
//        //    if (car == null || car.NeedsMaintenance) return "לא זמין";

//        //    // שינוי קריטי: סינון הזמנות שרלוונטיות אך ורק לטווח המבוקש
//        //    var orders = _orderRepository.GetAll()
//        //        .Where(o => o.CarId == carId &&
//        //                    o.Status != OrderStatus.Canceled &&
//        //                    o.Status != OrderStatus.Completed &&
//        //                    o.StartTime < requestedEnd && o.ExpectedEndTime > requestedStart) // רק מה שחופף!
//        //        .OrderBy(o => o.StartTime)
//        //        .ToList();

//        //    // עכשיו הלוגיקה שלך של ה-Gaps תעבוד במדויק
//        //    var availableGaps = new List<(DateTime Start, DateTime End)>();
//        //    DateTime currentTrack = requestedStart;

//        //    foreach (var order in orders)
//        //    {
//        //        // התעלם מהזמנות שמתחילות לפני הטווח המבוקש (אם יש חפיפה חלקית בתחילת הנסיעה)
//        //        var start = order.StartTime < requestedStart ? requestedStart : order.StartTime;
//        //        var end = order.ExpectedEndTime > requestedEnd ? requestedEnd : order.ExpectedEndTime;

//        //        if (start > currentTrack)
//        //        {
//        //            availableGaps.Add((currentTrack, start));
//        //        }

//        //        if (end > currentTrack)
//        //        {
//        //            currentTrack = end;
//        //        }
//        //    }

//        //    if (currentTrack < requestedEnd)
//        //    {
//        //        availableGaps.Add((currentTrack, requestedEnd));
//        //    }

//        //    // ... המשך הלוגיקה שלך נשאר זהה ...
//        //    // 2. עכשיו בודקים את איכות החלונות (ה-Gaps)
//        //    bool hasValidWindow = availableGaps.Any(gap => (gap.End - gap.Start).TotalMinutes >= 60);

//        //    if (!hasValidWindow)
//        //    {
//        //        // גם אם יש 20 דקות פנויות, אם אין שעה רצופה - מבחינתנו זה תפוס
//        //        return "תפוס";
//        //    }

//        //    // אם הגענו לכאן, יש לפחות שעה אחת פנויה. 
//        //    // האם זה כל מה שהוא ביקש או רק חלק?
//        //    double totalRequestedMinutes = (requestedEnd - requestedStart).TotalMinutes;
//        //    double totalAvailableMinutes = availableGaps.Sum(gap => (gap.End - gap.Start).TotalMinutes);

//        //    if (totalAvailableMinutes < totalRequestedMinutes)
//        //    {
//        //        return "פנוי חלקית"; // יש שעה פנויה, אבל לא כל הטווח
//        //    }

//        //    return "פנוי";
//        //}
//        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
//        //{
//        //    var israelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

//        //    DateTime? localStart = start.HasValue
//        //        ? TimeZoneInfo.ConvertTime(start.Value, israelTimeZone)
//        //        : (DateTime?)null;

//        //    DateTime? localEnd = end.HasValue
//        //        ? TimeZoneInfo.ConvertTime(end.Value, israelTimeZone)
//        //        : (DateTime?)null;

//        //    var cars = _carRepository.GetAll().ToList();

//        //    return cars.Select(c =>
//        //    {
//        //        var dto = _mapper.Map<CarDto>(c);
//        //        dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

//        //        if (localStart.HasValue && localEnd.HasValue)
//        //        {
//        //            dto.Status = GetDetailedAvailabilityStatus(c.Id, localStart.Value, localEnd.Value);
//        //        }
//        //        else
//        //        {
//        //            dto.Status = (CarStatus)c.Status;
//        //        }

//        //        return dto;
//        //    })
//        //    .OrderBy(d => d.Distance)
//        //    .ToList();
//        //}
//        //public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd)
//        //{
//        //    var car = _carRepository.GetById(carId);

//        //    if (car == null) return CarStatus.Occupied;
//        //    if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

//        //    var overlaps = _orderRepository.GetAll()
//        //        .Where(o => o.CarId == carId &&
//        //                    o.Status != OrderStatus.Canceled &&
//        //                    o.Status != OrderStatus.Completed &&
//        //                    o.StartTime < requestedEnd &&
//        //                    o.ExpectedEndTime > requestedStart)
//        //        .OrderBy(o => o.StartTime)
//        //        .ToList();

//        //    if (!overlaps.Any())
//        //        return CarStatus.Available;

//        //    var gaps = new List<(DateTime Start, DateTime End)>();
//        //    DateTime cursor = requestedStart;

//        //    foreach (var o in overlaps)
//        //    {
//        //        if (o.StartTime > cursor)
//        //            gaps.Add((cursor, o.StartTime));

//        //        if (o.ExpectedEndTime > cursor)
//        //            cursor = o.ExpectedEndTime;
//        //    }

//        //    if (cursor < requestedEnd)
//        //        gaps.Add((cursor, requestedEnd));
//        //    if (!gaps.Any())
//        //        return CarStatus.Occupied;
//        //    // הכי חשוב: חלון רציף אמיתי
//        //    var maxGapMinutes = gaps
//        //        .Max(g => (g.End - g.Start).TotalMinutes);

//        //    // אין אפילו שעה אחת פנויה ברצף
//        //    if (maxGapMinutes < 60)
//        //        return CarStatus.Occupied;

//        //    // יש לפחות שעה פנויה בתוך הטווח אבל יש התנגשות
//        //    return CarStatus.PartiallyBooked;
//        //}
//        //הוא הקודם
//        public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
//        {
//            var israelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

//            DateTime? localStart = start.HasValue
//                ? TimeZoneInfo.ConvertTime(start.Value, israelTimeZone)
//                : (DateTime?)null;

//            DateTime? localEnd = end.HasValue
//                ? TimeZoneInfo.ConvertTime(end.Value, israelTimeZone)
//                : (DateTime?)null;

//            var cars = _carRepository.GetAll().ToList();

//            return cars.Select(c =>
//            {
//                var dto = _mapper.Map<CarDto>(c);
//                dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

//                if (localStart.HasValue && localEnd.HasValue)
//                {
//                    // כאן אנחנו קוראים לפונקציה שלך
//                    dto.Status = GetDetailedAvailabilityStatus(c.Id, localStart.Value, localEnd.Value);

//                    // כאן אנחנו מוסיפים את השעות ל-DTO אם הרכב לא פנוי לגמרי
//                    if (dto.Status == CarStatus.PartiallyBooked || dto.Status == CarStatus.Occupied)
//                    {
//                        var blockingOrder = _orderRepository.GetAll()
//                            .FirstOrDefault(o => o.CarId == c.Id &&
//                                               o.Status != OrderStatus.Canceled &&
//                                               o.Status != OrderStatus.Completed &&
//                                               o.StartTime < localEnd.Value &&
//                                               o.ExpectedEndTime > localStart.Value);

//                        if (blockingOrder != null)
//                        {
//                            dto.BlockingOrderStart = blockingOrder.StartTime;
//                            dto.BlockingOrderEnd = blockingOrder.ExpectedEndTime.AddMinutes(15); ;
//                        }
//                    }
//                }
//                else
//                {
//                    dto.Status = (CarStatus)c.Status;
//                }

//                return dto;
//            })
//            .OrderBy(d => d.Distance)
//            .ToList();
//        }

//        //public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd)
//        //{
//        //    var car = _carRepository.GetById(carId);
//        //    if (car == null) return CarStatus.Occupied;

//        //    if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

//        //    var overlaps = _orderRepository.GetAll()
//        //        .Where(o => o.CarId == carId &&
//        //                    o.Status != OrderStatus.Canceled &&
//        //                    o.Status != OrderStatus.Completed &&
//        //                    o.StartTime < requestedEnd &&
//        //                    o.ExpectedEndTime > requestedStart)
//        //        .OrderBy(o => o.StartTime)
//        //        .ToList();

//        //    if (!overlaps.Any()) return CarStatus.Available;

//        //    var firstOrder = overlaps.First();
//        //    if ((firstOrder.StartTime - requestedStart).TotalMinutes >= 60) return CarStatus.PartiallyBooked;

//        //    DateTime cursor = requestedStart;
//        //    foreach (var o in overlaps)
//        //    {
//        //        if ((o.StartTime - cursor).TotalMinutes >= 60) return CarStatus.PartiallyBooked;
//        //        if (o.ExpectedEndTime > cursor) cursor = o.ExpectedEndTime;
//        //    }

//        //    if ((requestedEnd - cursor).TotalMinutes > 0) return CarStatus.PartiallyBooked;

//        //    return CarStatus.Occupied;
//        //}

//        public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd)
//        {
//            var car = _carRepository.GetById(carId);
//            if (car == null) return CarStatus.Occupied;
//            if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

//            int buffer = 15;

//            var overlaps = _orderRepository.GetAll()
//                .Where(o => o.CarId == carId &&
//                            o.Status != OrderStatus.Canceled &&
//                            o.Status != OrderStatus.Completed &&
//                            // הבדיקה כאן צריכה לכלול את הבאפר
//                            o.StartTime < requestedEnd.AddMinutes(buffer) &&
//                            o.ExpectedEndTime.AddMinutes(buffer) > requestedStart)
//                .OrderBy(o => o.StartTime)
//                .ToList();

//            if (!overlaps.Any()) return CarStatus.Available;

//            // חישוב חלונות פנויים עם התחשבות בבאפר
//            DateTime cursor = requestedStart;
//            foreach (var o in overlaps)
//            {
//                // חלון פנוי הוא רק מהסיום של הקודמת + באפר ועד ההתחלה של הבאה
//                if ((o.StartTime - cursor).TotalMinutes >= (60 + buffer))
//                    return CarStatus.PartiallyBooked;

//                if (o.ExpectedEndTime.AddMinutes(buffer) > cursor)
//                    cursor = o.ExpectedEndTime.AddMinutes(buffer);
//            }

//            return CarStatus.Occupied;
//        }
//    }
//        //    public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
//        //{
//        //    var israelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

//        //    DateTime? localStart = start.HasValue
//        //        ? TimeZoneInfo.ConvertTime(start.Value, israelTimeZone)
//        //        : (DateTime?)null;

//        //    DateTime? localEnd = end.HasValue
//        //        ? TimeZoneInfo.ConvertTime(end.Value, israelTimeZone)
//        //        : (DateTime?)null;

//        //    var cars = _carRepository.GetAll().ToList();

//        //    return cars.Select(c =>
//        //    {
//        //        var dto = _mapper.Map<CarDto>(c);
//        //        dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

//        //        if (localStart.HasValue && localEnd.HasValue)
//        //        {
//        //            dto.Status = GetDetailedAvailabilityStatus(c.Id, localStart.Value, localEnd.Value);
//        //        }
//        //        else
//        //        {
//        //            dto.Status = (CarStatus)c.Status;
//        //        }

//        //        return dto;
//        //    })
//        //    .OrderBy(d => d.Distance)
//        //    .ToList();
//        //}
//        //public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd)
//        //{
//        //    var car = _carRepository.GetById(carId);

//        //    if (car == null) return CarStatus.Occupied;
//        //    if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

//        //    var overlaps = _orderRepository.GetAll()
//        //        .Where(o => o.CarId == carId &&
//        //                    o.Status != OrderStatus.Canceled &&
//        //                    o.Status != OrderStatus.Completed &&
//        //                    o.StartTime < requestedEnd &&
//        //                    o.ExpectedEndTime > requestedStart)
//        //        .OrderBy(o => o.StartTime)
//        //        .ToList();

//        //    if (!overlaps.Any())
//        //        return CarStatus.Available;

//        //    var gaps = new List<(DateTime Start, DateTime End)>();
//        //    DateTime cursor = requestedStart;

//        //    foreach (var o in overlaps)
//        //    {
//        //        if (o.StartTime > cursor)
//        //            gaps.Add((cursor, o.StartTime));

//        //        if (o.ExpectedEndTime > cursor)
//        //            cursor = o.ExpectedEndTime;
//        //    }

//        //    if (cursor < requestedEnd)
//        //        gaps.Add((cursor, requestedEnd));
//        //    if (!gaps.Any())
//        //        return CarStatus.Occupied;
//        //    // הכי חשוב: חלון רציף אמיתי
//        //    var maxGapMinutes = gaps
//        //        .Max(g => (g.End - g.Start).TotalMinutes);

//        //    // אין אפילו שעה אחת פנויה ברצף
//        //    if (maxGapMinutes < 60)
//        //        return CarStatus.Occupied;

//        //    // יש לפחות שעה פנויה בתוך הטווח אבל יש התנגשות
//        //    return CarStatus.PartiallyBooked;
//        //}
//    }







using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CarStatus = Common.Dto.CarStatus;
using OrderStatus = Repository.Entities.OrderStatus;

namespace Service.Services
{
    public class CarService : ICarService
    {
        private readonly IRepository<Car> _carRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IMapper _mapper;

        public CarService(IRepository<Car> carRepository, IRepository<Order> orderRepository, IMapper mapper)
        {
            _carRepository = carRepository;
            _orderRepository = orderRepository;
            _mapper = mapper;
        }
        public async Task<CarDto> Add(CarDto item)
        {
            var isLicenseTaken = _carRepository.GetAll().Any(c => c.LicensePlate == item.LicensePlate);
            if (isLicenseTaken)
            {
                return null;
            }
            Car newCar = _mapper.Map<Car>(item);
            var saved = _carRepository.Add(newCar);
            return _mapper.Map<CarDto>(saved);
        }

        public bool Delete(int id)
        {
            if (!Exists(id)) return false;
            return _carRepository.Delete(id);
        }

        public bool Exists(int id)
        {
            return _carRepository.Exists(id);
        }

        public IEnumerable<CarDto> GetAll()
        {
            var cars = _carRepository.GetAll();
            return _mapper.Map<IEnumerable<CarDto>>(cars);
        }

        public CarDto? GetById(int id)
        {
            var c = _carRepository.GetById(id);
            if (c == null) return null;
            return _mapper.Map<CarDto>(c);

        }

        public bool Update(int id, CarDto item)
        {
            var existingCar = _carRepository.GetById(id);
            if (existingCar == null) return false;
            var isLicenseTaken = _carRepository.GetAll().Any(c => c.LicensePlate == item.LicensePlate && c.Id != id);
            if (isLicenseTaken)
            {
                return false;
            }
            _mapper.Map(item, existingCar);
            return _carRepository.Update(id, existingCar);
        }
        //
        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon)
        //{
        //    var sortedCars = _carRepository.GetAll()
        //        .AsEnumerable() // עוברים לזיכרון כדי לבצע את חישוב המרחק
        //        .OrderBy(c => CalculateDistance(userLat, userLon, c.Latitude, c.Longitude));
        //        return _mapper.Map<IEnumerable<CarDto>>(sortedCars);
        //}
        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon)
        //{
        //    // 1. שליפת כל הרכבים מהמסד
        //    var cars = _carRepository.GetAll().ToList();
        //    var carDtos = cars.Select(c =>
        //    {
        //        var dto = _mapper.Map<CarDto>(c);
        //        dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);
        //        return dto;
        //    })
        //    // 3. עכשיו המיון בטוח - הוא מתבצע על הנתון שיוחזר ל-React
        //    .OrderBy(d => d.Distance)
        //    .ToList();

        //    return carDtos;
        //}
        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
        //{
        //    // 1. שליפת כל הרכבים מהמסד
        //    var cars = _carRepository.GetAll().ToList();

        //    var carDtos = cars.Select(c =>
        //    {
        //        var dto = _mapper.Map<CarDto>(c);

        //        // 2. חישוב המרחק (הלוגיקה הקיימת שלך)
        //        dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

        //        // 3. עדכון הסטטוס בזמן אמת - רק אם נשלחו זמנים מה-React
        //        if (start.HasValue && end.HasValue)
        //        {
        //            string statusDescription = GetDetailedAvailabilityStatus(c.Id, start.Value, end.Value);

        //            // המרה לערך מספרי שה-React שלך יודע לעכל (לפי הקומפוננטה שראינו קודם)
        //          dto.Status = statusDescription switch
        //          {
        //              "פנוי" => CarStatus.Available,
        //              "פנוי חלקית" => CarStatus.PartiallyBooked,
        //              "תפוס" => CarStatus.Occupied,
        //              "לא זמין" => CarStatus.Occupied, // או להוסיף NotAvailable ל-Enum במידת הצורך
        //              _ => CarStatus.Occupied
        //          };
        //        }
        //        return dto;
        //    })
        //    // 4. מיון לפי המרחק (שומר על הדרישה המקורית שלך)
        //    .OrderBy(d => d.Distance)
        //    .ToList();

        //    return carDtos;
        //}
        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
        //{
        //    var cars = _carRepository.GetAll().ToList();

        //    var carDtos = cars.Select(c =>
        //    {
        //        var dto = _mapper.Map<CarDto>(c);
        //        dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

        //        if (start.HasValue && end.HasValue)
        //        {
        //            string statusDescription = GetDetailedAvailabilityStatus(c.Id, start.Value, end.Value);

        //            dto.Status = statusDescription switch
        //            {
        //                "פנוי" => CarStatus.Available,
        //                "פנוי חלקית" => CarStatus.PartiallyBooked,
        //                "תפוס" => CarStatus.Occupied,
        //                "לא זמין" => CarStatus.Occupied,
        //                _ => CarStatus.Occupied
        //            };
        //        }
        //        return dto;
        //    }).ToList(); 

        //    return carDtos.OrderBy(d => d.Distance).ToList();
        //}
        // נוסחת Haversine לחישוב מרחק אווירי בקילומטרים
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var d1 = lat1 * (Math.PI / 180.0);
            var num1 = lon1 * (Math.PI / 180.0);
            var d2 = lat2 * (Math.PI / 180.0);
            var num2 = (lon2 * (Math.PI / 180.0)) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6371.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }

        public bool SendToMaintenance(int carId)
        {
            var car = _carRepository.GetById(carId);
            if (car != null)
            {
                car.NeedsMaintenance = true;
                _carRepository.Update(carId, car);
                return true;
            }
            return false;
        }

        public bool ReleaseFromMaintenance(int carId)
        {
            var car = _carRepository.GetById(carId);
            if (car != null)
            {
                car.NeedsMaintenance = false;
                _carRepository.Update(carId, car);
                return true;
            }
            return false;
        }

        public IEnumerable<CarDto> GetVehiclesNeedingFuel()
        {
            var carsNeedingFuel = _carRepository.GetAll().Where(c => c.FuelLevel < 15).ToList();
            return _mapper.Map<IEnumerable<CarDto>>(carsNeedingFuel);
        }

        public bool UpdateFuelLevel(int carId, int newLevel)
        {
            var car = _carRepository.GetById(carId);
            if (car != null)
            {
                car.FuelLevel = newLevel;
                _carRepository.Update(carId, car);
                return true;
            }
            return false;
        }

        public IEnumerable<CarDto> GetAvailableCarsByRegion(int regionId)
        {
            return GetAvailableCars(DateTime.Now, DateTime.Now.AddMinutes(30), regionId);
        }

        public bool UpdateMileage(int carId, int newMileage)
        {
            var car = _carRepository.GetById(carId);
            if (car == null) return false;
            car.Kilometers = newMileage;
            return _carRepository.Update(carId, car);
        }

        public bool UpdateStatus(int carId, string status)
        {
            var car = _carRepository.GetById(carId);
            if (car == null) return false;
            if (!Enum.TryParse<Common.Dto.CarStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                return false;
            }
            car.Status = (Repository.Entities.CarStatus)parsedStatus;
            return _carRepository.Update(carId, car);
        }
        public bool IsCarFitForRoad(int carId)
        {
            var car = _carRepository.GetById(carId);
            if (car == null) return false;
            return !car.NeedsMaintenance && car.FuelLevel >= 15;
        }
        public IEnumerable<CarDto> GetAvailableCars(DateTime start, DateTime end, int regionId)
        {
            double tripDurationHours = (end - start).TotalHours;

            // 1. שיפור ביצועים: שליפת כל ההזמנות שחופפות לזמן המבוקש פעם אחת בלבד
            var busyCarIds = _orderRepository.GetAll()
                .Where(order => order.Status != OrderStatus.Completed &&
                                start < order.ExpectedEndTime &&
                                end > order.StartTime)
                .Select(order => order.CarId)
                .ToList();

            // 2. סינון הרכבים על בסיס הרשימה שהכנו
            var availableCars = _carRepository.GetAll()
         .Where(c => c.RegionId == regionId && !c.NeedsMaintenance)
         .AsEnumerable()
         .Where(car => !busyCarIds.Contains(car.Id) &&
                       !(tripDurationHours > 4 && car.FuelLevel < 40) &&
                       !(car.FuelLevel < 15))
         .ToList();

            // כאן השינוי: מיפוי ידני שדורס את הסטטוס לפי הזמן המבוקש
            return availableCars.Select(car =>
            {
                var dto = _mapper.Map<CarDto>(car);

                // כיוון שהרכב עבר את הסינון, אנחנו יודעים שהוא פנוי בטווח הזה
                // לכן נגדיר לו ידנית סטטוס "פנוי" כדי להבטיח עקביות
                dto.Status = Common.Dto.CarStatus.Available;

                return dto;
            });
        }

        public string CheckCarSuitability(int carId, DateTime start, DateTime end)
        {
            var car = _carRepository.GetById(carId);
            if (car == null) return "רכב לא נמצא";
            if (car.NeedsMaintenance) return "הרכב נמצא בתחזוקה";
            if (car.FuelLevel < 15) return "אין מספיק דלק לנסיעה מינימלית";

            double hours = (end - start).TotalHours;
            if (hours > 4 && car.FuelLevel < 40) return "לנסיעה ארוכה נדרש לפחות 40% דלק";

            // בדיקה שנוספה: האם הרכב פנוי בזמן הזה?
            bool isBusy = _orderRepository.GetAll().Any(order =>
                order.CarId == carId &&
                order.Status != OrderStatus.Completed &&
                start < order.ExpectedEndTime &&
                end > order.StartTime);

            if (isBusy) return "הרכב כבר מוזמן לזמן המבוקש";

            return "OK";
        }
        public IEnumerable<CarDto> GetAllPopularCars(int count = 5)
        {
            var popularCars = _carRepository.GetAll()
                .OrderByDescending(c => c.TotalOrdersCount)
                .Take(count)
                .ToList();
            return _mapper.Map<IEnumerable<CarDto>>(popularCars);
        }

        public IEnumerable<CarDto> GetByStatus(string status)
        {
            if (!Enum.TryParse<Repository.Entities.CarStatus>(status, true, out var parsedStatus))
                return Enumerable.Empty<CarDto>();

            var cars = _carRepository.GetAll().Where(c => c.Status == parsedStatus).ToList();
            return _mapper.Map<IEnumerable<CarDto>>(cars);
        }
        //public object GetCarAvailabilityInfo(int carId)
        //{
        //    var car = _carRepository.GetById(carId);
        //    if (car == null) return null;

        //    var now = DateTime.Now;
        //    var hourFromNow = now.AddHours(1);
        //    int bufferMinutes = 15;

        //    var orders = _orderRepository.GetAll()
        //        .Where(o => o.CarId == carId && o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed)
        //        .ToList();

        //    // בדיקה האם תפוס עכשיו (כולל הוופל של ה-15 דקות)
        //    var currentOrder = orders.FirstOrDefault(o =>
        //        now >= o.StartTime &&
        //        now < o.ExpectedEndTime.AddMinutes(bufferMinutes));

        //    var upcomingOrder = orders.FirstOrDefault(o =>
        //        o.StartTime > now &&
        //        o.StartTime.AddMinutes(-bufferMinutes) <= hourFromNow);

        //    string displayStatus = "פנוי";
        //    DateTime? nextAvailable = null;

        //    if (currentOrder != null)
        //    {
        //        displayStatus = "תפוס";
        //        nextAvailable = currentOrder.ExpectedEndTime.AddMinutes(bufferMinutes);
        //    }
        //    else if (upcomingOrder != null)
        //    {
        //        displayStatus = "פנוי חלקית";
        //        nextAvailable = upcomingOrder.ExpectedEndTime.AddMinutes(bufferMinutes);
        //    }

        //    return new
        //    {
        //        CarId = car.Id,
        //        Model = car.Model,
        //        Address = car.StartParking,
        //        Status = displayStatus,
        //        // השדה שה-React שלך צמא לו:
        //        NextAvailableStart = nextAvailable,
        //        NextOrderStart = upcomingOrder?.StartTime,
        //        ImageUrl = car.ImageUrl
        //    };
        //}

        public bool UpdateLockStatus(int carId, bool isLocked)
        {
            var car = _carRepository.GetById(carId);
            if (car == null) return false;
            car.IsLocked = isLocked;

            car.LastLockTime = DateTime.Now;

            return _carRepository.Update(carId, car);
        }
        //public CarDto GetDetailedAvailability(int carId, DateTime requestedStart, DateTime requestedEnd)
        //{
        //    var orders = _orderRepository.GetOrdersByCarId(carId)
        //        .Where(o => o.Status != OrderStatus.Cancelled && o.StartTime.Date == requestedStart.Date)
        //        .OrderBy(o => o.StartTime)
        //        .ToList();

        //    // בדיקה אם יש חפיפה כלשהי
        //    var conflictingOrder = orders.FirstOrDefault(o => requestedStart < o.EndTime && requestedEnd > o.StartTime);

        //    if (conflictingOrder == null)
        //    {
        //        return new CarAvailabilityDto { Status = 0, Message = "פנוי לגמרי" };
        //    }

        //    // אם יש חפיפה, נבדוק אם אפשר להציע זמן קרוב
        //    return new CarAvailabilityDto
        //    {
        //        Status = 1,
        //        Message = "פנוי חלקית",
        //        NextAvailableSlot = orders.FirstOrDefault(o => o.StartTime >= requestedEnd)?.StartTime,
        //        MaxEndTimePossible = orders.FirstOrDefault(o => o.StartTime > requestedStart)?.StartTime
        //    };
        //}
        // בתוך CarService.cs
        //public string GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd)
        //{
        //    var car = _carRepository.GetById(carId);
        //    if (car == null || car.NeedsMaintenance) return "לא זמין";

        //    // שינוי קריטי: סינון הזמנות שרלוונטיות אך ורק לטווח המבוקש
        //    var orders = _orderRepository.GetAll()
        //        .Where(o => o.CarId == carId &&
        //                    o.Status != OrderStatus.Canceled &&
        //                    o.Status != OrderStatus.Completed &&
        //                    o.StartTime < requestedEnd && o.ExpectedEndTime > requestedStart) // רק מה שחופף!
        //        .OrderBy(o => o.StartTime)
        //        .ToList();

        //    // עכשיו הלוגיקה שלך של ה-Gaps תעבוד במדויק
        //    var availableGaps = new List<(DateTime Start, DateTime End)>();
        //    DateTime currentTrack = requestedStart;

        //    foreach (var order in orders)
        //    {
        //        // התעלם מהזמנות שמתחילות לפני הטווח המבוקש (אם יש חפיפה חלקית בתחילת הנסיעה)
        //        var start = order.StartTime < requestedStart ? requestedStart : order.StartTime;
        //        var end = order.ExpectedEndTime > requestedEnd ? requestedEnd : order.ExpectedEndTime;

        //        if (start > currentTrack)
        //        {
        //            availableGaps.Add((currentTrack, start));
        //        }

        //        if (end > currentTrack)
        //        {
        //            currentTrack = end;
        //        }
        //    }

        //    if (currentTrack < requestedEnd)
        //    {
        //        availableGaps.Add((currentTrack, requestedEnd));
        //    }

        //    // ... המשך הלוגיקה שלך נשאר זהה ...
        //    // 2. עכשיו בודקים את איכות החלונות (ה-Gaps)
        //    bool hasValidWindow = availableGaps.Any(gap => (gap.End - gap.Start).TotalMinutes >= 60);

        //    if (!hasValidWindow)
        //    {
        //        // גם אם יש 20 דקות פנויות, אם אין שעה רצופה - מבחינתנו זה תפוס
        //        return "תפוס";
        //    }

        //    // אם הגענו לכאן, יש לפחות שעה אחת פנויה. 
        //    // האם זה כל מה שהוא ביקש או רק חלק?
        //    double totalRequestedMinutes = (requestedEnd - requestedStart).TotalMinutes;
        //    double totalAvailableMinutes = availableGaps.Sum(gap => (gap.End - gap.Start).TotalMinutes);

        //    if (totalAvailableMinutes < totalRequestedMinutes)
        //    {
        //        return "פנוי חלקית"; // יש שעה פנויה, אבל לא כל הטווח
        //    }

        //    return "פנוי";
        //}
        //public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
        //{
        //    var israelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

        //    DateTime? localStart = start.HasValue
        //        ? TimeZoneInfo.ConvertTime(start.Value, israelTimeZone)
        //        : (DateTime?)null;

        //    DateTime? localEnd = end.HasValue
        //        ? TimeZoneInfo.ConvertTime(end.Value, israelTimeZone)
        //        : (DateTime?)null;

        //    var cars = _carRepository.GetAll().ToList();

        //    return cars.Select(c =>
        //    {
        //        var dto = _mapper.Map<CarDto>(c);
        //        dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

        //        if (localStart.HasValue && localEnd.HasValue)
        //        {
        //            dto.Status = GetDetailedAvailabilityStatus(c.Id, localStart.Value, localEnd.Value, out var nextFree);

        //        }
        //        else
        //        {
        //            dto.Status = (CarStatus)c.Status;
        //        }

        //        return dto;
        //    })
        //    .OrderBy(d => d.Distance)
        //    .ToList();
        //}
        //    public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd, out DateTime? nextFreeTime)
        //    {
        //        nextFreeTime = null;
        //        var car = _carRepository.GetById(carId);
        //        int bufferMinutes = 15; // מרווח ביטחון בין הזמנות

        //        if (car == null) return CarStatus.Occupied;

        //        // בדיקת תחזוקה ודלק
        //        if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

        //        // שליפת הזמנות חופפות כולל ה-Buffer
        //        var overlaps = _orderRepository.GetAll()
        //            .Where(o => o.CarId == carId &&
        //                        o.Status != OrderStatus.Canceled &&
        //                        o.Status != OrderStatus.Completed &&
        //                        o.StartTime < requestedEnd.AddMinutes(bufferMinutes) &&
        //                        o.ExpectedEndTime.AddMinutes(bufferMinutes) > requestedStart)
        //            .OrderBy(o => o.StartTime)
        //            .ToList();

        //        if (!overlaps.Any())
        //            return CarStatus.Available;

        //        // חישוב חלונות פנויים (Gaps)
        //        var gaps = new List<(DateTime Start, DateTime End)>();
        //        DateTime cursor = requestedStart;

        //        foreach (var o in overlaps)
        //        {
        //            // התחלה של ההזמנה הנוכחית עם ה-Buffer "לפני"
        //            DateTime effectiveOrderStart = o.StartTime.AddMinutes(-bufferMinutes);

        //            if (effectiveOrderStart > cursor)
        //                gaps.Add((cursor, effectiveOrderStart));

        //            // סיום ההזמנה הנוכחית עם ה-Buffer "אחרי"
        //            DateTime effectiveOrderEnd = o.ExpectedEndTime.AddMinutes(bufferMinutes);

        //            if (effectiveOrderEnd > cursor)
        //                cursor = effectiveOrderEnd;
        //        }

        //        if (cursor < requestedEnd)
        //            gaps.Add((cursor, requestedEnd));

        //        // עדכון שעת הזמינות הבאה עבור ה-UI (הזמן הכי מאוחר שמישהו מסיים + באפר)
        //        nextFreeTime = overlaps.Max(o => o.ExpectedEndTime).AddMinutes(bufferMinutes);

        //        // אם אין חלונות בכלל
        //        if (!gaps.Any())
        //            return CarStatus.Occupied;

        //        // בדיקת חלון רציף מינימלי (שעה) כפי שביקשת
        //        var maxGapMinutes = gaps.Max(g => (g.End - g.Start).TotalMinutes);

        //        if (maxGapMinutes < 60)
        //            return CarStatus.Occupied;

        //        // יש חלון של שעה לפחות, אך יש התנגשויות אחרות בטווח
        //        return CarStatus.PartiallyBooked;
        //    }
        public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
        {
            var israelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
            DateTime? localStart = start.HasValue ? TimeZoneInfo.ConvertTime(start.Value, israelTimeZone) : (DateTime?)null;
            DateTime? localEnd = end.HasValue ? TimeZoneInfo.ConvertTime(end.Value, israelTimeZone) : (DateTime?)null;

            var cars = _carRepository.GetAll().ToList();

            return cars.Select(c =>
            {
                var dto = _mapper.Map<CarDto>(c);
                dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

                if (localStart.HasValue && localEnd.HasValue)
                {
                    // שולחים לסטטוס לקבל את תחילת החסימה האמיתית וסוף החסימה האמיתי
                    dto.Status = GetDetailedAvailabilityStatus(c.Id, localStart.Value, localEnd.Value, out var actualConflictStart, out var actualConflictEnd);

                    if (dto.Status != CarStatus.Available)
                    {
                        dto.BlockingOrderStart = actualConflictStart; // הזמן האמיתי שהרכב נתפס (פחות 15 דק באפר)
                        dto.BlockingOrderEnd = actualConflictEnd;     // הזמן האמיתי שהרכב משתחרר (ועוד 15 דק באפר)
                    }
                }
                else { dto.Status = (CarStatus)c.Status; }
                return dto;
            }).OrderBy(d => d.Distance).ToList();
        }

        //public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd, out DateTime? conflictStart, out DateTime? conflictEnd)
        //{
        //    conflictStart = null; conflictEnd = null;
        //    var car = _carRepository.GetById(carId);
        //    int buffer = 15;

        //    if (car == null) return CarStatus.Occupied;
        //    if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

        //    var overlaps = _orderRepository.GetAll()
        //        .Where(o => o.CarId == carId && o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed &&
        //                    o.StartTime < requestedEnd.AddMinutes(buffer) &&
        //                    o.ExpectedEndTime.AddMinutes(buffer) > requestedStart)
        //        .OrderBy(o => o.StartTime).ToList();

        //    if (!overlaps.Any()) return CarStatus.Available;

        //    // כאן אנחנו מגדירים ל-React מתי החסימה באמת
        //    conflictStart = overlaps.First().StartTime.AddMinutes(-buffer);
        //    conflictEnd = overlaps.Last().ExpectedEndTime.AddMinutes(buffer);

        //    // בדיקת Gaps (חלונות פנויים של שעה)
        //    var gaps = new List<(DateTime Start, DateTime End)>();
        //    DateTime cursor = requestedStart;
        //    foreach (var o in overlaps)
        //    {
        //        if (o.StartTime.AddMinutes(-buffer) > cursor)
        //            gaps.Add((cursor, o.StartTime.AddMinutes(-buffer)));

        //        if (o.ExpectedEndTime.AddMinutes(buffer) > cursor)
        //            cursor = o.ExpectedEndTime.AddMinutes(buffer);
        //    }
        //    if (cursor < requestedEnd) gaps.Add((cursor, requestedEnd));

        //    if (!gaps.Any() || gaps.Max(g => (g.End - g.Start).TotalMinutes) < 60)
        //        return CarStatus.Occupied;

        //    return CarStatus.PartiallyBooked;
        //}
        //public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd, out DateTime? conflictStart, out DateTime? conflictEnd)
        //{
        //    conflictStart = null;
        //    conflictEnd = null;
        //    var car = _carRepository.GetById(carId);
        //    int buffer = 15;

        //    // בדיקות תקינות בסיסיות של הרכב
        //    if (car == null) return CarStatus.Occupied;
        //    if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

        //    // 1. שליפת כל ההזמנות שחופפות לזמן המבוקש (כולל התחשבות בבאפר)
        //    var overlaps = _orderRepository.GetAll()
        //        .Where(o => o.CarId == carId &&
        //                    o.Status != OrderStatus.Canceled &&
        //                    o.Status != OrderStatus.Completed &&
        //                    o.StartTime.AddMinutes(-buffer) < requestedEnd &&
        //                    o.ExpectedEndTime.AddMinutes(buffer) > requestedStart)
        //        .OrderBy(o => o.StartTime)
        //        .ToList();

        //    // אם אין שום חפיפה - הרכב פנוי לחלוטין
        //    if (!overlaps.Any()) return CarStatus.Available;

        //    // 2. הגדרת זמני החסימה עבור ה-React (הזמן הראשון שתפוס והזמן האחרון שמשתחרר)
        //    conflictStart = overlaps.First().StartTime.AddMinutes(-buffer);
        //    conflictEnd = overlaps.Last().ExpectedEndTime.AddMinutes(buffer);

        //    // 3. בדיקת חלונות פנויים (Gaps) בתוך הטווח המבוקש
        //    var gaps = new List<(DateTime Start, DateTime End)>();
        //    DateTime cursor = requestedStart;

        //    foreach (var o in overlaps)
        //    {
        //        DateTime currentOrderStartWithBuffer = o.StartTime.AddMinutes(-buffer);

        //        // אם יש חלון פנוי בין הסמן הנוכחי לתחילת ההזמנה הבאה
        //        if (currentOrderStartWithBuffer > cursor)
        //        {
        //            gaps.Add((cursor, currentOrderStartWithBuffer));
        //        }

        //        // קידום הסמן לסוף ההזמנה הנוכחית (עם הבאפר)
        //        DateTime currentOrderEndWithBuffer = o.ExpectedEndTime.AddMinutes(buffer);
        //        if (currentOrderEndWithBuffer > cursor)
        //        {
        //            cursor = currentOrderEndWithBuffer;
        //        }
        //    }

        //    // בדיקה אם נשאר חלון פנוי בין ההזמנה האחרונה לסוף הטווח המבוקש
        //    if (cursor < requestedEnd)
        //    {
        //        gaps.Add((cursor, requestedEnd));
        //    }

        //    // 4. הכרעה: האם קיים חלון פנוי של לפחות שעה?
        //    if (!gaps.Any() || gaps.Max(g => (g.End - g.Start).TotalMinutes) < 60)
        //    {
        //        // אין אף רצף של שעה פנויה - הרכב נחשב תפוס
        //        return CarStatus.Occupied;
        //    }

        //    // קיים לפחות חלון אחד של שעה פנויה
        //    return CarStatus.PartiallyBooked;
        //}
        //public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd, out DateTime? conflictStart, out DateTime? conflictEnd)
        //{
        //    conflictStart = null;
        //    conflictEnd = null;
        //    var car = _carRepository.GetById(carId);
        //    int buffer = 15;

        //    if (car == null) return CarStatus.Occupied;
        //    if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

        //    var overlaps = _orderRepository.GetAll()
        //        .Where(o => o.CarId == carId &&
        //                    o.Status != OrderStatus.Canceled &&
        //                    o.Status != OrderStatus.Completed &&
        //                    o.StartTime.AddMinutes(-buffer) < requestedEnd &&
        //                    o.ExpectedEndTime.AddMinutes(buffer) > requestedStart)
        //        .OrderBy(o => o.StartTime)
        //        .ToList();

        //    // אם אין שום חפיפה - פנוי לגמרי
        //    if (!overlaps.Any()) return CarStatus.Available;

        //    // הגדרת זמני החסימה עבור ה-React
        //    conflictStart = overlaps.First().StartTime.AddMinutes(-buffer);
        //    conflictEnd = overlaps.Last().ExpectedEndTime.AddMinutes(buffer);

        //    // חישוב חלונות פנויים (Gaps)
        //    var gaps = new List<(DateTime Start, DateTime End)>();
        //    DateTime cursor = requestedStart;

        //    foreach (var o in overlaps)
        //    {
        //        DateTime currentOrderStartWithBuffer = o.StartTime.AddMinutes(-buffer);
        //        if (currentOrderStartWithBuffer > cursor)
        //        {
        //            gaps.Add((cursor, currentOrderStartWithBuffer));
        //        }
        //        DateTime currentOrderEndWithBuffer = o.ExpectedEndTime.AddMinutes(buffer);
        //        if (currentOrderEndWithBuffer > cursor) cursor = currentOrderEndWithBuffer;
        //    }
        //    if (cursor < requestedEnd) gaps.Add((cursor, requestedEnd));


        //    // --- הלוגיקה המתוקנת ---

        //    // 1. אם אין חלונות פנויים בכלל (חפיפה מלאה) - תפוס
        //    if (!gaps.Any()) return CarStatus.Occupied;

        //    // 2. בדיקת איכות החלונות:
        //    // האם יש חלון של לפחות 60 דקות?
        //    bool hasSignificantGap = gaps.Any(g => (g.End - g.Start).TotalMinutes >= 60);

        //    // האם יש חלון בקצוות שהוא מספיק גדול כדי להזיז את הנסיעה (למשל 30 דקות)?
        //    // זה מונע מצב שבו על 5 דקות פנויות נגיד "פנוי חלקית"
        //    bool hasUsableEdgeGap = gaps.Any(g =>
        //        (g.Start == requestedStart || g.End == requestedEnd) &&
        //        (g.End - g.Start).TotalMinutes >= 30);

        //    if (hasSignificantGap || hasUsableEdgeGap)
        //    {
        //        return CarStatus.PartiallyBooked;
        //    }

        //    // אם כל מה שנשאר אלו חלונות קטנים מ-30 דקות בקצוות או שאין חלון של שעה
        //    return CarStatus.Occupied;
        //}
        //קודם
        //public object GetCarAvailabilityInfo(int carId)
        //{
        //    var car = _carRepository.GetById(carId);
        //    if (car == null) return null;

        //    var now = DateTime.Now;

        //    // שליפת הזמנות רלוונטיות
        //    var orders = _orderRepository.GetAll()
        //        .Where(o => o.CarId == carId &&
        //                    o.Status != OrderStatus.Canceled &&
        //                    o.Status != OrderStatus.Completed)
        //        .ToList();

        //    // 1. נסיעה פעילה כרגע (בדיקה נקייה בלי באפר)
        //    var activeOrder = orders.FirstOrDefault(o => o.Status == OrderStatus.Active);

        //    // 2. הזמנה שממתינה להתחיל (בדיקה אם אנחנו בתוך זמן ההזמנה המקורי)
        //    var pendingCurrentOrder = orders.FirstOrDefault(o =>
        //        o.Status == OrderStatus.Pending &&
        //        now >= o.StartTime &&
        //        now < o.ExpectedEndTime);

        //    // 3. ההזמנה הבאה ביומן
        //    var upcomingOrder = orders
        //        .Where(o => o.Status == OrderStatus.Pending && o.StartTime > now)
        //        .OrderBy(o => o.StartTime)
        //        .FirstOrDefault();

        //    // הגדרת משתני עזר
        //    string displayStatusText = "פנוי";
        //    CarStatus numericStatus = CarStatus.Available;
        //    DateTime? blockingOrderEnd = null;
        //    DateTime? nextAvailableStart = null;
        //    string note = "";

        //    // לוגיקת הכרעה - ללא תוספות זמן שרירותיות
        //    if (activeOrder != null)
        //    {
        //        numericStatus = CarStatus.Occupied;
        //        displayStatusText = "תפוס";
        //        blockingOrderEnd = activeOrder.ExpectedEndTime;
        //        nextAvailableStart = activeOrder.ExpectedEndTime; // זמין מיד בסיום

        //        if (now > activeOrder.ExpectedEndTime)
        //            note = "הרכב באיחור בהחזרה";
        //    }
        //    else if (pendingCurrentOrder != null)
        //    {
        //        numericStatus = CarStatus.Occupied;
        //        displayStatusText = "תפוס";
        //        blockingOrderEnd = pendingCurrentOrder.ExpectedEndTime;
        //        nextAvailableStart = pendingCurrentOrder.ExpectedEndTime;
        //    }
        //    else if (upcomingOrder != null && upcomingOrder.StartTime <= now.AddHours(1))
        //    {
        //        numericStatus = CarStatus.PartiallyBooked;
        //        displayStatusText = "פנוי חלקית";
        //        nextAvailableStart = upcomingOrder.ExpectedEndTime;
        //        // שימי לב: כאן זה נשאר upcomingOrder.StartTime אם את רוצה לדעת מתי הוא נתפס שוב
        //    }

        //    // בדיקת תחזוקה
        //    if (car.NeedsMaintenance || car.FuelLevel < 15)
        //    {
        //        numericStatus = CarStatus.Maintenance;
        //        displayStatusText = "בטיפול / דלק נמוך";
        //    }

        //    return new
        //    {
        //        Id = car.Id,
        //        Model = car.Model,
        //        Address = car.StartParking,
        //        FuelLevel = car.FuelLevel,
        //        Kilometers = car.Kilometers,
        //        Seats = car.Seats,
        //        ImageUrl = car.ImageUrl,

        //        Status = (int)numericStatus,
        //        StatusLabel = displayStatusText,
        //        Note = note,

        //        // הזמנים נשלחים נקיים כמו שהם ב-Database
        //        BlockingOrderEnd = blockingOrderEnd,
        //        NextAvailableStart = nextAvailableStart ?? now,
        //        NextOrderStart = upcomingOrder?.StartTime
        //    };
        //}       
        //public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd, out DateTime? conflictStart, out DateTime? conflictEnd)
        //{
        //    conflictStart = null;
        //    conflictEnd = null;
        //    var car = _carRepository.GetById(carId);
        //    int buffer = 15;

        //    // 1. בדיקות תקינות בסיסיות (תחזוקה ודלק)
        //    if (car == null) return CarStatus.Occupied;
        //    if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

        //    // 2. שדרוג קריטי: בדיקת המציאות בשטח (נסיעה פעילה ואיחורים)
        //    var activeOrder = _orderRepository.GetAll()
        //        .FirstOrDefault(o => o.CarId == carId && o.Status == OrderStatus.Active);

        //    if (activeOrder != null)
        //    {
        //        // קביעת "זמן סיום ריאלי" - אם הוא באיחור, הזמן הקובע הוא 'עכשיו'. אם לא, זמן הסיום המקורי.
        //        DateTime realEndTimeWithBuffer = (activeOrder.ExpectedEndTime > DateTime.Now
        //            ? activeOrder.ExpectedEndTime
        //            : DateTime.Now).AddMinutes(buffer);

        //        // אם הנסיעה הפעילה (כולל האיחור והבאפר) חופפת לתחילת ההזמנה המבוקשת
        //        if (requestedStart < realEndTimeWithBuffer)
        //        {
        //            conflictStart = activeOrder.StartTime;
        //            conflictEnd = realEndTimeWithBuffer;
        //            return CarStatus.Occupied; // הרכב תפוס פיזית ולא יכול להתחיל נסיעה חדשה
        //        }
        //    }

        //    // 3. בדיקת חפיפות ביומן (הזמנות עתידיות שטרם התחילו)
        //    var overlaps = _orderRepository.GetAll()
        //        .Where(o => o.CarId == carId &&
        //                    o.Status != OrderStatus.Canceled &&
        //                    o.Status != OrderStatus.Completed &&
        //                    o.StartTime.AddMinutes(-buffer) < requestedEnd &&
        //                    o.ExpectedEndTime.AddMinutes(buffer) > requestedStart)
        //        .OrderBy(o => o.StartTime)
        //        .ToList();

        //    // אם אין שום חפיפה ביומן ואין נסיעה פעילה חופפת - הרכב פנוי לגמרי
        //    if (!overlaps.Any()) return CarStatus.Available;

        //    // הגדרת זמני החסימה הכלליים עבור תצוגת ה-UI ב-React
        //    conflictStart = overlaps.First().StartTime.AddMinutes(-buffer);
        //    conflictEnd = overlaps.Last().ExpectedEndTime.AddMinutes(buffer);

        //    // 4. חישוב חלונות פנויים (Gaps) בתוך הטווח שהמשתמש ביקש
        //    var gaps = new List<(DateTime Start, DateTime End)>();
        //    DateTime cursor = requestedStart;

        //    foreach (var o in overlaps)
        //    {
        //        DateTime currentOrderStartWithBuffer = o.StartTime.AddMinutes(-buffer);

        //        // אם יש רווח בין הסמן הנוכחי לתחילת ההזמנה הבאה ביומן
        //        if (currentOrderStartWithBuffer > cursor)
        //        {
        //            gaps.Add((cursor, currentOrderStartWithBuffer));
        //        }

        //        // קידום הסמן לסוף ההזמנה הנוכחית (כולל באפר)
        //        DateTime currentOrderEndWithBuffer = o.ExpectedEndTime.AddMinutes(buffer);
        //        if (currentOrderEndWithBuffer > cursor)
        //        {
        //            cursor = currentOrderEndWithBuffer;
        //        }
        //    }

        //    // בדיקה אם נשאר חלון פנוי בין ההזמנה האחרונה ביומן לסוף הטווח המבוקש
        //    if (cursor < requestedEnd)
        //    {
        //        gaps.Add((cursor, requestedEnd));
        //    }

        //    // 5. לוגיקת הכרעה: האם החלונות הפנויים שנמצאו "איכותיים" מספיק לנסיעה?

        //    // אם אין חלונות פנויים בכלל (חפיפה מלאה)
        //    if (!gaps.Any()) return CarStatus.Occupied;

        //    // בדיקה א: האם קיים חלון רציף אחד של לפחות 60 דקות?
        //    bool hasSignificantGap = gaps.Any(g => (g.End - g.Start).TotalMinutes >= 60);

        //    // בדיקה ב: האם קיים חלון בקצוות (התחלה או סוף) של לפחות 30 דקות?
        //    // (זה מאפשר למשתמש להזיז מעט את זמן היציאה/חזרה ועדיין להזמין)
        //    bool hasUsableEdgeGap = gaps.Any(g =>
        //        (g.Start == requestedStart || g.End == requestedEnd) &&
        //        (g.End - g.Start).TotalMinutes >= 30);

        //    if (hasSignificantGap || hasUsableEdgeGap)
        //    {
        //        return CarStatus.PartiallyBooked;
        //    }

        //    // אם נמצאו רק חלונות קטנים מדי (למשל 10 דקות פה ו-10 דקות שם)
        //    return CarStatus.Occupied;
        //}
        public object GetCarAvailabilityInfo(int carId)
        {
            var car = _carRepository.GetById(carId);
            if (car == null) return null;

            var now = DateTime.Now;
            int bufferMinutes = 15; // מומלץ למשוך מקונפיגורציה במידת האפשר

            // שליפת הזמנות רלוונטיות
            var orders = _orderRepository.GetAll()
                .Where(o => o.CarId == carId &&
                            o.Status != OrderStatus.Canceled &&
                            o.Status != OrderStatus.Completed)
                .OrderBy(o => o.StartTime)
                .ToList();

            var activeOrder = orders.FirstOrDefault(o => o.Status == OrderStatus.Active);
            // הזמנה עתידית - הראשונה שמתחילה אחרי "עכשיו"
            var upcomingOrder = orders.FirstOrDefault(o => o.Status == OrderStatus.Pending && o.StartTime > now);

            CarStatus numericStatus = CarStatus.Available;
            string displayStatusText = "פנוי";
            DateTime? blockingOrderEnd = null;
            DateTime? nextAvailableStart = null;
            string note = "";

            // אסטרטגיית בדיקה:

            // 1. האם יש נסיעה פעילה כרגע?
            if (activeOrder != null)
            {
                numericStatus = CarStatus.Occupied;
                displayStatusText = "תפוס";
                blockingOrderEnd = activeOrder.ExpectedEndTime;

                // חישוב מתי יהיה פנוי באמת (סוף הזמנה + באפר)
                nextAvailableStart = activeOrder.ExpectedEndTime.AddMinutes(bufferMinutes);

                if (now > activeOrder.ExpectedEndTime)
                    note = "הרכב באיחור בהחזרה";
            }
            // 2. האם יש הזמנה שעומדת להתחיל (בתוך טווח הבאפר)?
            else if (upcomingOrder != null && upcomingOrder.StartTime <= now.AddMinutes(bufferMinutes))
            {
                numericStatus = CarStatus.Occupied;
                displayStatusText = "תפוס"; // נחשב תפוס כי אי אפשר להזמין בטווח הזה
                blockingOrderEnd = upcomingOrder.ExpectedEndTime;
                nextAvailableStart = upcomingOrder.ExpectedEndTime.AddMinutes(bufferMinutes);
                note = "מתכונן לנסיעה קרובה";
            }
            // 3. האם יש הזמנה בשעה הקרובה? (פנוי חלקית)
            else if (upcomingOrder != null && upcomingOrder.StartTime <= now.AddHours(1))
            {
                numericStatus = CarStatus.PartiallyBooked;
                displayStatusText = "פנוי חלקית";
                // הרכב יהיה חסום החל מ-15 דקות לפני תחילת ההזמנה
                blockingOrderEnd = upcomingOrder.StartTime.AddMinutes(-bufferMinutes);
                nextAvailableStart = upcomingOrder.ExpectedEndTime.AddMinutes(bufferMinutes);
                note = $"פנוי עד {blockingOrderEnd?.ToString("HH:mm")}";
            }

            // 4. הגנה: דלק ותחזוקה (דרוס סטטוסים קודמים)
            if (car.NeedsMaintenance || car.FuelLevel < 15)
            {
                numericStatus = CarStatus.Maintenance;
                displayStatusText = "בטיפול";
                note = car.FuelLevel < 15 ? "נדרש תדלוק" : "בטיפול תקופתי";
                blockingOrderEnd = null;
                nextAvailableStart = null;
            }

            return new
            {
                Id = car.Id,
                Model = car.Model,
                Address = car.StartParking,
                FuelLevel = car.FuelLevel,
                Distance = car.Kilometers, // שיניתי לשם גנרי יותר אם ה-React מצפה לזה
                Seats = car.Seats,
                ImageUrl = car.ImageUrl,
                Status = (int)numericStatus,
                StatusLabel = displayStatusText,
                Note = note,
                BlockingOrderEnd = blockingOrderEnd,
                NextAvailableStart = nextAvailableStart ?? now
            };
        }
        public CarStatus GetDetailedAvailabilityStatus(int carId, DateTime requestedStart, DateTime requestedEnd, out DateTime? conflictStart, out DateTime? conflictEnd)
        {
            conflictStart = null;
            conflictEnd = null;
            int buffer = 15;

            var car = _carRepository.GetById(carId);
            if (car == null) return CarStatus.Occupied;

            // בדיקת תקינות בסיסית
            if (car.NeedsMaintenance || car.FuelLevel < 15) return CarStatus.Maintenance;

            // שליפה אחת מרוכזת של כל מה שרלוונטי (כולל Active)
            var overlaps = _orderRepository.GetAll()
                .Where(o => o.CarId == carId &&
                            o.Status != OrderStatus.Canceled &&
                            o.Status != OrderStatus.Completed &&
                            o.StartTime.AddMinutes(-buffer) < requestedEnd &&
                            o.ExpectedEndTime.AddMinutes(buffer) > requestedStart)
                .OrderBy(o => o.StartTime)
                .ToList();

            if (!overlaps.Any()) return CarStatus.Available;

            // השמת זמני קונפליקט לממשק המשתמש - כולל הבאפר כדי שהלקוח יראה מתי הרכב באמת משתחרר
            conflictStart = overlaps.First().StartTime.AddMinutes(-buffer);
            conflictEnd = overlaps.Last().ExpectedEndTime.AddMinutes(buffer);

            // בדיקה אם יש נסיעה שמתרחשת ממש עכשיו (חשוב להתרעה למשתמש)
            var activeOrder = overlaps.FirstOrDefault(o => o.Status == OrderStatus.Active);
            if (activeOrder != null && requestedStart < activeOrder.ExpectedEndTime.AddMinutes(buffer))
            {
                // אם המשתמש מנסה להזמין זמן שמתנגש עם מישהו שכבר על הרכב
                return CarStatus.Occupied;
            }

            // חישוב חלונות פנויים
            var gaps = new List<(DateTime Start, DateTime End)>();
            DateTime cursor = requestedStart;

            foreach (var o in overlaps)
            {
                DateTime orderStartWithBuffer = o.StartTime.AddMinutes(-buffer);
                if (orderStartWithBuffer > cursor)
                {
                    gaps.Add((cursor, orderStartWithBuffer));
                }

                DateTime orderEndWithBuffer = o.ExpectedEndTime.AddMinutes(buffer);
                if (orderEndWithBuffer > cursor) cursor = orderEndWithBuffer;
            }

            if (cursor < requestedEnd)
            {
                gaps.Add((cursor, requestedEnd));
            }

            // ניתוח איכות החלונות
            bool hasLongGap = gaps.Any(g => (g.End - g.Start).TotalMinutes >= 60);
            bool hasGoodEdgeGap = gaps.Any(g =>
                (g.Start == requestedStart || g.End == requestedEnd) &&
                (g.End - g.Start).TotalMinutes >= 30);

            if (hasLongGap || hasGoodEdgeGap)
            {
                return CarStatus.PartiallyBooked;
            }

            return CarStatus.Occupied;
        }
    }
}