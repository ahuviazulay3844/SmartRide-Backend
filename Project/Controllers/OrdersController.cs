using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Entities;
using Service.Interfaces;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService orderService;

        public OrdersController(IOrderService service)
        {
            this.orderService = service;
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IEnumerable<OrderDto> Get()
        {
            return orderService.GetAll();
        }
        [HttpGet("{id}")]
        [Authorize]
        public OrderDto? Get(int id)
        {
            var order = orderService.GetById(id);
            if (order == null) return null;
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (currentUserRole != "admin" && order.UserId != currentUserId)
            {
                //return null;    
                throw new UnauthorizedAccessException("אין לך הרשאות לגשת להזמנה זו");
            }
            return order;
        }

        [HttpPost]
        [Authorize(Roles = "user")]
        public OrderDto Post([FromBody] OrderDto item)
        {
            return orderService.Add(item);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "user")]
        public bool Put(int id, [FromBody] OrderDto item)
        {
            return orderService.Update(id, item);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public bool Delete(int id)
        {
           
            return orderService.Delete(id);
        }
    }
}
