using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Common.Dto
{
    public class RegionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double? CenterLatitude { get; set; }
        public double? CenterLongitude { get; set; }
    }
}
