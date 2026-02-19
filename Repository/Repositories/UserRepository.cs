using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repositories
{
    public class UserRepository : IRepository<User>
    {
        private readonly IContext context;
        public UserRepository(IContext context)
        {
            this.context = context;
        }
        public User Add(User item)
        {
            context.Users.Add(item);
            context.Save();
            return item;
        }

        public bool Delete(int id)
        {
            var user = context.Users.Find(id);
            if (user != null)
            {
                context.Users.Remove(user);
                context.Save();
                return true;
            }
            return false;
        }

        public bool Exists(int id)
        {
            return context.Users.Any(x => x.Id == id);
        }

        public IEnumerable<User> GetAll()
        {
            return context.Users.AsQueryable();
        }

        public User? GetById(int id)
        {
            return context.Users.Find(id);
        }

        public bool Update(int id, User item)
        {
            var existingUser = context.Users.Find(id);
            if (existingUser == null) return false;

            // עדכון פרטים אישיים
            existingUser.FirstName = item.FirstName;
            existingUser.LastName = item.LastName;
            existingUser.Email = item.Email;
            existingUser.PhoneNumber = item.PhoneNumber;
            existingUser.PasswordHash = item.PasswordHash;
            existingUser.DateOfBirth = item.DateOfBirth;
            existingUser.UserType = item.UserType;

            existingUser.LicenseNumber = item.LicenseNumber;
            existingUser.LicenseExpirationDate = item.LicenseExpirationDate;
            existingUser.IsLicenseVerified = item.IsLicenseVerified;
            existingUser.IsNewDriver = item.IsNewDriver;

            existingUser.Rank = item.Rank;
            existingUser.AccountBalance = item.AccountBalance;
            existingUser.IsBlocked = item.IsBlocked;


            context.Save();
            return true;
        }

    }
}
