using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    public enum ProjectStatus { Pending, UnderReview, Matched, Withdrawn }

    public class ProjectProposal
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
        [RegularExpression(@"^[A-Z][a-zA-Z0-9\s\-_.,&()]+$", ErrorMessage = "Title must start with an uppercase letter and contain only valid characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Abstract is required.")]
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "Abstract must be between 20 and 1000 characters.")]
        public string Abstract { get; set; }

        [Required(ErrorMessage = "Technical Stack is required.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Technical Stack must be between 5 and 500 characters.")]
        public string TechnicalStack { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;

        [Required(ErrorMessage = "Research Area is required.")]
        public int ResearchAreaId { get; set; }
        public ResearchArea? ResearchArea { get; set; }

        [Required(ErrorMessage = "Student ID is required.")]
        public string StudentId { get; set; }
        public string? SupervisorId { get; set; }

        public bool IsIdentityRevealed { get; set; } = false;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? MatchedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; } = DateTime.UtcNow;
    }
}