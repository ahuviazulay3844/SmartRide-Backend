using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

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
    public IEnumerable<CouponDto> Get()
    {
        return couponService.GetAll();
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "admin,user")]
    public CouponDto? Get(int id)
    {
        return couponService.GetById(id);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public CouponDto Post([FromBody]CouponDto item)
    {
        return couponService.Add(item);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public bool Put(int id, [FromBody]CouponDto item)
    {
        return couponService.Update(id, item);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public bool Delete(int id)
    {
        return couponService.Delete(id);
    }
}
}