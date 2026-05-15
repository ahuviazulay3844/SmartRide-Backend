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
        .Where(o => o.Status == OrderStatus.Pending && o.StartTime <= now && !o.HasConflict) 
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
            var allOrders = _orderService.GetAll().ToList();

            var lateOrders = allOrders
                .Where(o => o.Status == OrderStatus.Active && o.EndTime == null && o.ExpectedEndTime <= now.AddMinutes(-2))
                .ToList();

            foreach (var lateOrder in lateOrders)
            {
                var waitingOrder = allOrders
                    .FirstOrDefault(o => o.CarId == lateOrder.CarId &&
                                         o.Status == OrderStatus.Pending &&
                                         !o.HasConflict && // --- חשוב: אל תחפש אם כבר הצענו החלפה! ---
                                         !o.IsReassigned);

                if (waitingOrder != null)
                {
                    await _orderService.ProcessLateCustomerConflict(lateOrder.CarId);
                }
            }
        }
        //private async Task HandleBufferingAndConflicts(DateTime now)
        //{
        //    var allOrders = _orderService.GetAll().ToList();

        //    // 1. מי המאחרים (User A)?
        //    var lateOrders = allOrders
        //        .Where(o => o.Status == OrderStatus.Active && o.EndTime == null && o.ExpectedEndTime < now)
        //        .ToList();

        //    foreach (var lateOrder in lateOrders)
        //    {
        //        // 2. מי מחכה לרכב הספציפי הזה (User B)?
        //        // תנאי: רק אם הוא Pending ועוד לא קיבל הצעה (HasConflict == false)
        //        var waitingOrder = allOrders
        //            .FirstOrDefault(o => o.CarId == lateOrder.CarId &&
        //                                 o.Status == OrderStatus.Pending &&
        //                                 !o.HasConflict);

        //        if (waitingOrder != null)
        //        {
        //            // מפעילים את הקונפליקט על waitingOrder
        //            await _orderService.ProcessLateCustomerConflict(lateOrder.CarId);
        //        }
        //    }
        //}

    }
        }
   
