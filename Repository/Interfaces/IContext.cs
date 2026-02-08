using Microsoft.EntityFrameworkCore;
using Repository.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IContext
    {
        DbSet<User> Users { get; set; }
        DbSet<Car> Cars { get; set; }
        DbSet<Order> Orders { get; set; }
        DbSet<Region> Regions { get; set; }
        DbSet<Coupon> Coupons { get; set; }
        DbSet<CarFeedback> Feedbacks { get; set; }
        public void Save();

    }
}
