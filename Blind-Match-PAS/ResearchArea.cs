using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    /// <summary>
    /// Represents a research area or field of study.
    /// Used to categorize project proposals and match students with supervisors.
    /// </summary>
    public class ResearchArea
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Research area name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Research area name must be between 3 and 100 characters.")]
        [RegularExpression(@"^[A-Z][a-zA-Z0-9\s&\-.,()]+$", ErrorMessage = "Research area name must start with an uppercase letter and contain only valid characters.")]
        [Display(Name = "Research Area Name")]
        public required string Name { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property for project proposals in this research area.
        /// </summary>
        public ICollection<ProjectProposal>? ProjectProposals { get; set; }
    }
}