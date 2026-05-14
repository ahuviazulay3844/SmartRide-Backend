using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public enum OrderStatus { Pending = 0, Active = 1, Completed = 2, Canceled = 3 }// (מצב הזמנה (ממתינה, פעילה, הושלמה, בוטלה

    public class OrderDto
    {

        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime ExpectedEndTime { get; set; }
        public DateTime? EndTime { get; set; } // זמן סיום בפועל
        public int TotalDays { get; set; }// כמה ימים הוזמן
        public int TotalHours { get; set; }// כמה שעות הוזמן
        public DateTime? ActualOpeningTime { get; set; } // הזמן בו נלחץ "פתח רכב" לראשונה
        public bool IsInspectionSubmitted { get; set; } // האם השאלון כבר מולא?

        // --- שדות מחיר מפורטים לקבלה ---
        public decimal BasePrice { get; set; }      // מחיר הנסיעה המקורי
        public decimal LateFee { get; set; }       // סכום הקנס על איחור (אם יש)
        public decimal DiscountAmount { get; set; } // סכום ההנחה/פיצוי (אם יש)
        public decimal TotalPrice { get; set; }    // מחיר סופי אחרי הכל
        public bool WantsInsuranceUpgrade { get; set; } = false;
        public OrderStatus Status { get; set; } = OrderStatus.Pending; //מצב הזמנה
        public bool IsPaid { get; set; }
        public int UserId { get; set; }
        public string? UserFullName { get; set; }   // שם הלקוח לפנייה אישית
        public int CarId { get; set; }
        public string? CarModel { get; set; }
        public string PricingType { get; set; }
        public int? DistanceDrivenKm { get; set; }  // קילומטראז' שבוצע

        public bool IsReassigned { get; set; } = false;
        // בתוך הקובץ Order.cs
        public bool HasConflict { get; set; } = false;
        public int? SuggestedReplacementCarId { get; set; }

    }
}
