using Repository.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{

    public enum CarStatus { Available = 0, PartiallyBooked = 1, Occupied = 2, Maintenance = 3 }
    public enum CarCategory { Mini, Family, Large, Commercial, Luxury }

    public class Car
{
        // ---Identifiers and basic information ---
        [Key]
        public int Id { get; set; } 
        [Required]
        public string Model { get; set; } 
        [Required]
        public string LicensePlate { get; set; }
        public string ? ImageUrl { get; set; } 
        public int Seats { get; set; }
        public int Year { get; set; }


    // ---Operational (status) ---
        public CarCategory CarCategory { get; set; }
        public CarStatus Status { get; set; } 
        public bool IsLocked { get; set; }= false; 
        public DateTime? LastLockTime { get; set; }
        public bool NeedsMaintenance { get; set; } = false; 
        public string? MaintenanceNotes { get; set; } 
        public int FuelLevel { get; set; } 
        public int Kilometers { get; set; } 


        // --- Pricing ---

        public decimal  PricePerHour { get; set; }
        public decimal  PricePerDay { get; set; } 
        public decimal  PricePerKm { get; set; }


        // --- Location ---

        [Required]
        public string StartParking { get; set; } 
        public double Latitude { get; set; }
        public double Longitude { get; set; } 


        // --- Statistics and Marketing ---
        public int TotalOrdersCount { get; set; } = 0; 
        public bool IsPopular => TotalOrdersCount > 20;  




        public int RegionId { get; set; }
        [ForeignKey("RegionId")]
        public virtual Region Region { get; set; } 

        public virtual ICollection<CarFeedback> Feedbacks { get; set; } = new List<CarFeedback>();

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    }
}
