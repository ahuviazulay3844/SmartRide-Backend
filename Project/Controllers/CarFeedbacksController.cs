using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Repositories;
using Service.Interfaces;
using System.Net;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarFeedbacksController : ControllerBase
    {
        private readonly ICarFeedbackService FeedbackService;

        public CarFeedbacksController(ICarFeedbackService service)
        {
            this.FeedbackService = service;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var allFeedbacks = FeedbackService.GetAll();
            return Ok(allFeedbacks);
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var feedback = FeedbackService.GetById(id);
            if (feedback == null)
                return NotFound(new { message = "הפידבק לא נמצא במערכת" });

            return Ok(feedback);
        }

        [HttpPost]
        [Authorize(Roles = "user")]
        public IActionResult Post([FromBody] CarFeedbackDto item)
        {
            var created = FeedbackService.Add(item);
            return Created(nameof(Get), new { id = created.Id, message = "הפידבק נוסף בהצלחה", data = created });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "user")]
        public IActionResult Put(int id, [FromBody] CarFeedbackDto item)
        {
            var result = FeedbackService.Update(id, item);

            if (!result)
            {

                if (!FeedbackService.Exists(id))
                {
                    return NotFound(new { message = "הפידבק לא נמצא במערכת" });
                }
                return Forbid();
            }

            return Ok(new { message = "הפידבק עודכן בהצלחה", data = item });
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "user,admin")]
        public IActionResult Delete(int id)
        {
            var deleted = FeedbackService.Delete(id);
            if (!deleted)
                return NotFound(new { message = "הפידבק לא נמצא" });

            return Ok(new { message = "הפידבק נמחק בהצלחה" });
        }
        [HttpGet("car/{carId}")] 
        public IActionResult GetByIdOfCar(int carId)
        {
            var feedbacks = FeedbackService.GetByIdOfCar(carId);
            return Ok(new { data = feedbacks });
        }

        [HttpGet("user/{userId}")] 
        [Authorize] 
        public IActionResult GetByIdOfUser(int userId)
        {
            var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            bool isAdmin = User.IsInRole("admin");
            if (!isAdmin && currentUserIdClaim != userId.ToString())
            {
                return Forbid(); 
            }
            var feedbacks = FeedbackService.GetByIdOfUser(userId);
            return Ok(new { data = feedbacks });
        }
    }
}