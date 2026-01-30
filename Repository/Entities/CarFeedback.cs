using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repository.Entities
{
    //טבלת פידבקים על רכבים
    public class CarFeedback
    {
        [Key]
        public int Id { get; set; }//מס זיהוי לפידבק

        // --- קישור לרכב ---
        [Required]
        public int CarId { get; set; }//מזהה רכב

        [ForeignKey("CarId")]
        public virtual Car Car { get; set; }// קישור לטבלת רכבים



        // --- קישור למשתמש ---
        public int UserId { get; set; }//מזהה משתמש
        public virtual User User { get; set; } // קישור לטבלת משתמשים



        // --- נתוני הפידבק ---

        [Range(1, 5)]
        public int Rating { get; set; } // דירוג כוכבים:  1-5
       public string? UserComment { get; set; }//טקסט חופשי של המשתמש
        public bool ReportedIssue { get; set; }//האם דווחה תקלה?
        public DateTime DateCreated { get; set; } = DateTime.Now;// תאריך יצירה אוטומטי של הדיווח


        //קישורים לטבלאות אחרות


        // --- קישור להזמנה  ---

        public int OrderId { get; set; } // קישור להזמנה הספציפית
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
    }
}