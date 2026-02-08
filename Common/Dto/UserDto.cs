using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Common.Dto
{
    public enum UserType {user,admin }
    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Rank { get; set; }
        public bool IsBlocked { get; set; }
        public string LicenseNumber { get; set; }
        public string UserType { get; set; }
    }
}