using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Entities;
using Service.Interfaces;
using Service.Services;
using System.Security.Claims;
using System.Threading.Tasks;

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

        // [HttpPost]
        //// [Authorize(Roles = "user")]
        // public async Task<IActionResult> Post([FromBody] OrderDto item)
        // {
        //     if (item.StartTime < DateTime.Now.AddMinutes(-5))
        //     {
        //         return BadRequest(new { message = "לא ניתן להזמין לתאריך ושעה שעברו" });
        //     }

        //     var createdOrder = await orderService.Add(item);
        //     if (createdOrder == null)
        //         return BadRequest(new { message = "הרכב אינו פנוי בזמנים אלו" });

        //     return Created("", new { message = "ההזמנה בוצעה בהצלחה", data = createdOrder });
        // }
        [HttpPost]
        //// [Authorize(Roles = "user")]
        public async Task<IActionResult> Post([FromBody] OrderDto item)
        {
            // הגדלת הטווח ל-30 דקות אחורה כדי למנוע חסימה של משתמשים איטיים
            
            var createdOrder = await orderService.Add(item);
            if (item.StartTime < DateTime.Now.AddMinutes(-30))
            {
                return BadRequest(new { message = "זמן ההתחלה חרג מטווח הסבלנות המותר. אנא עדכן לזמן נוכחי." });
            }

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


        [HttpPost("{id}/unlock")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Unlock(int id)
        {
            // הוספנו await כדי לקבל את התוצאה האמיתית (bool) ולא את ה-Task
            var result =  orderService.UnlockCar(id);

            if (!result)
                return BadRequest(new { message = "לא ניתן לפתוח את הרכב. וודא שהנסיעה פעילה ושזה הרכב הנכון." });

            return Ok(new { message = "הרכב נפתח בהצלחה" });
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
        public async Task<IActionResult> Finish(int id, [FromQuery] int mileage, [FromQuery] int fuelTime)
        {
            var result = await orderService.FinishOrder(id, mileage, fuelTime);

            if (!result)
            {
                return BadRequest(new { message = "סיום ההזמנה נכשל - וודא שנתוני הקילומטראז' תקינים וההזמנה קיימת" });
            }

            return Ok(new { message = "הנסיעה הסתיימה בהצלחה, תודה שהשתמשת ב-Smart-Ride!" });
        }

        //[HttpPost("{id}/update-progress")]
        //[Authorize(Roles = "user")]
        //public IActionResult UpdateProgress(int id)
        //{
        //    orderService.UpdateTripProgress(id);
        //    return Ok(new { message = "התקדמות הנסיעה עודכנה" });
        //}
        [HttpPost("{id}/update-progress")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> UpdateProgress(int id)
        {
            int currentTotalKm =await orderService.UpdateTripProgress(id);

            return Ok(new
            {
                message = "התקדמות הנסיעה עודכנה בבסיס הנתונים",
                totalAccumulatedKm = currentTotalKm
            });
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
        public async Task<IActionResult> MarkAsPaid(int orderId)
        {
            var result = await orderService.MarkAsPaid(orderId);
            if (!result)
            {
                return BadRequest(new { message = "עדכון תשלום נכשל - הזמנה לא נמצאה" });
            }
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
        [Authorize]
        public IActionResult GetOrdersByUserId(int userId)
        {
            return Ok(orderService.GetOrdersByUserId(userId));
        }


        //[HttpPost("{id}/submit-start-report")]
        //[Authorize]
        //public async Task<IActionResult> SubmitStartReport(int id, [FromQuery] bool isDirty, [FromQuery] bool isDamaged, [FromQuery] string comments)
        //{
        //    var success = await orderService.ReportStartCondition(id, isDirty, isDamaged, comments);
        //    if (!success)
        //    {
        //        return BadRequest(new { message = "חלה שגיאה בעיבוד הדיווח. וודא שאתה ליד הרכב." });
        //    }
        //    return Ok(new { message = "הדיווח התקבל בהצלחה! הרכב נפתח, נסיעה טובה." });
        //}
        //[HttpGet("track/{orderId}")]
        //public IActionResult GetTripProgress(int orderId)
        //{
        //    // שואלים את ה-Service מה המרחק העדכני שרשום בדאטה-בייס
        //    var order = orderService.GetOrdersByUserId(orderId);

        //    if (order != null)
        //    {
        //        return Ok(new { accumulatedKm = order.DistanceDrivenKm });
        //    }

        //    return Ok(new { accumulatedKm = 0 });
        //}
        [HttpPost("{id}/submit-start-report")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> SubmitStartReport(int id, [FromBody] CarInspectionDto report)
        {
            // קריאה ל-Service עם ה-DTO החדש שכולל פנצ'ר
            var success = await orderService.ReportStartCondition(id, report);

            if (!success)
            {
                return BadRequest(new { message = "חלה שגיאה בעיבוד הדיווח. וודא שההזמנה קיימת." });
            }
            return Ok(new { message = "הדיווח התקבל בהצלחה! הרכב נפתח, נסיעה טובה." });
        }

        //// פונקציה 2: מקבלת אובייקט שלם מהטופס (Body)
        //[HttpPost("{id}/submit-start-report")] // השארתי את הכתובת המקורית
        //[Authorize(Roles = "user")]
        //public async Task<IActionResult> SubmitStartReportBody(int id, [FromBody] CarInspectionDto report)
        //{
        //    var success = await orderService.ProcessStartInspection(id, report);

        //    if (!success)
        //        return BadRequest(new { message = "חלה שגיאה בעיבוד הדיווח או שההזמנה לא קיימת" });

        //    return Ok(new { message = "הדיווח התקבל, הרכב נפתח. נסיעה טובה!" });
        //}
        // בתוך OrdersController.cs
        [HttpGet("check-user-overlap")]
        public IActionResult CheckUserOverlap([FromQuery] int userId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            // ה-Controller רק קורא ל-Service ומעביר את התשובה הלאה
            var hasOverlap = orderService.IsUserOverlap(userId, start, end);

            return Ok(new { hasOverlap });
        }
        [HttpPost("extend/{id}")]
        public async Task<IActionResult> ExtendOrder(int id)
        {
            // חייבים להוסיף await כדי לחכות לתוצאה האמיתית (bool)
            var success = await orderService.RequestExtension(id);

            if (!success)
            {
                return BadRequest("לא ניתן להאריך את ההזמנה. ייתכן שהרכב תפוס על ידי לקוח אחר או שהנסיעה אינה פעילה.");
            }

            return Ok(true);
        }
        [HttpPost("{id}/confirm-replacement")]
        [Authorize]
        public IActionResult ConfirmReplacement(int id, [FromQuery] bool accept)
        {
            // קריאה לפעולה שיצרנו ב-OrderService
            var result = orderService.ConfirmReplacement(id, accept);

            if (!result) return BadRequest(new { message = "לא ניתן לבצע את הפעולה" });

            return Ok(true);
        }
        //[HttpPost("{id}/report-refuel")]
        //[Authorize(Roles = "user")]
        //public IActionResult ReportRefuel(int id)
        //{
        //    var result = orderService.ReportRefuel(id);
        //    if (!result) return BadRequest(new { message = "דיווח תדלוק נכשל" });
        //    return Ok(new { message = "התדלוק עודכן בהצלחה! הבונוס יחושב בסיום הנסיעה." });
        //}
        [HttpPost("{id}/report-refuel")]
        [Authorize(Roles = "user")]
        public IActionResult ReportRefuel(int id)
        {
            var result = orderService.ReportRefuel(id);
            if (!result) return BadRequest(new { message = "דיווח תדלוק נכשל" });
            return Ok(true);
        }
    }
}

