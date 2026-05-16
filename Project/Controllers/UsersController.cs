using Common.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Service.Interfaces;
using Service.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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

        //[HttpPost("login")]
        //public IActionResult Login([FromBody] LoginDto loginDto)
        //{
        //    var token = userService.Login(loginDto);
        //    if (string.IsNullOrEmpty(token))
        //    {
        //        return Unauthorized(new { message = "אימייל או סיסמה שגויים, או שהמשתמש חסום" });
        //    }
        //    return Ok(token);
        //}

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            string token;
            int status = userService.Login(loginDto, out token);

            return status switch
            {
                // שינוי קטן: מחזירים אובייקט עם המילה token
                200 => Ok(new { token = token }),
                404 => NotFound(new { message = "UserNotFound" }),
                401 => Unauthorized(new { message = "WrongPassword" }),
                403 => Forbid(),
                _ => BadRequest()
            };
        }
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult Get()
        {
            return Ok(userService.GetAll());
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult Get(int id)
        {
            var user = userService.GetById(id);
            if (user == null)
                return NotFound(new { message = "המשתמש לא נמצא" });
            return Ok(user);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Post([FromBody] UserDto item)
        {
            if (item == null) return BadRequest(new { message = "נתונים חסרים" });
            var createdUser = await userService.Add(item);
            if (createdUser == null)
            {
                return BadRequest(new { message = "משתמש עם אימייל זה כבר קיים במערכת" });
            }
            return Ok(new
            {
                message = "נרשמת בהצלחה למערכת סיטי קאר!",
                data = createdUser
            });
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "user")]
        public IActionResult Put(int id, [FromBody] UserDto item)
        {
            var result = userService.Update(id, item);
            if (!result)
                return BadRequest(new { message = "עדכון נכשל: משתמש לא נמצא או אימייל תפוס" });

            return Ok(new { message = "פרטי המשתמש עודכנו בהצלחה" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult Delete(int id)
        {
            var result = userService.Delete(id);
            if (!result)
                return NotFound(new { message = "מחיקה נכשלה: משתמש לא נמצא" });

            return Ok(new { message = "המשתמש הוסר מהמערכת" });
        }
        [HttpGet("email/{email}")]
        [Authorize(Roles = "admin")]
        public IActionResult GetByEmail(string email)
        {
            var user = userService.GetByEmail(email);
            if (user == null)
                return NotFound(new { message = "משתמש לא נמצא" });
            return Ok(user);
        }

        [HttpPatch("change-password")]
        [Authorize]
        public IActionResult ChangePassword(int userId, string oldPassword, string newPassword)
        {
            var result = userService.ChangePassword(userId, oldPassword, newPassword);
            if (!result)
                return BadRequest(new { message = "שינוי הסיסמה נכשל. וודא שהפרטים נכונים ושאינך מנסה לשנות סיסמה למשתמש אחר" });

            return Ok(new { message = "הסיסמה עודכנה בהצלחה" });
        }

        [HttpPatch("toggle-block/{userId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ToggleBlockUser(int userId)
        {
            var result = userService.ToggleBlockUser(userId);
            if (!result)
                return NotFound(new { message = "עדכון סטטוס חסימה נכשל - משתמש לא נמצא" });

            return Ok(new { message = "סטטוס חסימת המשתמש עודכן בהצלחה" });
        }

        [HttpGet("current")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var user = userService.GetCurrentUser();
            if (user == null)
                return Unauthorized(new { message = "לא נמצא משתמש מחובר" });
            return Ok(user);
        }

        [HttpGet("count-user")]
        [Authorize(Roles = "admin")]
        public IActionResult GetTotalUsersCount()
        {
            var count = userService.GetTotalUsersCount();
            return Ok(new { total = count });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            var result = await userService.RequestPasswordReset(email);
            return Ok(new { message = "אם המייל קיים במערכת, קוד איפוס נשלח אליו כעת" });
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromQuery] string email, [FromQuery] string code, [FromQuery] string newPassword)
        {
            var result = userService.ResetPassword(email, code, newPassword);

            if (!result)
            {
                return BadRequest(new { message = "האיפוס נכשל: קוד שגוי או פג תוקף" });
            }

            return Ok(new { message = "הסיסמה עודכנה בהצלחה" });
        }
        [HttpPost("request-registration-code")]
        public async Task<IActionResult> RequestCode([FromQuery] string email)     {
            try
            {
                var result = await userService.RequestRegistrationCode(email);
                if (!result)
                    return BadRequest(new { message = "המייל כבר קיים במערכת" });

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-registration-code")]
        public IActionResult VerifyCode([FromQuery] string email, [FromQuery] string code)
        {
            // פענוח של תווים מקודדים מה-URL
            string decodedEmail = Uri.UnescapeDataString(email);

            var isValid = userService.VerifyRegistrationCode(decodedEmail, code);

            if (!isValid) return BadRequest(new { message = "קוד שגוי או פג תוקף" });
            return Ok(new { success = true });
        }
    }
}
