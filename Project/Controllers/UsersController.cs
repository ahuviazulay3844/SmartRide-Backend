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
        private readonly IConfiguration _configuration;
        private readonly IUserService userService;
        public readonly IsExist<UserDto> isExist;

        public UsersController(IUserService userService, IConfiguration configuration, IsExist<UserDto> isExist)
        {
            this.userService = userService;
            this._configuration = configuration;
            this.isExist = isExist;
        }


        [HttpPost("login")]
        public string Login([FromBody] LoginDto l)
        {
            UserDto user = isExist.Exist(l);

            if (user != null)
                return GenerateToken(user);
            return "user dosent exist..";
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

        private string GenerateToken(UserDto user)
        {
            var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                new Claim(ClaimTypes.Role, user.UserType.ToString()) 
            };
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: credentials);
                
                return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private UserDto? GetCurrentUser()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var userClaims = identity.Claims;

                return new UserDto()
                {
                    Id = int.Parse(userClaims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "0"),

                    FirstName = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,

                    UserType = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value
                };
            }
            return null;
        }
    }
}

