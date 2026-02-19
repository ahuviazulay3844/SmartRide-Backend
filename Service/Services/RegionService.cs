using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class RegionService : IRegionService
    {
        private readonly IRepository<Region> _regionRepository;

        public RegionService(IRepository<Region> regionRepository)
        {
            _regionRepository = regionRepository;
        }
        public RegionDto Add(RegionDto item)
        {
           if(!Exists(item.Id))
            {
                var region = new Region
                {
                    Id = item.Id,
                    Name = item.Name,
                    CenterLatitude = item.CenterLatitude,
                    CenterLongitude = item.CenterLongitude
                };
                _regionRepository.Add(region);
                return item;
            }
            else
            {
                throw new Exception("Region with the same ID already exists.");
            }
        }

        public bool Delete(int id)
        {
            if (!Exists(id)) return false;
            return _regionRepository.Delete(id);
        }

        public bool Exists(int id)
        {
            return _regionRepository.Exists(id);
        }

        public IEnumerable<RegionDto> GetAll()
        {
            return _regionRepository.GetAll().Select(r => new RegionDto
            {
                Id = r.Id,
                Name = r.Name,
                CenterLatitude = r.CenterLatitude,
                CenterLongitude = r.CenterLongitude
            }).ToList();
        }
        
        public RegionDto? GetById(int id)
        {
            var region = _regionRepository.GetById(id);
            if (region == null) return null;

            return new RegionDto
            {
                Id = region.Id,
                Name = region.Name,
                CenterLatitude = region.CenterLatitude,
                CenterLongitude = region.CenterLongitude
            };
        }

        public bool Update(int id, RegionDto item)
        {
            var existingRegion = _regionRepository.GetById(id);
            if (existingRegion == null) return false;

            var isNameTaken = _regionRepository.GetAll().Any(r => r.Name == item.Name && r.Id != id);
            if (isNameTaken)
            {
                throw new Exception("קיימים נתונים זהים באזור אחר");
            }

            existingRegion.Name = item.Name;
            existingRegion.CenterLatitude = item.CenterLatitude;
            existingRegion.CenterLongitude = item.CenterLongitude;
            return _regionRepository.Update(id, existingRegion);
        }
    }
    
}
