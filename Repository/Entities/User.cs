using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repository.Entities
{
    public enum UserType { Customer, Administrator } // (סוג משתמש (לקוח, מנהל מערכת
    public enum UserRank { Regular, Bronze, Silver, Gold, PurpleBadge }// דרגת משתמש

    //טבלת משתמשים
    public class User
    {


        // --- מידע אישי וגישה ---

        [Key]
        public int Id { get; set; } //מזהה משתמש
        [Required]
        public string PasswordHash { get; set; } // חובה לכניסה למערכת
        [Required, MaxLength(50)]
        public string FirstName { get; set; } //שם פרטי
        [MaxLength(50)]
        public string? LastName { get; set; } //שם משפחה
        [Required, EmailAddress]
        public string Email { get; set; } //אימייל
        [Required, Phone]
        public string PhoneNumber { get; set; } //מס טלפון
        [Required]
        public DateTime DateOfBirth { get; set; } //תאריך לידה    
        public DateTime CreatedAt { get; set; } = DateTime.Now;// תאריך יצירת המשתמש, נשמר אוטומטית
        public UserType UserType { get; set; } // סוג משתמש: לקוח/מנהל מערכת

       

        // --- נתוני רישיון נהיגה ---

        [Required, MaxLength(50)]
        public string LicenseNumber { get; set; }// מספר רישיון נהיגה   
        public string? LicenseImageUri { get; set; }// קישור לתמונת רישיון נהיגה
        public DateTime LicenseExpirationDate { get; set; }// תאריך תפוגת רישיון נהיגה
        public bool IsLicenseVerified { get; set; } = false;     // האם הרישיון אושר על ידי מנהל המערכת
        public bool IsNewDriver { get; set; } = false; // האם  נהג חדש



        // ---דרגת משתמש---
        public int CompletedOrdersCount { get; set; } = 0; // מונה הזמנות שבוצעו
        public UserRank Rank { get; set; } = UserRank.Regular;// דרגת משתמש, לפי מספר הזמנות שבוצעו



        //---סטטוס פיננסי ואמינות---
        public decimal AccountBalance { get; set; } = 0; // יתרה כספית -חובות או זיכויים
        public int DirtyReportsCount { get; set; } = 0; // מונה דיווחי לכלוך מהשוכר הבא
        public bool IsBlocked { get; set; } = false; // האם חסום לשימוש




        // --- קשרים ורשימות ---

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>(); // קישור  לרשימת ההזמנות של המשתמש הזה
        public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();  // קישור  לרשימת הקופונים של המשתמש הזה
    }
}
