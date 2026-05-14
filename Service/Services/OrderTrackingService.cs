using Service.Interfaces;
using Common.Dto;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Services
{
    public class OrderTrackingService
    {
        private readonly IOrderService _orderService;
        private readonly ICarService _carService;

        public OrderTrackingService(IOrderService orderService, ICarService carService)
        {
            _orderService = orderService;
            _carService = carService;
        }

        public async Task RunOnce()
        {
            var now = DateTime.Now;
            Console.WriteLine($"[Worker Check] {now}: Processing cycles...");

            await UpdatePendingOrders(now);        // טיפול בהזמנות שצריכות להתחיל
            await UpdateActiveTripsProgress();     // סימולציית תנועה/קילומטראז'
            await AutoFinishExpiredOrders(now);    // סגירת הזמנות שנסתיימו
            await HandleBufferingAndConflicts(now);
        }

        private async Task UpdatePendingOrders(DateTime now)
        {
            var toActivate = _orderService.GetAll()
                .Where(o => o.Status == OrderStatus.Pending && o.StartTime <= now)
                .ToList();

            foreach (var order in toActivate)
            {
                // 1. האם יש משתמש קודם שעדיין לא סיים?
                bool isCarBlockedByLateUser = _orderService.GetAll()
                    .Any(o => o.CarId == order.CarId &&
                              o.Status == OrderStatus.Active &&
                              o.Id != order.Id);

                // 2. האם הרכב בסטטוס שמונע נסיעה (למשל בטיפול)?
                var car = _carService.GetById(order.CarId);
                bool isCarInMaintenance = car?.Status == CarStatus.Maintenance;

                if (isCarBlockedByLateUser || isCarInMaintenance)
                {
                    // מפעיל את תהליך הצעת רכב חלופי
                    await _orderService.ProcessLateCustomerConflict(order.CarId);

                    Console.WriteLine($"[Worker] Order {order.Id} deferred. Reason: " +
                        (isCarBlockedByLateUser ? "Car is still busy" : "Car is in maintenance"));

                    continue;
                }

                // 3. הכל תקין - מפעילים את הנסיעה
                await _orderService.UpdateStatusAsync(order.Id, OrderStatus.Active);

                if (car != null)
                {
                    car.Status = CarStatus.Occupied;
                    _carService.Update(car.Id, car);
                }

                Console.WriteLine($"[Worker] Order {order.Id} is now Active.");
            }
        }
        private async Task UpdateActiveTripsProgress()
        {
            // חשוב: לוודא ש-GetAll() מחזיר נתונים עדכניים מה-DB ולא מה-Cache
            var activeOrders = _orderService.GetAll()
                .Where(o => o.Status == OrderStatus.Active)
                .ToList();

            if (!activeOrders.Any()) return;

            foreach (var order in activeOrders)
            {
                try
                {
                    // נשמור את ה-KM שהיה לפני העדכון כדי לדעת אם באמת היה שינוי
                    int kmAdded =await _orderService.UpdateTripProgress(order.Id);

                    if (kmAdded > 0)
                    {
                        Console.WriteLine($"[Worker] Order {order.Id}: Updated {kmAdded}km successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Worker Error] Failed to update order {order.Id}: {ex.Message}");
                }
            }
        }

        private async Task AutoFinishExpiredOrders(DateTime now)
        {
            var overdue = _orderService.GetAll()
                .Where(o => o.Status == OrderStatus.Active && o.ExpectedEndTime < now)
                .ToList();

            foreach (var order in overdue)
            {
                // כאן אתה יכול להוסיף שליחת מייל התראה: "אתה באיחור!"
                // אל תריץ FinishOrder()! ככה ההזמנה נשארת פעילה והמשתמש רואה שהזמן עבר.
                Console.WriteLine($"[Worker] Order {order.Id} is late. Alarm activated in UI.");
            }
        }
        private async Task HandleBufferingAndConflicts(DateTime now)
        {
            // 1. שליפת כל ההזמנות הרלוונטיות פעם אחת (כדי למנוע פניות מיותרות ל-DB בלולאה)
            var allOrders = _orderService.GetAll().ToList();

            // 2. איתור הזמנות "בעייתיות": הן פעילות, הזמן שלהן עבר, והן עוד לא הסתיימו (EndTime הוא NULL)
            // הערה: הורדתי את ה-15 דקות ל-2 דקות כדי שהמערכת תגיב מהר יותר לאיחור
            var lateOrders = allOrders
                .Where(o => o.Status == OrderStatus.Active &&
                            o.EndTime == null &&
                            o.ExpectedEndTime <= now.AddMinutes(-2))
                .ToList();

            foreach (var lateOrder in lateOrders)
            {
                // 3. איתור "הקורבן" (User B): מישהו שההזמנה שלו צריכה להתחיל בקרוב (או כבר התחילה)
                // על אותו רכב בדיוק, ועדיין לא טיפלנו בו (IsReassigned == false)
                var waitingOrder = allOrders
                    .FirstOrDefault(o => o.CarId == lateOrder.CarId &&
                                         o.Status == OrderStatus.Pending &&
                                         o.StartTime <= lateOrder.ExpectedEndTime.AddMinutes(30) && // חלון זמן רלוונטי
                                         !o.IsReassigned &&
                                         o.Id != lateOrder.Id);

                if (waitingOrder != null)
                {
                    // בדיקה נוספת: האם כבר סימנו שיש קונפליקט? אם לא, נסמן ונדפיס
                    if (!waitingOrder.HasConflict)
                    {
                        Console.WriteLine($"[Worker] Conflict detected! Order {waitingOrder.Id} is blocked by late User in Order {lateOrder.Id}");
                        // אפשר לעדכן כאן דגל ב-DB שהמשתמש יראה "הרכב מאחר" באפליקציה
                    }

                    // 4. ניסיון ההחלפה הקריטי
                    // אנחנו שולחים את ה-CarId של הרכב התפוס כדי שהפונקציה תמצא רכב חלופי ל-waitingOrder
                    bool success = await _orderService.ProcessLateCustomerConflict(lateOrder.CarId);

                    if (success)
                    {
                        Console.WriteLine($"[Worker] SUCCESS: Order {waitingOrder.Id} reassigned to a new car.");
                    }
                    else
                    {
                        // אם נכשל - אולי כדאי לשלוח התראה למנהל המערכת או למשתמש
                        Console.WriteLine($"[Worker] FAIL: No alternative car found for Order {waitingOrder.Id}. User is still stuck.");
                    }
                }
            }
        }
    }
    }
