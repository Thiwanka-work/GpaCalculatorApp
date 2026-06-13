using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GpaCalculator.Api.Models
{
    public class GpaRecord
    {
        [Key]
        public int GpaId { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Semester { get; set; } = string.Empty;

        public double GPA { get; set; }

        public double CGPA { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
