using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CarService : ICarService
    {
        private readonly IRepository<Car> _carRepository;

        public CarService(IRepository<Car> carRepository)
        {
            _carRepository = carRepository;
        }
        public CarDto Add(CarDto item)
        {
            var isLicenseTaken = _carRepository.GetAll().Any(c => c.LicensePlate == item.LicensePlate);
            if (isLicenseTaken)
            {
                throw new Exception("רכב עם מספר רישוי זה כבר קיים במערכת");
            }
            Car newCar = new Car
            {
                Model = item.Model,
                LicensePlate = item.LicensePlate,
                ImageUrl = item.ImageUrl,
                Seats = item.Seats,
                FuelLevel = item.FuelLevel,
                PricePerHour = item.PricePerHour,
                StartParking = item.StartParking,
                RegionId = item.RegionId,
                Status = Enum.TryParse(item.Status, out CarStatus status) ? status : CarStatus.Available
      
        };
            var saved = _carRepository.Add(newCar);
            item.Id = saved.Id;
            return item;
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
            return _carRepository.GetAll().Select(c=> new CarDto
            {
                Id = c.Id,
                Model = c.Model,
                LicensePlate = c.LicensePlate,
                ImageUrl = c.ImageUrl,
                Seats = c.Seats,
                Status = c.Status.ToString(),
                FuelLevel = c.FuelLevel,
                PricePerHour = c.PricePerHour,
                StartParking = c.StartParking,
                RegionId = c.RegionId,
                RegionName = c.Region?.Name
            }).ToList();
        }

        public CarDto? GetById(int id)
        {
           var c= _carRepository.GetById(id);
            if (c == null) return null;
            return new CarDto
            {
                Id = c.Id,
                Model = c.Model,
                LicensePlate = c.LicensePlate,
                ImageUrl = c.ImageUrl,
                Seats = c.Seats,
                Status = c.Status.ToString(),
                FuelLevel = c.FuelLevel,
                PricePerHour = c.PricePerHour,
                StartParking = c.StartParking,
                RegionId = c.RegionId,
                RegionName = c.Region?.Name
            };
        }

        public bool Update(int id, CarDto item)
        {
            var existingCar = _carRepository.GetById(id);
            if (existingCar == null) return false;

            var isLicenseTaken = _carRepository.GetAll().Any(c => c.LicensePlate == item.LicensePlate && c.Id != id);
            if (isLicenseTaken)
            {
                throw new Exception("מספר רישוי זה כבר קיים עבור רכב אחר");
            }

            existingCar.Model = item.Model;
            existingCar.LicensePlate = item.LicensePlate;
            existingCar.ImageUrl = item.ImageUrl;
            existingCar.Seats = item.Seats;
            existingCar.FuelLevel = item.FuelLevel;
            existingCar.PricePerHour = item.PricePerHour;
            existingCar.StartParking = item.StartParking;
            existingCar.RegionId = item.RegionId;

            if (Enum.TryParse(item.Status, out CarStatus status))
                existingCar.Status = status;

            return _carRepository.Update(id, existingCar);
        }
        
        public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon)
        {
            return _carRepository.GetAll()
                .AsEnumerable() // עוברים לזיכרון כדי לבצע את חישוב המרחק
                .OrderBy(c => CalculateDistance(userLat, userLon, c.Latitude, c.Longitude))
                .Select(c => new CarDto
                {
                    Id = c.Id,
                    Model = c.Model,
                    LicensePlate = c.LicensePlate,
                    ImageUrl = c.ImageUrl,
                    Seats = c.Seats,
                    Status = c.Status.ToString(), 
                    FuelLevel = c.FuelLevel,
                    PricePerHour = c.PricePerHour,
                    StartParking = c.StartParking,
                    RegionId = c.RegionId,
                    RegionName = c.Region?.Name
                }).ToList();
        }

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
    }
}
