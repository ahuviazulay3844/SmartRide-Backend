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

        public IEnumerable<Order> GetAll()
        {
            return context.Orders.AsQueryable();
        }

        public Order? GetById(int id)
        {
            return context.Orders.Find(id);
        }

        public bool Update(int id, Order item)
        {
            var existingOrder = context.Orders.Find(id);
            if (existingOrder == null) return false;
            existingOrder.Status = item.Status;
            existingOrder.IsPaid = item.IsPaid;
            existingOrder.EndTime = item.EndTime;
            existingOrder.EndMileage = item.EndMileage;
            existingOrder.DistanceDrivenKm = item.DistanceDrivenKm;
            existingOrder.LateFee = item.LateFee;
            existingOrder.TotalPrice = item.TotalPrice;
            existingOrder.ConditionNotes = item.ConditionNotes;
            existingOrder.DidCustomerRefuel = item.DidCustomerRefuel;
            context.Save();
            return true;
        }

    }
}
