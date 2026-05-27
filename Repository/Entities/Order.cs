using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repository.Entities
{
    public enum OrderStatus { Pending = 0, Active = 1, Completed = 2, Canceled = 3 }
    public enum PricingType { ByHour, ByDay }

    //טבלת הזמנות   
    public class Order
    {
       
        [Key]
        public int Id { get; set; }

        // ---Times (for calculating late payment fines)---
        public DateTime StartTime { get; set; } 
        public DateTime ExpectedEndTime { get; set; }   
        public int TotalDays { get; set; }
        public int TotalHours { get; set; }
        public DateTime? EndTime { get; set; } 
        public DateTime? ActualOpeningTime { get; set; } 
        public bool IsInspectionSubmitted { get; set; }

        // --- Mileage for distance calculation  ---
        public int StartMileage { get; set; } 
        public int? EndMileage { get; set; } 
        public int? DistanceDrivenKm { get; set; } 

        // --- pricing ---
        public decimal BasePrice { get; set; } 
        public decimal LateFee { get; set; } = 0; 
        public decimal TotalPrice { get; set; } 
        public bool WantsInsuranceUpgrade { get; set; } = false; 
        public PricingType PricingType { get; set; }

        // --- Management and status  ---
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public bool IsPaid { get; set; } = false;

        // --- Conflict ---
        public bool HasConflict { get; set; } = false;
        public int? SuggestedCarSeats { get; set; }
        public int? SuggestedCarFuelLevel { get; set; }
        public string? SuggestedCarModel { get; set; }
        public string? SuggestedCarLocation { get; set; }
        public int? SuggestedReplacementCarId { get; set; }

        // --- Fuel and reports ---
        public bool DidCustomerRefuel { get; set; } = false; 
        public string? ConditionNotes { get; set; }
        public string? ConflictReason { get; set; }
        public virtual CarInspection? Inspection { get; set; } 
        public bool IsReassigned { get; set; } = false;
        public decimal DiscountAmount { get; set; } = 0;     



        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        public int CarId { get; set; } 
        [ForeignKey("CarId")]
        public virtual Car Car { get; set; }
        public virtual CarFeedback? Feedback { get; set; }
        public int? CouponId { get; set; } 
        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }

    }
}