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
        }

        private async Task UpdatePendingOrders(DateTime now)
        {
            // מוצא את כל ההזמנות הממתינות שהגיע זמן תחילתן
            var toActivate = _orderService.GetAll()
                .Where(o => o.Status == OrderStatus.Pending && o.StartTime <= now)
                .ToList();

            foreach (var order in toActivate)
            {
                // 1. עדכון סטטוס ההזמנה ל-Active
                await _orderService.UpdateStatusAsync(order.Id, OrderStatus.Active);

                // 2. עדכון סטטוס הרכב ל-Occupied (כדי שייעלם מהמפה ללקוחות אחרים)
                var car = _carService.GetById(order.CarId);
                if (car != null && car.Status != CarStatus.Occupied)
                {
                    car.Status = CarStatus.Occupied;
                    _carService.Update(car.Id, car);
                }
                Console.WriteLine($"[Worker] Order {order.Id} activated. Car {order.CarId} is now Occupied.");
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
                    int kmAdded = _orderService.UpdateTripProgress(order.Id);

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
    }
    }
