using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Repository.Repositories
{
    public class CarInspectionRepository : IRepository<CarInspection>
    {
        private readonly IContext context;

        public CarInspectionRepository(IContext context)
        {
            this.context = context;
        }

        public CarInspection Add(CarInspection item)
        {
            context.CarInspections.Add(item);
            context.Save();
            return item;
        }

        public IEnumerable<CarInspection> GetAll()
        {
            return context.CarInspections.AsQueryable();
        }

        public CarInspection? GetById(int id)
        {
            return context.CarInspections.Find(id);
        }

        public bool Exists(int id)
        {
            return context.CarInspections.Any(x => x.Id == id);
        }

        public bool Update(int id, CarInspection item)
        {
            var existing = context.CarInspections.Find(id);
            if (existing == null) return false;

            existing.IsCleanInside = item.IsCleanInside;
            existing.IsCleanOutside = item.IsCleanOutside;
            existing.IsAicConditionWorking = item.IsAicConditionWorking;
            existing.AnyNewDamage = item.AnyNewDamage;
            existing.DamageDescription = item.DamageDescription;
            existing.CarId = item.CarId;
            existing.UserId = item.UserId;
            existing.OrderId = item.OrderId;

            context.Save();
            return true;
        }

        public bool Delete(int id)
        {
            var item = context.CarInspections.Find(id);
            if (item != null)
            {
                context.CarInspections.Remove(item);
                context.Save();
                return true;
            }
            return false;
        }
    }
}