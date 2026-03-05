using AutoMapper;
using Common.Dto;
using Microsoft.AspNetCore.Http;
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
namespace Service.Services
{
    public class UserService : IUserService, IsExist<UserDto>
    {
        private readonly IConfiguration _configuration;
        private readonly IRepository<User> _userRepository;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;//בשביל לדעת מי המשתמש?
        private readonly IMapper _mapper;
        public UserService(IConfiguration configuration, IRepository<User> userRepository, IHttpContextAccessor httpContextAccessor, IMapper mapper, IEmailService emailService)
        {
            this._userRepository = userRepository;
            this._configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _emailService = emailService;
        }
        public async Task<UserDto> Add(UserDto item)
        {
            var isEmailTaken = _userRepository.GetAll().Any(u => u.Email == item.Email);
            if (isEmailTaken) { return null; }
            User newUser = _mapper.Map<User>(item);
            newUser.PasswordHash = item.Password ?? "123456";
            newUser.CreatedAt = DateTime.Now;
            newUser.IsBlocked = false;
            newUser.Rank = UserRank.Regular;
            var savedUser = _userRepository.Add(newUser);
            if (savedUser != null)
            {
                try
                {
                    await _emailService.SendWelcomeEmailAsync(savedUser.Email, savedUser.FirstName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Welcome email failed for {savedUser.Email}: {ex.Message}");
                }
            }
            return _mapper.Map<UserDto>(savedUser);
        }

        public bool Delete(int id)
        {
            if (!Exists(id)) return false;

            return _userRepository.Delete(id);
        }

      

        public IEnumerable<UserDto> GetAll()
        {
            var users = _userRepository.GetAll()
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public UserDto? GetById(int id)
        {
            var user = _userRepository.GetById(id);
            if (user == null) return null;
            return _mapper.Map<UserDto>(user);
        }


        public bool Update(int id, UserDto item)
        {
            var existing = _userRepository.GetById(id);
            if (existing == null) return false;
            bool emailTakenByOther = _userRepository.GetAll().Any(u => u.Email == item.Email && u.Id != id);
            if (emailTakenByOther) return false;
            _mapper.Map(item, existing);
            _userRepository.Update(id, existing);
            return true;
        }

        public string Login(LoginDto l)
        {
            UserDto user = Exist(l);
            if (user != null && !user.IsBlocked)
                return GenerateToken(user);
            return null;
         
        }
        private string GenerateToken(UserDto user)
        {
            var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.UserType ?? "user"),
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
            return _mapper.Map<UserDto>(user);
        }

        public bool Exists(int id)
        {
            return _userRepository.Exists(id);
        }

        public UserDto GetByEmail(string email)
        {
            var user=_userRepository.GetAll()
                .FirstOrDefault(u=>u.Email==email); 
            if (user == null) return null;
            return _mapper.Map<UserDto>(user);
        }

        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            var currentUserIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (currentUserIdClaim == null) return false;

            int currentUserId = int.Parse(currentUserIdClaim);

            if (currentUserId != userId)
            {
                return false;
            }

            var user = _userRepository.GetById(userId);

            if (user == null || user.PasswordHash != oldPassword || user.IsBlocked)
            {
                return false;
            }

            user.PasswordHash = newPassword;
            _userRepository.Update(userId, user);

            return true;
        }
        public bool ToggleBlockUser(int userId)
        {
            var user = _userRepository.GetAll()
                .FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;
            user.IsBlocked = !user.IsBlocked;
            _userRepository.Update(userId, user);
            return true;
        }

        public UserDto GetCurrentUser()
        {
            var identity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return GetById(userId);
                }
            }
            return null;
        }

        public int GetTotalUsersCount()
        {
            return _userRepository.GetAll().Count();
        }
        // בקשת איפוס סיסמה: יצירת קוד, שמירתו במסד ושליחת מייל למשתמש
        public async Task<bool> RequestPasswordReset(string email)
        {
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Email == email);
            if (user == null || user.IsBlocked) return false;

            // יצירת קוד בן 6 ספרות
            string resetCode = new Random().Next(100000, 999999).ToString();

            user.PasswordResetCode = resetCode;
            user.ResetCodeExpiration = DateTime.Now.AddMinutes(5);

            _userRepository.Update(user.Id, user);

            try
            {
                // שליחת המייל עם הקוד
                await _emailService.SendPasswordResetAsync(user.Email, resetCode);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send reset email: {ex.Message}");
                return false;
            }
        }
        // איפוס סיסמה: בדיקת הקוד, עדכון הסיסמה ומחיקת הקוד
        public bool ResetPassword(string email, string code, string newPassword)
        {
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Email == email);
            if (user == null ||
                user.PasswordResetCode != code ||
                user.ResetCodeExpiration < DateTime.Now)
            {
                return false;
            }
            user.PasswordHash = newPassword;
            user.PasswordResetCode = null;
            user.ResetCodeExpiration = null;
            _userRepository.Update(user.Id, user);
            return true;
        }
    }
}
