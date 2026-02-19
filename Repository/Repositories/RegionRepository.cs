using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repositories
{


    public class RegionRepository : IRepository<Region>
    {
        private readonly IContext context;

        public RegionRepository(IContext context)
        {
            this.context = context;
        }
        public Region Add(Region item)
        {
            context.Regions.Add(item);
            context.Save();
            return item;
        }

        public bool Delete(int id)
        {
            var region = context.Regions.Find(id);
            if (region != null)
            {
                context.Regions.Remove(region);
                context.Save();
                return true;
            }
            return false;
        }

        public bool Exists(int id)
        {
            return context.Regions.Any(x => x.Id == id);
        }

        public IEnumerable<Region> GetAll()
        {
            return context.Regions.AsQueryable();
        }

        public Region? GetById(int id)
        {
            return context.Regions.Find(id);
        }

        public bool Update(int id, Region item)
        {
            var existingRegion = context.Regions.Find(id);

            if (existingRegion != null)
            {
                existingRegion.Name = item.Name;
                existingRegion.CenterLatitude = item.CenterLatitude;
                existingRegion.CenterLongitude = item.CenterLongitude;

                context.Save();
                return true;
            }

            return false;
        }

    }
}

