using System.ComponentModel.DataAnnotations;

namespace Repository.Entities
{
    //טבלת אזורים
    public class Region
    {

        //מזהה אזור
        [Key]
        public int Id { get; set; }

        //שם אזור
        [Required]
        public string Name { get; set; }

        //  נקודת המרכז של האזור כדי שהמפה תדע לאן "לקפוץ" כשבוחרים אזור
        public double? CenterLatitude { get; set; }
        public double? CenterLongitude { get; set; }
    }
}