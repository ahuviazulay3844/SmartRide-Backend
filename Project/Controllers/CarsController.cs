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
        public IEnumerable<CarDto> Get()
        {
            return carService.GetAll();
        }

        [HttpGet("{id}")]
        public CarDto? Get(int id)
        {
            return carService.GetById(id);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public CarDto Post([FromBody] CarDto car)
        {
          return carService.Add(car);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public bool Put(int id, [FromBody] CarDto car)
        {
            return carService.Update(id, car);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public bool Delete(int id)
        {
            return carService.Delete(id);
        }

    }
}