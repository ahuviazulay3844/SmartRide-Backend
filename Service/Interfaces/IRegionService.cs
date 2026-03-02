using Common.Dto;
using Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IRegionService: IService<RegionDto>
    {
        RegionDto GetByName(string name);
        int GetTotalRegionsCount();
        bool UpdateCenterPoint(int regionId, double lat, double lng);
    }
}
