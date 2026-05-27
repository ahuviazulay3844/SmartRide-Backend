using System.ComponentModel.DataAnnotations;

namespace Repository.Entities
{
    public class Region
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public double? CenterLatitude { get; set; }
        public double? CenterLongitude { get; set; }
        public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
    }
}