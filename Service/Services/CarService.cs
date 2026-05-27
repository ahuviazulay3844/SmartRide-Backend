
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
using static System.Runtime.InteropServices.JavaScript.JSType;
using CarStatus = Common.Dto.CarStatus;
using OrderStatus = Repository.Entities.OrderStatus;

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
            if (isLicenseTaken) { return false; }
            _mapper.Map(item, existingCar);
            return _carRepository.Update(id, existingCar);
        }

        // Haversine formula for calculating aerial distance in kilometers
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

        public IEnumerable<CarDto> GetAllClosest(double userLat, double userLon, DateTime? start = null, DateTime? end = null)
        {
            //Creates an object that represents the official time zone of the State of Israel

            var israelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

            DateTime? localStart = start.HasValue ? TimeZoneInfo.ConvertTime(start.Value, israelTimeZone) : (DateTime?)null;
            DateTime? localEnd = end.HasValue ? TimeZoneInfo.ConvertTime(end.Value, israelTimeZone) : (DateTime?)null;

            var cars = _carRepository.GetAll().ToList();
            var allActiveOrders = _orderRepository.GetAll()
                .Where(o => o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed)
                .ToList();

            return cars.Select(c =>
            {
                var dto = _mapper.Map<CarDto>(c);
                dto.Distance = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude);

                if (localStart.HasValue && localEnd.HasValue)
                {
                    // Send the list of orders already in memory to avoid repeated calls to the DB
                    dto.Status = GetDetailedAvailabilityStatus(c, localStart.Value, localEnd.Value, allActiveOrders, out var startC, out var endC);

                    if (dto.Status != CarStatus.Available)
                    {
                        dto.BlockingOrderStart = startC;
                        dto.BlockingOrderEnd = endC;
                    }
                }
                else { dto.Status = (CarStatus)c.Status; }
                return dto;
            }).OrderBy(d => d.Distance).ToList();
        }

        public Common.Dto.CarStatus GetDetailedAvailabilityStatus(Car car, DateTime requestedStart, DateTime requestedEnd, List<Order> allOrders, out DateTime? conflictStart, out DateTime? conflictEnd)
        {
            conflictStart = null;
            conflictEnd = null;
            int buffer = 15;

            if (!IsCarFitForRoad(car.Id))
            {
                return Common.Dto.CarStatus.Maintenance;
            }
            //find this car
            var relevantOrders = allOrders
                .Where(o => o.CarId == car.Id || o.SuggestedReplacementCarId == car.Id)
                .Select(o => new
                {
                    ActualStart = o.StartTime,
                    ActualEndWithBuffer = (o.Status == OrderStatus.Active && DateTime.Now > o.ExpectedEndTime
                                           ? DateTime.Now
                                           : o.ExpectedEndTime).AddMinutes(buffer)
                })
                // Filters and only leaves orders that mathematically overlap the requested time range (creating a time conflict)
                .Where(x => x.ActualStart < requestedEnd && x.ActualEndWithBuffer > requestedStart)
                .OrderBy(x => x.ActualStart)
                .ToList();

            if (!relevantOrders.Any())
                return Common.Dto.CarStatus.Available;

            //Defining the edges of the conflicts - useful for the "partially booked" logic and for the UI to show the user when the car is actually free
            conflictStart = relevantOrders.First().ActualStart;
            conflictEnd = relevantOrders.Last().ActualEndWithBuffer;
           
            bool gapAtStart = (relevantOrders.First().ActualStart - requestedStart).TotalMinutes >= 60;
            bool gapAtEnd = (requestedEnd - relevantOrders.Last().ActualEndWithBuffer).TotalMinutes > 0;

            if (gapAtStart || gapAtEnd)
            {
                return Common.Dto.CarStatus.PartiallyBooked;
            }

            return Common.Dto.CarStatus.Occupied;
        }
        public object GetCarAvailabilityInfo(int carId)
        {
            var car = _carRepository.GetById(carId);
            if (car == null) return null;
            var now = DateTime.Now;
            int buffer = 15;

            var orders = _orderRepository.GetAll()
                .Where(o => o.CarId == carId && o.Status != OrderStatus.Canceled && o.Status != OrderStatus.Completed)
                .OrderBy(o => o.StartTime)
                .ToList();

            //  Trying to find out if there is an order going on right now And who is the next order that is about to start?.
            var currentOrder = orders.FirstOrDefault(o => now >= o.StartTime && now < o.ExpectedEndTime.AddMinutes(buffer));
            var upcomingOrder = orders.FirstOrDefault(o => o.StartTime >= now);

            CarStatus numericStatus = CarStatus.Available;
            DateTime? nextAvailable = now;
            DateTime? nextOrderStart = upcomingOrder?.StartTime;

            if (currentOrder != null)
            {
                numericStatus = CarStatus.Occupied;
                nextAvailable = currentOrder.ExpectedEndTime.AddMinutes(buffer);
            }

            else if (upcomingOrder != null)
            {
                if ((upcomingOrder.StartTime - now).TotalMinutes < 60)
                {
                    numericStatus = CarStatus.PartiallyBooked;
                }
            }
            if (!IsCarFitForRoad(car.Id))
            {
                numericStatus = CarStatus.Maintenance;
            }    
            
            return new
            {
                Id = car.Id,
                Model = car.Model,
                Address = car.StartParking,
                FuelLevel = car.FuelLevel,
                Distance = car.Kilometers,
                Seats = car.Seats,
                ImageUrl = car.ImageUrl,
                Status = (int)numericStatus,
                NextAvailableStart = nextAvailable, 
                NextOrderStart = nextOrderStart,   
                IsLate = currentOrder != null && now > currentOrder.ExpectedEndTime // האם הנהג מאחר
            };
        }
        public bool SendToMaintenance(int carId)
        {
            var car = _carRepository.GetById(carId);
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
            var carsNeedingFuel = _carRepository.GetAll().Where(c => c.FuelLevel < 15).ToList();
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
            if (car == null || car.NeedsMaintenance || car.FuelLevel < 15)
            {
                return false;
            }
            return true; 
        }
        public IEnumerable<CarDto> GetAvailableCars(DateTime start, DateTime end, int regionId)
        {
            double tripDurationHours = (end - start).TotalHours;
            var busyCarIds = _orderRepository.GetAll()
                .Where(order => order.Status != OrderStatus.Completed &&
                                start < order.ExpectedEndTime &&
                                end > order.StartTime)
                .Select(order => order.CarId)
                .ToList();
            var availableCars = _carRepository.GetAll()
         .Where(c => c.RegionId == regionId && !c.NeedsMaintenance)
         .AsEnumerable()
         .Where(car => !busyCarIds.Contains(car.Id) &&
                       !(tripDurationHours > 4 && car.FuelLevel < 40) &&
                       !(car.FuelLevel < 15))
         .ToList();
            return availableCars.Select(car =>
            {
                var dto = _mapper.Map<CarDto>(car);
                dto.Status = Common.Dto.CarStatus.Available;

                return dto;
            });
        }
        public string CheckCarSuitability(int carId, DateTime start, DateTime end)
        {
            var car = _carRepository.GetById(carId);
            if (car == null) return "רכב לא נמצא";
            if (car.NeedsMaintenance) return "הרכב נמצא בתחזוקה";
            if (car.FuelLevel < 15) return "אין מספיק דלק לנסיעה מינימלית";

            double hours = (end - start).TotalHours;
            if (hours > 4 && car.FuelLevel < 40) return "לנסיעה ארוכה נדרש לפחות 40% דלק";
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

        public bool UpdateLockStatus(int carId, bool isLocked)
        {
            var car = _carRepository.GetById(carId);
            if (car == null) return false;
            car.IsLocked = isLocked;

            car.LastLockTime = DateTime.Now;

            return _carRepository.Update(carId, car);
        }    

   
    }
}
