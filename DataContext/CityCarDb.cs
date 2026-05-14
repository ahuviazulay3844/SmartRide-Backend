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
    public class CityCarDb : DbContext, IContext
    {
        // 1. הוספת בנאי שמקבל אפשרויות - קריטי ל-Worker!
        public CityCarDb(DbContextOptions<CityCarDb> options) : base(options)
        {
        }

        // 2. בנאי ריק (אופציונלי, למקרה של Migration או שימוש ידני)
        public CityCarDb() { }

        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CarFeedback> Feedbacks { get; set; }
        public DbSet<CarInspection> CarInspections { get; set; }


        public void Save()
        {
            SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // אם לא הוגדרו אפשרויות מבחוץ (כמו ב-Worker), נשתמש בברירת המחדל
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("server=DESKTOP-1VUANBN;database=CityCarDB;trusted_connection=true;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // תיקון שגיאת ה-Cascade בטבלת הבדיקות
            modelBuilder.Entity<CarInspection>()
                .HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CarInspection>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CarInspection>()
                .HasOne(i => i.Car)
                .WithMany()
                .HasForeignKey(i => i.CarId)
                .OnDelete(DeleteBehavior.Restrict);

            // תיקון אזהרות ה-Decimal (כדי שלא יהיו חיתוכי מספרים)
            modelBuilder.Entity<Car>().Property(c => c.PricePerHour).HasPrecision(18, 2);
            modelBuilder.Entity<Car>().Property(c => c.PricePerDay).HasPrecision(18, 2);
            modelBuilder.Entity<Car>().Property(c => c.PricePerKm).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.TotalPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.BasePrice).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.LateFee).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(cp => cp.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(cp => cp.MinimumOrderAmount).HasPrecision(18, 2);
            modelBuilder.Entity<User>().Property(u => u.AccountBalance).HasPrecision(18, 2);

            // שאר ההגדרות הקיימות שלך (Feedback וכו')
            modelBuilder.Entity<Order>()
               .HasOne(o => o.Feedback)
               .WithOne(f => f.Order)
               .HasForeignKey<CarFeedback>(f => f.OrderId)
               .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
