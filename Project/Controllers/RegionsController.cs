using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using Service.Services;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegionsController : ControllerBase
    {
        private readonly IRegionService regionService;

    public RegionsController(IRegionService service)
    {
        this.regionService = service;
    }

    
    [HttpGet]
    public IActionResult Get()
    {
            return Ok(regionService.GetAll());
    }

    
    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
            var region = regionService.GetById(id);
            if (region == null)
                return NotFound(new { message = "האזור לא נמצא" });

            return Ok(region);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public IActionResult Post([FromBody]RegionDto item)
    {
            var created = regionService.Add(item);
            if (created == null)
                return BadRequest(new { message = "אזור בשם זה כבר קיים במערכת" });

            return Created("", new { message = "האזור נוסף בהצלחה", data = created });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Put(int id, [FromBody]RegionDto item)
    {
            var result = regionService.Update(id, item);
            if (!result)
                return BadRequest(new { message = "עדכון נכשל: האזור לא נמצא או שהשם תפוס" });

            return Ok(new { message = "נתוני האזור עודכנו בהצלחה" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Delete(int id)
    {
            var result = regionService.Delete(id);
            if (!result)
                return NotFound(new { message = "מחיקה נכשלה: האזור לא נמצא" });

            return Ok(new { message = "האזור נמחק בהצלחה" });
    }
    [HttpGet("name/{name}")]
    public ActionResult<RegionDto> GetByName(string name)
    {
        var region = regionService.GetByName(name);
        if (region == null)
        {
            return NotFound($"לא נמצא רובע בשם: {name}");
        }
        return Ok(region);
    }

    [HttpGet("count")]
    public ActionResult<int> GetTotalRegionsCount()
    {
        var count = regionService.GetTotalRegionsCount();
        return Ok(count);
    }

    [HttpPatch("{id}/center")]
    public IActionResult UpdateCenterPoint(int id, [FromQuery] double lat, [FromQuery] double lng)
    {
        var success = regionService.UpdateCenterPoint(id, lat, lng);
        if (!success)
        {
            return BadRequest("עדכון הנקודה נכשל. וודא שהמזהה תקין.");
        }
        return Ok("נקודת המרכז עודכנה בהצלחה.");
    }
    }
}