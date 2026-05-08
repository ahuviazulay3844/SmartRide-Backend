using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Entities
{
    public class CarInspection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public bool IsCleanInside { get; set; } // האם נקי בפנים?

        [Required]
        public bool IsCleanOutside { get; set; } // האם נקי בחוץ?

        [Required]
        public bool IsAicConditionWorking { get; set; } // האם המזגן תקין?

        [Required]
        public bool AnyNewDamage { get; set; } // האם זיהית נזק חדש?
        public bool HasFlatTire { get; set; }//פנצר?

        public string? DamageDescription { get; set; } // תיאור הנזק (אם יש)

        public DateTime InspectionDate { get; set; } = DateTime.Now;

        // --- קישורים לוגיים ---

        [Required]
        public int CarId { get; set; }
        [ForeignKey("CarId")]
        public virtual Car Car { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
       
    }
}
