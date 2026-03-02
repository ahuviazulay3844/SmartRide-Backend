using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using Service.Services;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService couponService;

    public CouponsController(ICouponService service)
    {
        this.couponService = service;
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public IActionResult Get()
    {
      return Ok(couponService.GetAll());    
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "admin,user")]
    public IActionResult Get(int id)
    {
            var coupon = couponService.GetById(id);
            if (coupon == null)
                return NotFound(new { message = "הקופון לא נמצא" });
            return Ok(coupon);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public IActionResult Post([FromBody]CouponDto item)
    {
            var created = couponService.Add(item);
            if (created == null)
                return BadRequest(new { message = "קוד קופון זה כבר קיים" });

            return Created("", new { message = "קופון נוצר בהצלחה", data = created });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Put(int id, [FromBody]CouponDto item)
    {
            var result = couponService.Update(id, item);
            if (!result)
                return BadRequest(new { message = "עדכון נכשל" });

            return Ok(new { message = "הקופון עודכן בהצלחה" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Delete(int id)
    {
            var result = couponService.Delete(id);
            if (!result)
                return NotFound(new { message = "מחיקה נכשלה: הקופון לא נמצא" });

            return Ok(new { message = "הקופון נמחק בהצלחה" });
    }
    [HttpGet("apply-discount")]
    public ActionResult<decimal> ApplyDiscount(string code, decimal originalAmount, int userId)
    {
        var finalAmount = couponService.ApplyDiscount(code, originalAmount, userId);
        return Ok(finalAmount);
    }

    [HttpGet("validate")]
    public ActionResult<bool> IsCouponValid(string code, int userId, decimal amount)
    {
        return Ok(couponService.IsCouponValid(code, userId, amount));
    }

    [HttpPost("redeem")]
    public IActionResult MarkAsUsed(int userId, string code)
    {
        var success = couponService.MarkAsUsed(userId, code);
        if (!success)
        {
            return BadRequest("הפעולה נכשלה: הקופון כבר נוצל או שאינו תקף.");
        }
        return Ok("הקופון מומש בהצלחה.");
    }

    [HttpGet("user/{userId}/unused")]
    public ActionResult<IEnumerable<CouponDto>> GetUnusedCouponsByUserId(int userId)
    {
        var coupons = couponService.GetUnusedCouponsByUserId(userId);
        return Ok(coupons);
    }

    [HttpGet("expiring-soon")]
    public ActionResult<IEnumerable<CouponDto>> GetExpiringSoon(int days = 7)
    {
        var coupons = couponService.GetExpiringSoon(days);
        return Ok(coupons);
    }


    }
}