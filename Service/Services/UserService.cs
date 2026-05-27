using AutoMapper;
using Common.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.Interfaces;
using System;
using BCrypt.Net;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly EncryptionService _encryptionService;
        public UserService(IConfiguration configuration, IRepository<User> userRepository, IHttpContextAccessor httpContextAccessor, IMapper mapper, IEmailService emailService, IMemoryCache cache, EncryptionService encryptionService)
        {
            this._userRepository = userRepository;
            this._configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _cache = cache;
            _emailService = emailService;
            _encryptionService = encryptionService;
        }
        public async Task<UserDto> Add(UserDto item)
        {
            var isEmailTaken = _userRepository.GetAll().Any(u => u.Email == item.Email);
            if (isEmailTaken) return null;
       
            User newUser = _mapper.Map<User>(item);

            if (!string.IsNullOrEmpty(item.LicenseNumber))
                newUser.LicenseNumber = _encryptionService.Encrypt(item.LicenseNumber);

            if (!string.IsNullOrEmpty(item.PassportNumber))
                newUser.PassportNumber = _encryptionService.Encrypt(item.PassportNumber);

            if (!string.IsNullOrEmpty(item.CardNumber))
                newUser.CardNumber = _encryptionService.Encrypt(item.CardNumber);

            if (!string.IsNullOrEmpty(item.CardExpiry))
                newUser.CardExpiry = _encryptionService.Encrypt(item.CardExpiry);

            if (!string.IsNullOrEmpty(item.CVV))
                newUser.CVV = _encryptionService.Encrypt(item.CVV);

            string passwordToHash = item.Password ?? "123456";
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordToHash);

            newUser.CreatedAt = DateTime.Now;
            newUser.IsBlocked = false;
            newUser.Rank = UserRank.Regular;
            var savedUser = _userRepository.Add(newUser);

           
            if (savedUser != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(savedUser.Email, savedUser.FirstName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Welcome email failed: {ex.Message}");
                    }
                });
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
            var dto= _mapper.Map<UserDto>(user);
            dto.LicenseNumber = _encryptionService.Decrypt(dto.LicenseNumber);
            dto.PassportNumber = _encryptionService.Decrypt(dto.PassportNumber);
            if (!string.IsNullOrEmpty(dto.CardNumber))
                dto.CardNumber = _encryptionService.Decrypt(dto.CardNumber);

            if (!string.IsNullOrEmpty(dto.CardExpiry))
                dto.CardExpiry = _encryptionService.Decrypt(dto.CardExpiry);

            if (!string.IsNullOrEmpty(dto.CVV))
                dto.CVV = _encryptionService.Decrypt(dto.CVV);
            return dto;
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

    
        public int Login(LoginDto l, out string token)
        {
            token = null;

            var user = _userRepository.GetAll().FirstOrDefault(u => u.Email == l.Email);
            if (user == null) return 404;

            bool isCorrect = !string.IsNullOrEmpty(user.PasswordHash) &&
                             BCrypt.Net.BCrypt.Verify(l.Pass, user.PasswordHash);

            if (!isCorrect) return 401; 

            if (user.IsBlocked) return 403;

            token = GenerateToken(_mapper.Map<UserDto>(user));
            return 200;
        }
        private string GenerateToken(UserDto user)
        {
            var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.UserType.ToString())            };
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public UserDto GetByEmail(string email)
        {
            var user=_userRepository.GetAll()
                .FirstOrDefault(u=>u.Email==email); 
            if (user == null) return null;
            return _mapper.Map<UserDto>(user);
        }
        public async Task<bool> RequestPasswordReset(string email)
        {
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Email == email);
            if (user == null || user.IsBlocked) return false;

            string resetCode = new Random().Next(100000, 999999).ToString();

            user.PasswordResetCode = resetCode;
            user.ResetCodeExpiration = DateTime.Now.AddMinutes(5);

            _userRepository.Update(user.Id, user);

            try
            {
                await _emailService.SendPasswordResetAsync(user.Email, resetCode);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send reset email: {ex.Message}");
                return false;
            }
        }

        //public bool ChangePassword(int userId, string oldPassword, string newPassword)
        //{
        //    var user = _userRepository.GetById(userId);
        //    var currentUserIdClaim = _httpContextAccessor.HttpContext?.User
        //        .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        //    if (currentUserIdClaim == null) return false;

        //    int currentUserId = int.Parse(currentUserIdClaim);

        //    if (currentUserId != userId)
        //    {
        //        return false;
        //    }
        //    bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);

        //    if (user == null || !isOldPasswordValid || user.IsBlocked)
        //    {
        //        return false; // הסיסמה הישנה לא נכונה
        //    }

        //    // 2. שינוי בעדכון: הפיכת הסיסמה החדשה ל-Hash לפני שמירה
        //    // אנחנו לא שומרים את ה-newPassword כמו שהיא, אלא את ה-Hash שלה
        //    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        //    //var user = _userRepository.GetById(userId);

        //    //if (user == null || user.PasswordHash != oldPassword || user.IsBlocked)
        //    //{
        //    //    return false;
        //    //}

        //    //user.PasswordHash = newPassword;
        //    _userRepository.Update(userId, user);

        //    return true;
        //}
        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            var user = _userRepository.GetById(userId);

            if (user == null || user.IsBlocked) return false;

            var currentUserIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (currentUserIdClaim == null) return false;
            int currentUserId = int.Parse(currentUserIdClaim);

            if (currentUserId != userId) return false;

            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);

            if (!isOldPasswordValid)
            {
                return false; 
            }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _userRepository.Update(userId, user);

            return true;
        }
        public bool ResetPassword(string email, string code, string newPassword)
        {
            
            string normalizedEmail = email.Trim().ToLower();

            var user = _userRepository.GetAll().FirstOrDefault(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || user.IsBlocked ||
                user.PasswordResetCode != code ||
                user.ResetCodeExpiration < DateTime.Now)
            {
                return false;
            }    
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            user.PasswordResetCode = null;
            user.ResetCodeExpiration = null;

            _userRepository.Update(user.Id, user);
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


        //Verify that the new user is real (by sending a code to their email)
        public async Task<bool> RequestRegistrationCode(string email)
        {
            email = email.Trim().ToLower();

            if (_userRepository.GetAll().Any(u => u.Email == email))
                return false;

            // צרי קוד חדש בכל בקשה כדי למנוע סתירה בין מה שיש ב-Cache למה שנשלח במייל
            string code = new Random().Next(100000, 999999).ToString();

            // שמירה ב-Cache (ידרוס קוד קודם אם היה)
            _cache.Set(email, code, TimeSpan.FromMinutes(5));

            await _emailService.SendRegistrationVerificationAsync(email, code);
            return true;
        }

        public bool VerifyRegistrationCode(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code)) return false;

            string normalizedEmail = email.Trim().ToLower();

            if (_cache.TryGetValue(normalizedEmail, out string savedCode))
            {
                string cleanCode = code.Trim();
                bool isMatch = savedCode == cleanCode;
                Console.WriteLine($"Verification for {normalizedEmail}: Sent={cleanCode}, Saved={savedCode}, Match={isMatch}");
                return isMatch;
            }
            Console.WriteLine($"Verification failed: Email '{normalizedEmail}' not found in Cache.");
            return false;
        }

        public bool Exists(int id)
        {
            return _userRepository.Exists(id);
        }

        public bool Exists(LoginDto l)
        {
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Email == l.Email);
            return user != null && BCrypt.Net.BCrypt.Verify(l.Pass, user.PasswordHash);
        }
        public UserDto Exist(LoginDto l)
        {
            // שליפת המשתמש לפי אימייל
            var userEntity = _userRepository.GetAll().FirstOrDefault(u => u.Email == l.Email);

            // בדיקה אם המשתמש קיים והאם הסיסמה (בטקסט רגיל) תואמת
            if (userEntity != null && userEntity.PasswordHash == l.Pass)
            {
                return _mapper.Map<UserDto>(userEntity);
            }

            return null;
        }
    }
}
