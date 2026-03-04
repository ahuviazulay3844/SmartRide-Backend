using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Entities;
using Service.Interfaces;
using Service.Services;
using System.Security.Claims;

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
        public IActionResult Get()
        {
            return Ok(orderService.GetAll());
        }

        [HttpGet("{id}")]
        [Authorize]
        public IActionResult Get(int id)
        {
            var order = orderService.GetById(id);
            if (order == null)
                return NotFound(new { message = "הזמנה לא נמצאה" });

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole != "admin" && order.UserId != currentUserId)
            {
                return StatusCode(403, new { message = "אינך מורשה לצפות בפרטי הזמנה שאינה שלך" });
            }

            return Ok(order);
        }

        [HttpPost]
        [Authorize(Roles = "user")]
        public IActionResult Post([FromBody] OrderDto item)
        {
            var createdOrder = orderService.Add(item);
            if (createdOrder == null)
                return BadRequest(new { message = "הרכב אינו פנוי בזמנים אלו" });

            return Created("", new { message = "ההזמנה בוצעה בהצלחה", data = createdOrder });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "user")]
        public IActionResult Put(int id, [FromBody] OrderDto item)
        {
            var result = orderService.Update(id, item);
            if (!result)
                return BadRequest(new { message = "עדכון ההזמנה נכשל" });

            return Ok(new { message = "ההזמנה עודכנה בהצלחה" });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {

            var result = orderService.Delete(id);
            if (!result)
                return BadRequest(new { message = "לא ניתן לבטל הזמנה זו (אולי היא כבר החלה?)" });

            return Ok(new { message = "ההזמנה בוטלה בהצלחה" });
        }

        [HttpPut("{id}/start-condition")]
        [Authorize(Roles = "user")]
        public IActionResult ReportStartCondition(int id, bool isDirty, bool isDamaged, string comments)
        {
            var result = orderService.ReportStartCondition(id, isDirty, isDamaged, comments);
            if (!result)
            {
                return BadRequest(new { message = "דיווח נכשל: ההזמנה לא נמצאה או שאינה במצב המתנה" });
            }
            return Ok(new { message = "הדיווח התקבל בהצלחה, הרכב נפתח" });
        }

        [HttpPost("{id}/unlock")]
        [Authorize(Roles = "user")]
        public IActionResult Unlock(int id)
        {
            var result = orderService.UnlockCar(id);
            if (!result) return BadRequest(new { message = "לא ניתן לפתוח את הרכב" });
            return Ok(new { message = "הרכב נפתח" });
        }

        [HttpPut("{id}/lock")]
        [Authorize(Roles = "user")]
        public IActionResult Lock(int id)
        {
            var result = orderService.LockCar(id);
            if (!result) return BadRequest(new { message = "לא ניתן לנעול את הרכב" });
            return Ok(new { message = "הרכב ננעל" });
        }

        [HttpPatch("{id}/finish")]
        [Authorize(Roles = "user")]
        public IActionResult Finish(int id, [FromQuery] int mileage, [FromQuery] int fuelTime)
        {
            var result = orderService.FinishOrder(id, mileage, fuelTime);
            if (!result) return BadRequest(new { message = "סיום ההזמנה נכשל" });
            return Ok(new { message = "הנסיעה הסתיימה בהצלחה" });
        }

        [HttpPost("{id}/update-progress")]
        [Authorize(Roles = "user")]
        public IActionResult UpdateProgress(int id)
        {
            orderService.UpdateTripProgress(id);
            return Ok(new { message = "התקדמות הנסיעה עודכנה" });
        }
        [HttpGet("count")]
        [Authorize(Roles = "admin")]
        public IActionResult GetTotalOrdersCount()
        {
            return Ok(new { count = orderService.GetTotalOrdersCount() });
        }

        [HttpGet("active")]
        [Authorize]
        public IActionResult GetActiveOrder()
        {
            var order = orderService.GetActiveOrder();
            if (order == null)
                return NotFound(new { message = "לא נמצאה הזמנה פעילה עבורך" });
            return Ok(order);
        }

        [HttpGet("revenue")]
        [Authorize(Roles = "admin")]
        public IActionResult GetTotalRevenue([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            var revenue = orderService.GetTotalRevenue(start, end);
            return Ok(new { totalRevenue = revenue });
        }

        [HttpGet("by-date/{date}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetOrdersByDate(DateTime date)
        {
            return Ok(orderService.GetOrdersByDate(date));
        }

        [HttpPatch("mark-as-paid/{orderId}")]
        [Authorize]
        public IActionResult MarkAsPaid(int orderId)
        {
            var result = orderService.MarkAsPaid(orderId);
            if (!result)
                return BadRequest(new { message = "עדכון תשלום נכשל - הזמנה לא נמצאה" });
            return Ok(new { message = "ההזמנה סומנה כנפרעת בהצלחה" });
        }

        [HttpPatch("cancel/{orderId}")]
        [Authorize]
        public IActionResult CancelOrder(int orderId)
        {
            var result = orderService.CancelOrder(orderId);
            if (!result)
            {
                return BadRequest(new { message = "ביטול ההזמנה נכשל (ייתכן שאינך מורשה או שההזמנה כבר החלה)" });
            }
            return Ok(new { message = "ההזמנה בוטלה בהצלחה" });
        }

        [HttpGet("range")]
        [Authorize(Roles = "admin")]
        public IActionResult GetOrdersByDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            return Ok(orderService.GetOrdersByDateRange(start, end));
        }

        [HttpGet("by-email/{email}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetOrdersByUserEmail(string email)
        {
            return Ok(orderService.GetOrdersByUserEmail(email));
        }

        [HttpGet("by-car/{carNumber}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetOrdersByCarNumber(string carNumber)
        {
            return Ok(orderService.GetOrdersByCarNumber(carNumber));
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetOrdersByUserId(int userId)
        {
            return Ok(orderService.GetOrdersByUserId(userId));
        }
    }
}

