using Common.Dto;
using Repository.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Service.Interfaces
{
    public interface ICarService: IService<CarDto>
    {
        
        bool SendToMaintenance(int carId);
        bool ReleaseFromMaintenance(int carId);
        IEnumerable<CarDto> GetVehiclesNeedingFuel();
        IEnumerable<CarDto> GetAllPopularCars(int count=5);
        bool UpdateFuelLevel(int carId, int newLevel);
        IEnumerable<CarDto> GetAvailableCarsByRegion(int regionId);
        IEnumerable<CarDto> GetAvailableCars(DateTime start, DateTime end, int regionId);
        bool UpdateMileage(int carId, int newMileage);
        bool UpdateStatus(int carId, string status);
        bool IsCarFitForRoad(int carId);
        string CheckCarSuitability(int carId, DateTime start, DateTime end);
        IEnumerable<CarDto> GetByStatus(string status);
        object GetCarAvailabilityInfo(int id);
        bool UpdateLockStatus(int carId, bool isLocked);
        Common.Dto.CarStatus GetDetailedAvailabilityStatus(Car car, DateTime requestedStart, DateTime requestedEnd, List<Order> allOrders, out DateTime? conflictStart, out DateTime? conflictEnd);
        IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null);
    }
}
