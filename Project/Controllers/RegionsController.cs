using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

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
    public IEnumerable<RegionDto> Get()
    {
        return regionService.GetAll();
    }

    
    [HttpGet("{id}")]
    public RegionDto? Get(int id)
    {
        return regionService.GetById(id);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public RegionDto Post([FromBody]RegionDto item)
    {
        return regionService.Add(item);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public bool Put(int id, [FromBody]RegionDto item)
    {
        return regionService.Update(id, item);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public bool Delete(int id)
    {
        return regionService.Delete(id);
    }
}
}