using Microsoft.EntityFrameworkCore;
using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Repository.Repositories
{
    public class OrderRepository : IRepository<Order>
    {
        private readonly IContext context;

        public OrderRepository(IContext context)
        {
            this.context = context;
        }
        public Order Add(Order item)
        {
            context.Orders.Add(item);
            context.Save();
            return item;
        }

        public bool Delete(int id)
        {
            var order = context.Orders.Find(id);
            if (order != null)
            {
                context.Orders.Remove(order);
                context.Save();
                return true;
            }
            return false;
        }

        public bool Exists(int id)
        {
            return context.Orders.Any(x => x.Id == id);
        }

        //public IEnumerable<Order> GetAll()
        //{
        //    return context.Orders.AsQueryable();
        //}

        //public Order? GetById(int id)
        //{
        //    return context.Orders.Find(id);
        //}
        // Repository/Repositories/OrderRepository.cs

        public IEnumerable<Order> GetAll()
        {
            // נשתמש ב-Include כדי להביא את הנתונים המקושרים
            if (context is Microsoft.EntityFrameworkCore.DbContext dbContext)
            {
                return dbContext.Set<Order>()
                    .Include(o => o.Car)  // זה מה שחסר כדי ש-CarModel יעבוד
                    .Include(o => o.User) // זה מה שחסר כדי ש-UserFullName יעבוד
                    .AsQueryable();
            }
            return context.Orders.AsQueryable();
        }
        public Order? GetById(int id)
        {
            if (context is Microsoft.EntityFrameworkCore.DbContext dbContext)
            {
                return dbContext.Set<Order>()
                    .Include(o => o.Car)
                    .Include(o => o.User)
                    .FirstOrDefault(o => o.Id == id);
            }
            return context.Orders.Find(id);
        }

        //public bool Update(int id, Order item)
        //{
        //    var existing = context.Orders.Find(id);
        //    if (existing == null) return false;

        //    if (context is Microsoft.EntityFrameworkCore.DbContext dbContext)
        //    {
        //        dbContext.Entry(existing).CurrentValues.SetValues(item);
        //    }

        //    context.Save();
        //    return true;
        //}
        public bool Update(int id, Order item)
        {
            var existing = context.Orders.Find(id);
            if (existing == null) return false;

            // 1. נתק את הישות הקיימת מהמעקב אם היא קיימת (אופציונלי אך בטוח)
            // 2. השתמש ב-SetValues רק על המאפיינים הפשוטים (Scalars)
            if (context is Microsoft.EntityFrameworkCore.DbContext dbContext)
            {
              
                dbContext.Entry(existing).CurrentValues.SetValues(item);

                dbContext.Entry(existing).Reference(o => o.Car).IsModified = false;
                dbContext.Entry(existing).Reference(o => o.User).IsModified = false;
            }

            context.Save();
            return true;
        }
    }
}
