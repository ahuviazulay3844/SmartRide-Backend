using Common.Dto;
using Repository.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IOrderService: IService<OrderDto>
    {
            Task<bool> FinishOrder(int orderId, int reportedMileage, int fuelTime);
            bool IsCarBusy(OrderDto item);
            decimal CalculateOrderPrice(OrderDto order);
            void UpdateTripProgress(int orderId);
            bool UnlockCar(int orderId); 
            bool LockCar(int orderId);   
            Task<bool> ReportStartCondition(int orderId, bool isDirty, bool isDamaged, string comments); 
            bool SimulateDrive(int orderId, int kmToAdd);
            int GetTotalOrdersCount();
            OrderDto GetActiveOrder();
            decimal GetTotalRevenue(DateTime? start, DateTime? end);
            IEnumerable<OrderDto> GetOrdersByDate(DateTime date);
            bool MarkAsPaid(int orderId);
            bool CancelOrder(int orderId);
            IEnumerable<OrderDto> GetOrdersByDateRange(DateTime start, DateTime end);
            IEnumerable<OrderDto> GetOrdersByUserEmail(string email);
            IEnumerable<OrderDto> GetOrdersByCarNumber(string carNumber);
            IEnumerable<OrderDto> GetOrdersByUserId(int userId);
    }
}
