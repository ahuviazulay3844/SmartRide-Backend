using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repositories
{
    public class CouponRepository : IRepository<Coupon>
    {
        private readonly IContext context;
        public CouponRepository(IContext context)
        {
            this.context = context;
        }

        public Coupon Add(Coupon item)
        {
            context.Coupons.Add(item);
            context.Save();
            return item;
        }

        public bool Delete(int id)
        {
            var coupon = context.Coupons.Find(id);
            if (coupon != null)
            {
                context.Coupons.Remove(coupon);
                context.Save();
                return true;
            }
            return false;
        }

        public bool Exists(int id)
        {
            return context.Coupons.Any(x => x.Id == id);
        }

        public IEnumerable<Coupon> GetAll()
        {
            return context.Coupons.AsQueryable();
        }

        public Coupon? GetById(int id)
        {
            return context.Coupons.Find(id);
        }

        public bool Update(int id,Coupon item)
        {
          
            return false;
        }
    }
}
