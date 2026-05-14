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
    public interface IOrderService : IService<OrderDto>
    {
        Task<bool> FinishOrder(int orderId, int reportedMileage, int fuelTime);
        bool IsCarBusy(OrderDto item);
        decimal CalculateOrderPrice(OrderDto order);
        Task<int> UpdateTripProgress(int orderId);
        //Task<bool> UnlockCar(int orderId);
        bool UnlockCar(int orderId);
        bool LockCar(int orderId);
        Task<bool> ReportStartCondition(int orderId, CarInspectionDto dto);
        bool SimulateDrive(int orderId, int kmToAdd);
        Task<bool> UpdateStatusAsync(int id, Common.Dto.OrderStatus newStatus);
        int GetTotalOrdersCount();
        OrderDto GetActiveOrder();
        decimal GetTotalRevenue(DateTime? start, DateTime? end);
        IEnumerable<OrderDto> GetOrdersByDate(DateTime date);
        Task<bool> MarkAsPaid(int orderId);
        bool CancelOrder(int orderId);
        IEnumerable<OrderDto> GetOrdersByDateRange(DateTime start, DateTime end);
        IEnumerable<OrderDto> GetOrdersByUserEmail(string email);
        IEnumerable<OrderDto> GetOrdersByCarNumber(string carNumber);
        IEnumerable<OrderDto> GetOrdersByUserId(int userId);
        object GetCarAvailabilityInfo(int carId);
        bool IsUserOverlap(int userId, DateTime start, DateTime end);
        Task<bool> ProcessLateCustomerConflict(int carId);
        Task<bool> RequestExtension(int orderId);
        bool ConfirmReplacement(int orderId, bool accept);
        bool ReportRefuel(int orderId);
    }
}
