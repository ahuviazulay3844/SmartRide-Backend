using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    //לסדר את הענין שלא יהיה אופציה ליוזר להסתכלבהזמנה של לקוח אחר 
    [HttpGet("{id}")]
    [Authorize]
    public OrderDto? Get(int id)
    {
        return orderService.GetById(id);
    }
   
    [HttpPost]
    [Authorize(Roles = "user")]
    public OrderDto Post([FromBody]OrderDto item)
    {
        return orderService.Add(item);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "user")]
    public bool Put(int id, [FromBody]OrderDto item)
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