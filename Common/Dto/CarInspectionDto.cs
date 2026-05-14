using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dto
{

        public class CarInspectionDto
        {
            public int OrderId { get; set; } 
            public bool IsCleanInside { get; set; }
            public bool IsCleanOutside { get; set; }
            public bool IsAicConditionWorking { get; set; }
            public bool AnyNewDamage { get; set; }
            public bool HasFlatTire { get; set; }
            public string? DamageDescription { get; set; }
        }
    
}
