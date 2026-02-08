using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class CouponDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string DiscountType { get; set; } // אחוזים או סכום
        public decimal DiscountAmount { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsUsed { get; set; }
    }
}
