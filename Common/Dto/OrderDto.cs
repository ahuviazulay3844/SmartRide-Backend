using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class OrderDto
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime ExpectedEndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public bool IsPaid { get; set; }
        public int UserId { get; set; }
        public int CarId { get; set; }
        public string? CarModel { get; set; } 
    }
}
