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
            throw new NotImplementedException();
        }
    }
}
