using Common.Dto;
using Microsoft.AspNetCore.Http;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Car> _carRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(IRepository<Order> orderRepository, IRepository<Car> carRepository, IHttpContextAccessor httpContextAccessor)
        {
            _orderRepository = orderRepository;
            _carRepository = carRepository;
            _httpContextAccessor = httpContextAccessor;
        }
        public OrderDto Add(OrderDto item)
        {
            // למנוע זיופים: שליפת המשתמש המחובר מהטוקן
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = int.Parse(userIdStr ?? "0");
            if (currentUserId == 0) throw new Exception("נא להתחבר למערכת");
            //בדיקה שהרכב פנוי בזמנים המבוקשים
            var isCarBusy = _orderRepository.GetAll().Any(o =>
                            o.CarId == item.CarId &&
                            o.Status != OrderStatus.Completed &&
                            ((item.StartTime >= o.StartTime && item.StartTime < o.ExpectedEndTime) ||
                             (item.ExpectedEndTime > o.StartTime && item.ExpectedEndTime <= o.ExpectedEndTime)));
            if (isCarBusy) throw new Exception("הרכב אינו פנוי בזמנים המבוקשים");
            var car = _carRepository.GetById(item.CarId);
            if (car == null) throw new Exception("רכב לא נמצא");
            TimeSpan duration = item.ExpectedEndTime - item.StartTime;
            decimal calculatedPrice = 0;
            var pType = Enum.TryParse(item.PricingType, out PricingType type) ? type : PricingType.ByHour;
            if (pType == PricingType.ByDay)
            {
                int days = (int)Math.Ceiling(duration.TotalDays);
                calculatedPrice = days * car.PricePerDay;
            }
            else 
            {
                calculatedPrice = (decimal)duration.TotalHours * car.PricePerHour;
            }

            Order newOrder = new Order
            {
                StartTime = item.StartTime,
                ExpectedEndTime = item.ExpectedEndTime,
                UserId = currentUserId,
                CarId = item.CarId,
                BasePrice = calculatedPrice,
                TotalPrice = calculatedPrice,
                PricingType = pType,
                Status = OrderStatus.Pending,
                IsPaid = false
            };

            var saved = _orderRepository.Add(newOrder);
            item.Id = saved.Id;
            item.TotalPrice = saved.TotalPrice;
            item.UserId = currentUserId;
            item.Status = saved.Status.ToString();

            return item;
        }

        public bool Delete(int id)
        {
            var order = _orderRepository.GetById(id);
            if (order == null) return false;

            var user = _httpContextAccessor.HttpContext?.User;
            //שליפת הID מהטוקן
            var currentUserId = int.Parse(user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = user?.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole != "admin")
            {
                if (order.UserId != currentUserId || order.StartTime <= DateTime.Now)
                {
                    return false; 
                }
            }

            if (order.Status != OrderStatus.Pending)
                throw new Exception("לא ניתן לבטל הזמנה שכבר החלה או הושלמה");

            return _orderRepository.Delete(id);
        }

        public bool Exists(int id)
        {
            return _orderRepository.Exists(id);
        }

        public IEnumerable<OrderDto> GetAll()
        {         
              return _orderRepository.GetAll().Select(o => new OrderDto
              {
                  Id = o.Id,
                  StartTime = o.StartTime,
                  ExpectedEndTime = o.ExpectedEndTime,
                  TotalPrice = o.TotalPrice,
                  Status = o.Status.ToString(),
                  IsPaid = o.IsPaid,
                  UserId = o.UserId,
                  CarId = o.CarId,
                  CarModel = o.Car?.Model
              }).ToList();
        }

        public OrderDto? GetById(int id)
        {
            if(!Exists(id)) return null;

            var order = _orderRepository.GetById(id);
            if (order == null) return null;

            return new OrderDto
            {
                Id = order.Id,
                StartTime = order.StartTime,
                ExpectedEndTime = order.ExpectedEndTime,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                IsPaid = order.IsPaid,
                UserId = order.UserId,
                CarId = order.CarId,
                CarModel = order.Car?.Model
            };
        }

        public bool Update(int id, OrderDto item)
        {
            var existing = _orderRepository.GetById(id);
            if (existing == null) return false;
            //צריך להוסיף פה דברים אבל בהמשך...
            existing.Status = Enum.TryParse(item.Status, out OrderStatus status) ? status : existing.Status;
            existing.IsPaid = item.IsPaid;

            return _orderRepository.Update(id, existing);
        }
    }
}
