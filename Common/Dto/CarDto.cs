using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Common.Dto
{
    public enum CarStatus { Available = 0, PartiallyBooked = 1, Occupied = 2, Maintenance = 3 }
    public enum CarCategory { Mini, Family, Large, Commercial, Luxury }
    public class CarDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="שדה חובה")]
        public string Model { get; set; }
        public CarCategory Category { get; set; }

        public string CategoryName =>
            Category switch
            {
                CarCategory.Mini => "מיני",
                CarCategory.Family => "משפחתי",
                CarCategory.Large => "גדול",
                CarCategory.Commercial => "מסחרי",
                CarCategory.Luxury => "יוקרה",
                _ => ""
            };
        public int Kilometers { get; set; }    
        public string LicensePlate { get; set; }
        public string? ImageUrl { get; set; }
        public int Seats { get; set; }
        public CarStatus Status { get; set; } 
        public int FuelLevel { get; set; }
        public decimal PricePerHour { get; set; }
        public string StartParking { get; set; }
        public int RegionId { get; set; }
        public string? RegionName { get; set; }
        public int Year { get; set; }
        public bool IsPopular { get; set; } 
        public int TotalOrdersCount { get; set; }
        public double Latitude { get; set; } // קו רוחב      
        public double Longitude { get; set; }  // קו אורך
        public double Distance { get; set; }
        public bool IsLocked { get; set; } = false; //האם נעול
        public decimal PricePerDay { get; set; } 
        public decimal PricePerKm { get; set; }
        public DateTime? BlockingOrderStart { get; set; }
        public DateTime? BlockingOrderEnd { get; set; }

    }
}