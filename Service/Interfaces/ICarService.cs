using Common.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Service.Interfaces
{
    public interface ICarService: IService<CarDto>
    {
        IEnumerable<CarDto> GetAllClosest(double latitude, double longitude);    
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
    }
}
