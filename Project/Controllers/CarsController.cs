using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Service.Interfaces;

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
        [HttpGet("closest")]
        public IActionResult GetClosest([FromQuery] double lat, [FromQuery] double lng)
        {
            var closestCars = carService.GetAllClosest(lat, lng);
            return Ok(new { data = closestCars });
        }
        [HttpGet("available")]
        public IActionResult GetAvailableCars([FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] int regionId)
        {
            var availableCars = carService.GetAvailableCars(start, end, regionId);
            return Ok(new { data = availableCars });
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
        public IActionResult UpdateStatus(int id, [FromBody] string status)
        {
            var result = carService.UpdateStatus(id, status);
            return result ? Ok() : BadRequest("עדכון סטטוס נכשל");
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
    }
}