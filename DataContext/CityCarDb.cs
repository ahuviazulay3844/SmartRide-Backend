using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repository.Entities;
using Repository.Interfaces;

namespace DataContext
{
    //מחלקת הקשר לבסיס הנתונים - CityCarDb
    public class CityCarDb: DbContext,IContext
    {
        // טבלאות המערכת - DbSets
        public DbSet<User> Users { get; set; }// טבלת משתמשים
        public DbSet<Car> Cars { get; set; }// טבלת רכבים
        public DbSet<Order> Orders { get; set; }// טבלת הזמנות
        public DbSet<Region> Regions { get; set; }// טבלת אזורים
        public DbSet<Coupon> Coupons { get; set; }// טבלת קופונים
        public DbSet<CarFeedback> Feedbacks { get; set; }// טבלת פידבקים

        public void Save()
        {
            SaveChanges();
        }

        //  חיבור לבסיס הנתונים
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("server=DESKTOP-1VUANBN;database=CityCarDB;trusted_connection=true;TrustServerCertificate=True");
        }









        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
             modelBuilder.Entity<Car>().Property(c => c.PricePerHour).HasPrecision(18, 2);
             modelBuilder.Entity<Order>().Property(o => o.TotalPrice).HasPrecision(18, 2);
             modelBuilder.Entity<Coupon>().Property(cp => cp.DiscountAmount).HasPrecision(18, 2);

             modelBuilder.Entity<Order>()
            .HasOne(o => o.Feedback)
            .WithOne(f => f.Order)
            .HasForeignKey<CarFeedback>(f => f.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

             modelBuilder.Entity<CarFeedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

             modelBuilder.Entity<CarFeedback>()
                .HasOne(f => f.Car)
                .WithMany()
                .HasForeignKey(f => f.CarId)
                .OnDelete(DeleteBehavior.Restrict);

        }


    }
}
