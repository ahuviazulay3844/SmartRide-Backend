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
        private readonly IHttpContextAccessor _httpContextAccessor;//בשביל לדעת מי המשתמש?
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
            // 1. בדיקה אם המשתמש כבר קיים במערכת לפי אימייל
            var isEmailTaken = _userRepository.GetAll().Any(u => u.Email == item.Email);
            if (isEmailTaken) return null;

            // 2. מיפוי מה-DTO ל-Entity
            User newUser = _mapper.Map<User>(item);

            // 3. הצפנת נתונים רגישים (Symmetric Encryption)
            // אנחנו מצפינים את הנתונים לפני השמירה כדי שגם אם מסד הנתונים ייחשף, המידע יהיה מוגן
            if (!string.IsNullOrEmpty(item.LicenseNumber))
                newUser.LicenseNumber = _encryptionService.Encrypt(item.LicenseNumber);

            if (!string.IsNullOrEmpty(item.PassportNumber))
                newUser.PassportNumber = _encryptionService.Encrypt(item.PassportNumber);

            // הצפנת פרטי כרטיס אשראי שהגיעו מה-Frontend
            if (!string.IsNullOrEmpty(item.CardNumber))
                newUser.CardNumber = _encryptionService.Encrypt(item.CardNumber);

            if (!string.IsNullOrEmpty(item.CardExpiry))
                newUser.CardExpiry = _encryptionService.Encrypt(item.CardExpiry);

            if (!string.IsNullOrEmpty(item.CVV))
                newUser.CVV = _encryptionService.Encrypt(item.CVV);

            // 4. אבטחת סיסמה (Hashing - בלתי ניתן לפענוח)
            string passwordToHash = item.Password ?? "123456";
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordToHash);

            // 5. הגדרות ברירת מחדל למשתמש חדש
            newUser.CreatedAt = DateTime.Now;
            newUser.IsBlocked = false;
            newUser.Rank = UserRank.Regular;

            // 6. שמירה במסד הנתונים
            var savedUser = _userRepository.Add(newUser);

            // 7. שליחת מייל ברוך הבא (תהליך אסינכרוני "שגר ושכח" כדי לא לעכב את ה-UI)
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
                        // לוג שגיאה בלבד, לא עוצר את תהליך הרישום
                        Console.WriteLine($"Welcome email failed: {ex.Message}");
                    }
                });
            }

            // 8. החזרת המשתמש השמור כ-DTO
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

        public string Login(LoginDto l) 
        {
            UserDto user = Exist(l);
            if (user != null && !user.IsBlocked)
            {
                var token = GenerateToken(user);
                return token;
            }
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
                new Claim(ClaimTypes.Role, user.UserType.ToString())            };
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        //public UserDto Exist(LoginDto l)
        //{
        //    var user = _userRepository.GetAll()
        //                   .FirstOrDefault(u => u.Email == l.Email && u.PasswordHash == l.Pass);
        //    if (user == null) return null;
        //    return _mapper.Map<UserDto>(user);
        //}

        //public UserDto Exist(LoginDto l)
        //{
        //    // שולפים את המשתמש לפי האימייל בלבד
        //    var user = _userRepository.GetAll()
        //                           .FirstOrDefault(u => u.Email == l.Email);

        //    // משתמשים ב-Verify כדי לבדוק אם הסיסמה שהוקלדה מתאימה ל-Hash המוצפן
        //    if (user == null || !BCrypt.Net.BCrypt.Verify(l.Pass, user.PasswordHash))
        //        return null;

        //    return _mapper.Map<UserDto>(user);
        //}
        //public UserDto Exist(LoginDto l)
        //{
        //    var userEntity = _userRepository.GetAll().FirstOrDefault(u => u.Email == l.Email);
        //    if (userEntity != null && BCrypt.Net.BCrypt.Verify(l.Pass, userEntity.PasswordHash))
        //    {
        //        return _mapper.Map<UserDto>(userEntity);
        //    }

        //    return null;
        //}
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

            // בדיקה: האם המפתח קיים בכלל?
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
        // בתוך UserService.cs
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
