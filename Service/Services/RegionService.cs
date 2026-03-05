using AutoMapper;
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
        private readonly IMapper _mapper;


        public RegionService(IRepository<Region> regionRepository, IMapper mapper)
        {
            _regionRepository = regionRepository;
            _mapper = mapper;
        }
        public async Task<RegionDto> Add(RegionDto item)
        {
            var exists = _regionRepository.GetAll().Any(r => r.Name == item.Name);
            if (exists) return null;
            var region = _mapper.Map<Region>(item);
            var saved = _regionRepository.Add(region);
            return _mapper.Map<RegionDto>(saved);           
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
            var regions = _regionRepository.GetAll();
            return _mapper.Map<IEnumerable<RegionDto>>(regions);
        }
        
        public RegionDto? GetById(int id)
        {
            var region = _regionRepository.GetById(id);
            if (region == null) return null;
            return _mapper.Map<RegionDto>(region);
        }

        public RegionDto GetByName(string name)
        {
            var region = _regionRepository.GetAll().FirstOrDefault(r => r.Name == name);
            return _mapper.Map<RegionDto>(region);
            }

        public int GetTotalRegionsCount()
        {
            return _regionRepository.GetAll().Count();
        }

        public bool Update(int id, RegionDto item)
        {
            var existingRegion = _regionRepository.GetById(id);
            if (existingRegion == null) return false;
            var isNameTaken = _regionRepository.GetAll().Any(r => r.Name == item.Name && r.Id != id);
            if (isNameTaken)
            {
               return false;
            }
            _mapper.Map(item, existingRegion);
            return _regionRepository.Update(id, existingRegion);
        }

        public bool UpdateCenterPoint(int regionId, double lat, double lng)
        {
            var region = _regionRepository.GetById(regionId);
            if (region == null) return false;
            region.CenterLatitude = lat;
            region.CenterLongitude = lng;
            return _regionRepository.Update(regionId, region);
        }
    }
    
}
