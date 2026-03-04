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
        public IActionResult Login([FromBody] LoginDto l)
        {
            var token = userService.Login(l);
            if (token == null )
                return Unauthorized(new { message = "שם משתמש או סיסמה שגויים" });
            return Created("",new { message = "התחברת בהצלחה, ברוך הבא לסיטי קאר!", token = token });
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

        [HttpPost]
        public IActionResult Post([FromBody] UserDto item)
        {
            var created = userService.Add(item);
            if (created == null)
                return BadRequest(new { message = "משתמש עם אימייל זה כבר קיים במערכת" });

            return Created("", new { message = "נרשמת בהצלחה למערכת סיטי קאר!", data = created });
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
        public IActionResult ToggleBlockUser(int userId)
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
    }
}
