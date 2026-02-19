using Common.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
namespace Service.Services
{
    public class UserService : IUserService, IsExist<UserDto>
    {
        private readonly IConfiguration _configuration;
        private readonly IRepository<User> _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;//בשביל לדעת מי המשתמש?

        public UserService(IConfiguration configuration, IRepository<User> userRepository, IHttpContextAccessor httpContextAccessor)
        {
            this._userRepository = userRepository;
            this._configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }
        public UserDto Add(UserDto item)
        {
            var isEmailTaken = _userRepository.GetAll().Any(u => u.Email == item.Email);
            if (isEmailTaken)
            {
                throw new Exception("משתמש עם אימייל זה כבר קיים במערכת");
            }
            User newUser = new User
            {
                FirstName = item.FirstName,
                LastName = item.LastName,
                Email = item.Email,
                PasswordHash = item.Password ?? "123456",
                PhoneNumber = item.PhoneNumber,
                LicenseNumber = item.LicenseNumber,
                CreatedAt = DateTime.Now,
                UserType = item.UserType == "admin" ? Repository.Entities.UserType.admin : Repository.Entities.UserType.user,
                IsBlocked = false,
                Rank = UserRank.Regular
            };
            var savedUser = _userRepository.Add(newUser);
            item.Id = savedUser.Id;
            return item;
        }

        public bool Delete(int id)
        {
            if (!Exists(id)) return false;

            return _userRepository.Delete(id);
        }

      

        public IEnumerable<UserDto> GetAll()
        {
         return _userRepository.GetAll()
        .OrderBy(u => u.FirstName) 
        .ThenBy(u => u.LastName)   
        .Select(u => new UserDto {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            LicenseNumber = u.LicenseNumber,
            UserType = u.UserType.ToString()
        }).ToList();
        }

        public UserDto? GetById(int id)
        {
            var user =_userRepository.GetById(id);
            if (user == null) return null;
            return new UserDto
                   {
                       Id = user.Id,
                       FirstName = user.FirstName,
                       LastName = user.LastName,
                       Email = user.Email,
                       PhoneNumber = user.PhoneNumber,
                       LicenseNumber = user.LicenseNumber,
                       UserType = user.UserType.ToString()
            };
        }


        public bool Update(int id, UserDto item)
        {
            if (!Exists(id)) return false;
            bool emailTakenByOther = _userRepository.GetAll()
        .Any(u => u.Email == item.Email && u.Id != id);

            if (emailTakenByOther)
            {
                throw new Exception("האימייל הזה כבר תפוס על ידי משתמש אחר במערכת");
            }
            _userRepository.Update(id, new User
            {
                Id = id,
                FirstName = item.FirstName,
                LastName = item.LastName,
                Email = item.Email,
                PhoneNumber = item.PhoneNumber,
                LicenseNumber = item.LicenseNumber,
                UserType = item.UserType == "admin" ? Repository.Entities.UserType.admin : Repository.Entities.UserType.user,
                IsBlocked = item.IsBlocked,
                Rank = Enum.TryParse(item.Rank, out UserRank rank) ? rank : UserRank.Regular
            });
            return true;
        }

        public string Login(LoginDto l)
        {
            UserDto user = Exist(l);
            if (user != null)
                return GenerateToken(user);
            return "user dosent exist..";
         
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

        public UserDto Exist(LoginDto l)
        {
            var user = _userRepository.GetAll()
                           .FirstOrDefault(u => u.Email == l.Email && u.PasswordHash == l.Pass);

            if (user == null) return null;

            return new UserDto { Id = user.Id, FirstName = user.FirstName };
        }

        public bool Exists(int id)
        {
            return _userRepository.Exists(id);
        }
    }
}
