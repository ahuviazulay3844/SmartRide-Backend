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
              
                bool isCarBlockedByLateUser = _orderService.GetAll()
                    .Any(o => o.CarId == order.CarId &&
                              o.Status == OrderStatus.Active &&
                              o.Id != order.Id);

             
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
          
            var activeOrders = _orderService.GetAll()
                .Where(o => o.Status == OrderStatus.Active)
                .ToList();

            if (!activeOrders.Any()) return;

            foreach (var order in activeOrders)
            {
                try
                {
                   
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
                                         !o.HasConflict && 
                                         !o.IsReassigned);

                if (waitingOrder != null)
                {
                    await _orderService.ProcessLateCustomerConflict(lateOrder.CarId);
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
   
