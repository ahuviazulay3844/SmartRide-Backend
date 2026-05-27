using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repository.Entities
{
    public class CarFeedback
    {
        [Key]
        public int Id { get; set; } 
        [Range(1, 5)]
        public int Rating { get; set; } 
        public string? UserComment { get; set; }
        public bool ReportedIssue { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;

        [Required]
        public int CarId { get; set; }

        [ForeignKey("CarId")]
        public virtual Car? Car { get; set; }

        public int UserId { get; set; }
        public virtual User? User { get; set; }

        public int OrderId { get; set; } 
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}