using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repositories
{
    public class CarRepository : IRepository<Car>
    {
        private readonly IContext context;

        public CarRepository(IContext context)
        {
            this.context = context;
        }

        public Car Add(Car item)
        {
            context.Cars.Add(item);
            context.Save();
            return item;
        }

        public bool Delete(int id)
        {
            var car = context.Cars.Find(id);
            if (car != null)
            {
                context.Cars.Remove(car);
                context.Save();
                return true;
            }
            return false;
        }

        public bool Exists(int id)
        {
            return context.Cars.Any(x=>x.Id == id);
        }

        public IEnumerable<Car> GetAll()
        {
           return context.Cars.AsQueryable();
        }

        public Car? GetById(int id)
        {
            return context.Cars.Find(id);
        }

        public bool Update(int id, Car item)
        {
            var carToUpdate = context.Cars.Find(id);
            if (carToUpdate == null) return false;
            carToUpdate.Model = item.Model;
            carToUpdate.FuelLevel = item.FuelLevel;
            carToUpdate.Kilometers = item.Kilometers;
            carToUpdate.Status = item.Status;
            carToUpdate.PricePerHour = item.PricePerHour;
            carToUpdate.PricePerDay = item.PricePerDay;
            carToUpdate.PricePerKm = item.PricePerKm;
            carToUpdate.Seats = item.Seats;
            carToUpdate.ImageUrl = item.ImageUrl;
            carToUpdate.LicensePlate = item.LicensePlate;
            carToUpdate.NeedsMaintenance = item.NeedsMaintenance;
            context.Save();
            return true;
        }
    
    }
}
