using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GpaCalculator.Api.Models
{
    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(150)]
        public string SubjectName { get; set; } = string.Empty;

        public int Credits { get; set; }

        [Required]
        [MaxLength(2)]
        public string Grade { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Semester { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
