using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{
    public class CarFeedbackDto
    {
        public int Id { get; set; }
        public int Rating { get; set; } 
        public string? UserComment { get; set; }
        public DateTime DateCreated { get; set; }
        public string? UserName { get; set; } 
        public int CarId { get; set; }
    }
}
