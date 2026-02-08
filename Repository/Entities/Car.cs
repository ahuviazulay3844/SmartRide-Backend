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

    //(מצב רכב (זמין, חלקית, תפוס
    public enum CarStatus {  Available, PartiallyBooked, Occupied}


    //טבלת רכבים
    public class Car
    {
        // --- מזהים ומידע בסיסי ---
        [Key]
        public int Id { get; set; } //מזהה רכב   
        [Required]
        public string Model { get; set; } //דגם רכב
        [Required]
        public string LicensePlate { get; set; }//מס רישוי
        public string ? ImageUrl { get; set; } //תמונת רכב
        public int Seats { get; set; }//מס מקומות ברכב


        // ---(מצב תפעולי (סטטוס ---
        public CarStatus Status { get; set; } // פנוי/תפוס/מוזמן
        public bool IsLocked { get; set; }= false; //האם נעול
        public DateTime? LastLockTime { get; set; } // זמן נעילה אחרון בפועל
        public bool NeedsMaintenance { get; set; } = false; // האם הרכב דורש תחזוקה/ניקיון?
        public string? MaintenanceNotes { get; set; } // פירוט התקלה הנוכחית
        public int FuelLevel { get; set; } //(רמת הדלק (1.0 = מלא, 0.5 = חצי     
        public int Kilometers { get; set; } // קילומטרז נוכחי


        // --- מחירון ---

        public decimal  PricePerHour { get; set; } //מחיר לשעה
        public decimal  PricePerDay { get; set; } //מחיר ליום
        public decimal  PricePerKm { get; set; } //מחיר לקילומטר


        //מיקום רכב


        [Required]
        public string StartParking { get; set; } //חנית התחלה
        public double Latitude { get; set; } // קו רוחב      
        public double Longitude { get; set; }  // קו אורך


        // --- סטטיסטיקה ושיווק  ---
        public int TotalOrdersCount { get; set; } = 0; // כמה פעמים הרכב הוזמן בסך הכל
        public bool IsPopular => TotalOrdersCount > 10; // משתנה שמחשב: אם הוזמן מעל 10 פעמים הוא  פופולרי  


        //----- קישורים לטבלאות אחרות -----


        // --- קישור לאזור  ---
        public int RegionId { get; set; }//מזהה אזור
        [ForeignKey("RegionId")]
        public virtual Region Region { get; set; } // קישור לטבלת אזורים

        // --- קשרים ורשימות ---
        public virtual ICollection<CarFeedback> Feedbacks { get; set; } = new List<CarFeedback>();//רשימת פידבקים על הרכב זה

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>(); // קישור  לרשימת ההזמנות של הרכב הזה

    }
}
