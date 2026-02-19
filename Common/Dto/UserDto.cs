using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Common.Dto
{
    public enum UserType {user,admin }
    public class UserDto
    {
        public int Id { get; set; }
        [MinLength(2, ErrorMessage = "שם פרטי חייב להכיל לפחות 2 אותיות")]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [EmailAddress(ErrorMessage = "פורמט אימייל לא תקין")]
        public string Email { get; set; }
        [Phone(ErrorMessage = "מספר טלפון לא תקין")]
        public string PhoneNumber { get; set; }
        public string Rank { get; set; }
        public string? Password { get; set; }
        public bool IsBlocked { get; set; }
        public string LicenseNumber { get; set; }
        public string UserType { get; set; }
    }
}