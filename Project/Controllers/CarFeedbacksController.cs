using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

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
        public IEnumerable<CarFeedbackDto> Get()
        {
            return FeedbackService.GetAll();
        }

        [HttpGet("{id}")]
        public CarFeedbackDto? Get(int id)
        {
            return FeedbackService.GetById(id);
        }

        [HttpPost]
        [Authorize(Roles = "user")]
        public CarFeedbackDto Post([FromBody] CarFeedbackDto item)
        {
            return FeedbackService.Add(item);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "user")]
        public bool Put(int id, [FromBody] CarFeedbackDto item)
        {
            return FeedbackService.Update(id, item);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "user,admin")]
        public bool Delete(int id)
        {
            return FeedbackService.Delete(id);
        }
    }
}