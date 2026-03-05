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
              return  false;
            }

            _mapper.Map(item, existingCar);
            return _carRepository.Update(id, existingCar);
        }
        
        public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon)
        {
            var sortedCars = _carRepository.GetAll()
                .AsEnumerable() // עוברים לזיכרון כדי לבצע את חישוב המרחק
                .OrderBy(c => CalculateDistance(userLat, userLon, c.Latitude, c.Longitude));
                return _mapper.Map<IEnumerable<CarDto>>(sortedCars);
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

        public bool SendToMaintenance(int carId)
        {
          var car= _carRepository.GetById(carId);
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
            var carsNeedingFuel=_carRepository.GetAll().Where(c=>c.FuelLevel<15).ToList(); 
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
                .AsEnumerable() // עוברים לזיכרון לחישובים המורכבים
                .Where(car =>
                    !busyCarIds.Contains(car.Id) && // הרכב לא ברשימת התפוסים
                    !(tripDurationHours > 4 && car.FuelLevel < 40) &&
                    !(car.FuelLevel < 15)
                ).ToList();

            return _mapper.Map<IEnumerable<CarDto>>(availableCars);
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
    }
}
