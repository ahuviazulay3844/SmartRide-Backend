using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Service.Interfaces;
using Service.Services;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarsController : ControllerBase
    {
        private readonly ICarService carService;

        public CarsController(ICarService carService)
        {
            this.carService = carService;
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            var cars = carService.GetAll();
            return Ok(cars);
        }

        [HttpGet("{id}")]

        public IActionResult Get(int id)
        {
            var car = carService.GetById(id);
            if (car == null)
                return NotFound(new { message = "הרכב לא נמצא במערכת" });
            return Ok(car);
        }

        [HttpPost]
        [Authorize (Roles = "admin")]
        public IActionResult Post([FromBody] CarDto car)
        {
            var newCar = carService.Add(car);
            if (newCar == null)
                return BadRequest(new { message = "רכב עם מס' רישוי זהה קיים" });
            return Created("", new { message = "הרכב נוסף בהצלחה", data = newCar });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult Put(int id, [FromBody] CarDto car)
        {
            var result = carService.Update(id, car);
            if (!result)
                return NotFound(new { message = "לא ניתן לעדכן" });
            return Ok(new { message = "נתוני הרכב עודכנו בהצלחה" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult Delete(int id)
        {
            var result = carService.Delete(id);
            if (!result)
                return NotFound(new { message = "מחיקה נכשלה, הרכב לא נמצא" });

            return Ok(new { message = "הרכב נמחק בהצלחה מהמערכת" });
        }
        //[HttpGet("closest")]
        //public IActionResult GetClosest([FromQuery] double lat, [FromQuery] double lng, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
        //{
        //    if (lat == 0 && lng == 0)
        //    {
        //        return BadRequest("Location data is missing");
        //    }

        //    var cars = carService.GetAllClosest(lat, lng, start, end);
        //    return Ok(cars);
        //}
        [HttpGet("closest")]
        public IActionResult GetClosest([FromQuery] double lat, [FromQuery] double lng, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            if (lat == 0 && lng == 0)
            {
                return BadRequest(new { message = "מיקום חסר" });
            }
            var cars = carService.GetAllClosest(lat, lng, start, end);
            return Ok(cars);
        }
        [HttpGet("available")] 
        public IActionResult GetAvailableCars([FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] int regionId)
        {
            var availableCars = carService.GetAvailableCars(start, end, regionId);
            return Ok(availableCars);
        }
        [HttpGet("{id}/check-suitability")]
        public IActionResult CheckCarSuitability(int id, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var suitability = carService.CheckCarSuitability(id, start, end);
            if (suitability != "OK")
            {
                return BadRequest(new { message = suitability });
            }
            return Ok(new { message = "הרכב מתאים לנסיעה" });
        }

        [HttpGet("popular")]
        public IActionResult GetAllPopularCars([FromQuery] int count = 5)
        {
            var popularCars = carService.GetAllPopularCars(count);
            return Ok(new { data = popularCars });
        }

        [HttpGet("needs-fuel")]
        public IActionResult GetVehiclesNeedingFuel()
        {
            var carsNeedingFuel = carService.GetVehiclesNeedingFuel();
            return Ok(new { data = carsNeedingFuel });
        }

        [HttpPatch("{id}/fuel")]
        public IActionResult UpdateFuelLevel(int id, [FromBody] int newLevel)
        {
            var result = carService.UpdateFuelLevel(id, newLevel);
            if (!result) return NotFound(new { message = "הרכב לא נמצא לעדכון דלק" });
            return Ok(new { message = "רמת הדלק עודכנה", currentLevel = newLevel });
        }
        [HttpPatch("{id}/mileage")]
        public IActionResult UpdateMileage(int id, [FromBody] int newMileage)
        {
            var result = carService.UpdateMileage(id, newMileage);
            if (!result) return NotFound(new { message = "הרכב לא נמצא לעדכון קילומטראז'" });
            return Ok(new { message = "קילומטראז' הרכב עודכן", currentMileage = newMileage });
        }
        [HttpGet("{id}/is-fit")]
        public IActionResult IsCarFit(int id)
        {
            var result = carService.IsCarFitForRoad(id);
            return Ok(result);
        }
        [HttpGet("status/{status}")]
        public IActionResult GetByStatus(string status)
        {
            var cars = carService.GetByStatus(status);
            if (cars == null || !cars.Any())
            {
                return Ok(new { message = $"לא נמצאו רכבים בסטטוס: {status}", data = new List<CarDto>() });
            }
            return Ok(new { data = cars });
        }
        [HttpPatch("{id}/status")]
        public IActionResult UpdateStatus(int id, [FromBody] System.Text.Json.JsonElement body)
        {
            if (body.TryGetProperty("status", out var statusProperty))
            {
                string statusValue = statusProperty.ToString();
                var result = carService.UpdateStatus(id, statusValue);
                return result ? Ok() : BadRequest("עדכון סטטוס נכשל");
            }
            return BadRequest("נתונים לא תקינים");
        }
        [HttpPatch("{carId}/send-to-maintenance")]
        public IActionResult SendToMaintenance(int carId)
        {
            var result = carService.SendToMaintenance(carId);
            return result ? Ok() : BadRequest("שליחה לתחזוקה נכשלה");
        }
        [HttpPatch("{carId}/release-from-maintenance")]
        public IActionResult ReleaseFromMaintenance(int carId)
        {
            var result = carService.ReleaseFromMaintenance(carId);
            return result ? Ok() : BadRequest("שחרור מהתחזוקה נכשל");
        }
        [HttpGet("available/by-region/{regionId}")]
        public IActionResult GetAvailableCarsByRegion(int regionId)
        {
            var availableCars = carService.GetAvailableCarsByRegion(regionId);
            return Ok(new { data = availableCars });
        }
        [HttpGet("{id}/extended-status")]
        public IActionResult GetExtendedStatus(int id)
        {
            var car = carService.GetById(id);
            if (car == null) return NotFound();

            // קריאה לשירות שיבדוק זמינות נוכחית ובשעה הקרובה
            var statusInfo = carService.GetCarAvailabilityInfo(id);
            return Ok(statusInfo);
        }
        [HttpPatch("{id}/toggle-lock")]
        public IActionResult ToggleLock(int id, [FromBody] System.Text.Json.JsonElement body)
        {
            if (body.TryGetProperty("isLocked", out var lockProperty))
            {
                bool isLocked = lockProperty.GetBoolean();
                var result = carService.UpdateLockStatus(id, isLocked);
                return result ? Ok() : BadRequest("עדכון הנעילה נכשל");
            }
            return BadRequest("נתונים לא תקינים");
          }
    }
}