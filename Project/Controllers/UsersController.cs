using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Service.Interfaces;
using Service.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController: ControllerBase
    {
        private readonly IUserService userService;

        public UsersController(IUserService userService)
        {
            this.userService = userService;
        }


        [HttpPost("login")]
        public string Login([FromBody] LoginDto l)
        {
            var token = userService.Login(l);
            if (token == null || token == "user dosent exist..")
                return "מייל או סיסמה שגויים";
            return token;
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IEnumerable<UserDto> Get()
        {
            return userService.GetAll();
        }


        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public UserDto? Get(int id)
        {
            return userService.GetById(id);
        }

        [HttpPost]
        public UserDto Post([FromBody] UserDto item)
        {
            return userService.Add(item);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "user")]
        public bool Put(int id, [FromBody] UserDto item)
        {
            return userService.Update(id, item);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public bool Delete(int id)
        {
            return userService.Delete(id);
        }

    //  [HttpPost]
    //    private UserDto? GetCurrentUser()
    //    {
    //        var identity = HttpContext.User.Identity as ClaimsIdentity;
    //        if (identity != null)
    //        {
    //            var userClaims = identity.Claims;

    //            return new UserDto()
    //            {
    //                Id = int.Parse(userClaims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "0"),

    //                FirstName = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,

    //                UserType = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value
    //            };
    //        }
    //        return null;
    //    }
    }
}
