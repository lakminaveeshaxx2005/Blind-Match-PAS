using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    public class SupervisorProfileViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Expertise cannot exceed 500 characters.")]
        [Display(Name = "Expertise")]
        public string? Expertise { get; set; }

        [StringLength(500, ErrorMessage = "Research interests cannot exceed 500 characters.")]
        [Display(Name = "Research Interests")]
        public string? ResearchInterest { get; set; }
    }
}
