using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    public class ResearchArea
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Research area name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Research area name must be between 3 and 100 characters.")]
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}