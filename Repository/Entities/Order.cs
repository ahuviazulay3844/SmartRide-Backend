using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repository.Entities
{
    public enum OrderStatus { Pending = 0, Active = 1, Completed = 2, Canceled = 3 }
    public enum PricingType { ByHour, ByDay }// סוג תמחור: לפי שעה/ לפי יום

    //טבלת הזמנות   
    public class Order
    {
       
        [Key]
        public int Id { get; set; } //מזהה הזמנה


        // --- זמנים (לחישוב קנס איחור) ---
        public DateTime StartTime { get; set; } //זמן התחלה של ההזמנה
        public DateTime ExpectedEndTime { get; set; } //זמן שצריך להסתיים ההזמנה-לפי מה שהוזמן  
        public int TotalDays { get; set; }// כמה ימים הוזמן
        public int TotalHours { get; set; }// כמה שעות הוזמן
        public DateTime? EndTime { get; set; } //זמן סיום של ההזמנה בפועל  
        public DateTime? ActualOpeningTime { get; set; } // הזמן בו נלחץ "פתח רכב" לראשונה
        public bool IsInspectionSubmitted { get; set; } // האם השאלון כבר מולא?

        // --- קילומטראז' - לחישוב מרחק  ---
        public int StartMileage { get; set; } // כמה היה לרכב כשיצא
        public int? EndMileage { get; set; } // כמה היה לרכב כשחזר
        public int? DistanceDrivenKm { get; set; } // כמה קילומטרים נסע בפועל
        // --- מחירים ותשלומים ---
        public decimal BasePrice { get; set; } // מחיר לפי ימים/שעות
        public decimal LateFee { get; set; } = 0; // קנס איחור-מחושב
        public decimal TotalPrice { get; set; } //מחיר סופי של ההזמנה
        public bool WantsInsuranceUpgrade { get; set; } = false; // ביטול השתתפות עצמית
        public PricingType PricingType { get; set; } // סוג תמחור: לפי שעה/ לפי יום


        // --- ניהול וסטטוס ---
        public OrderStatus Status { get; set; } = OrderStatus.Pending; //מצב הזמנה
        public bool IsPaid { get; set; } = false; //האם ההזמנה שולמה


        // --- דלק ודיווחים ---
        public bool DidCustomerRefuel { get; set; } = false; // האם מילא דלק-בשביל הבונוס
        //public bool ReportedDirty { get; set; } = false; // האם דווח שהחזיר מלוכלך-אם יערער
        public string? ConditionNotes { get; set; } // יקבל מהמייל- הערות חופשיות על מצב הרכב

        public virtual CarInspection? Inspection { get; set; } // מאפשר גישה לדיווח מתוך ההזמנה
        public bool IsReassigned { get; set; } = false;
        public decimal DiscountAmount { get; set; } = 0;
        // בתוך הקובץ Order.cs
        public bool HasConflict { get; set; } = false;
        public int? SuggestedReplacementCarId { get; set; }

        //קישורים לטבלאות אחרות


        // --- קישור למשתמש ---
        public int UserId { get; set; } //מזהה משתמש
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // --- קישור לרכב ---
        public int CarId { get; set; } //מזהה רכב
        [ForeignKey("CarId")]
        public virtual Car Car { get; set; }

        // --- קישור לפידבק - אופציונלי ---

        public virtual CarFeedback? Feedback { get; set; }


        // --- קישור לקופון - אופציונלי ---
        public int? CouponId { get; set; } //מזהה קופון
        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }

    }
}