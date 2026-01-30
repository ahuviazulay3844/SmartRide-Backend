using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    // סוגי הנחות
    public enum DiscountType { Amount = 0, Percentage = 1 }// סכום קבוע/אחוזים

    ///טבלת קופונים
    public class Coupon
    {
        [Key]
        public int Id { get; set; } //מזהה קופון
        [Required, MaxLength(50)]
        public string Code { get; set; } //קוד קופון
        public DiscountType DiscountType { get; set; } // סוג הנחה
        public decimal DiscountAmount { get; set; } // סכום הנחה
        public DateTime? ExpirationDate { get; set; } //תאריך תפוגה של הקופון
        public bool IsUsed { get; set; } = false; // האם הקופון שומש
        public decimal? MinimumOrderAmount { get; set; } // אופציונלי- מינימום הזמנה כדי להפעיל את הקופון



        //----- קישורים לטבלאות אחרות -----


        // --- קישור למשתמש  ---
        public int? UserId { get; set; }// אופציונלי- מזהה משתמש אם הקופון מיועד למשתמש ספציפי
        [ForeignKey("UserId")]  
        public virtual User? User { get; set; }// קישור למשתמש אם הקופון מיועד למשתמש ספציפי





        // --- קשרים ורשימות ---


        // --- קישור להזמנות שהשתמשו בקופון ---
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
