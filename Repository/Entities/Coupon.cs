using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public enum DiscountType { Amount = 0, Percentage = 1 }

    public class Coupon
    {
        [Key]
        public int Id { get; set; } 
        [Required, MaxLength(50)]
        public string Code { get; set; } 
        public DiscountType DiscountType { get; set; } 
        public decimal DiscountAmount { get; set; } 
        public DateTime? ExpirationDate { get; set; } 
        public bool IsUsed { get; set; } = false; 
        public decimal? MinimumOrderAmount { get; set; } 
        public int? UserId { get; set; }
        [ForeignKey("UserId")]  
        public virtual User? User { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
