using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Common.Dto
{
    public class CarDto
    {
        public int Id { get; set; }
        public string Model { get; set; }
        public string LicensePlate { get; set; }
        public string? ImageUrl { get; set; }
        public int Seats { get; set; }
        public string Status { get; set; } 
        public int FuelLevel { get; set; }
        public decimal PricePerHour { get; set; }
        public string StartParking { get; set; }
        public int RegionId { get; set; }
        public string? RegionName { get; set; } 
    }
}