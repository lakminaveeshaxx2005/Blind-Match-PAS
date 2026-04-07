using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blind_Match_PAS.Models
{
    public enum ProjectStatus { Pending, Interested, UnderReview, Matched, Withdrawn }

    /// <summary>
    /// Project proposal submitted by a student.
    /// 
    /// Workflow:
    /// 1. Student creates proposal (Pending)
    /// 2. Supervisor expresses interest (Interested)
    /// 3. Student confirms (Matched) or rejects (Pending)
    /// 4. After match, both identities are revealed
    /// </summary>
    public class ProjectProposal
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
        [RegularExpression(@"^[A-Z][a-zA-Z0-9\s\-_.,&()]+$", ErrorMessage = "Title must start with an uppercase letter and contain only letters, numbers, spaces, and basic punctuation.")]
        [Display(Name = "Project Title")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Abstract is required.")]
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "Abstract must be between 20 and 1000 characters.")]
        [Display(Name = "Project Abstract")]
        public required string Abstract { get; set; }

        [Required(ErrorMessage = "Technical Stack is required.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Technical Stack must be between 5 and 500 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9\s,.\-_/+()#]+$", ErrorMessage = "Technical Stack contains invalid characters.")]
        [Display(Name = "Technical Stack")]
        public required string TechnicalStack { get; set; }

        [Required(ErrorMessage = "Project status is required.")]
        [EnumDataType(typeof(ProjectStatus), ErrorMessage = "Invalid project status.")]
        [Display(Name = "Status")]
        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;

        [Required(ErrorMessage = "Research Area is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "A valid research area must be selected.")]
        [Display(Name = "Research Area")]
        public int ResearchAreaId { get; set; }

        [ForeignKey("ResearchAreaId")]
        public ResearchArea? ResearchArea { get; set; }

        [Required(ErrorMessage = "Student ID is required.")]
        [StringLength(450, ErrorMessage = "Student ID cannot exceed 450 characters.")]
        public required string StudentId { get; set; }

        [StringLength(450, ErrorMessage = "Supervisor ID cannot exceed 450 characters.")]
        public string? SupervisorId { get; set; }

        [Display(Name = "Identity Revealed")]
        public bool IsIdentityRevealed { get; set; } = false;

        // Audit Fields
        [Display(Name = "Submitted Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Matched Date")]
        public DateTime? MatchedAt { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime? LastModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Determines if this proposal can be edited by the student.
        /// Only Pending proposals can be edited.
        /// </summary>
        [NotMapped]
        public bool CanEdit => Status == ProjectStatus.Pending;

        /// <summary>
        /// Validates if a state transition is allowed.
        /// Enforces blind matching workflow:
        /// Pending → UnderReview → Matched
        ///        → Pending (rejection)
        /// </summary>
        public bool IsValidStateTransition(ProjectStatus newStatus)
        {
            return (Status, newStatus) switch
            {
                (ProjectStatus.Pending, ProjectStatus.Interested) => true,    // Supervisor expresses interest
                (ProjectStatus.Interested, ProjectStatus.Matched) => true,    // Student confirms interest
                (ProjectStatus.Interested, ProjectStatus.Pending) => true,    // Student rejects interest
                (ProjectStatus.Interested, ProjectStatus.Withdrawn) => true,  // Student withdraws after interest
                (ProjectStatus.Pending, ProjectStatus.Withdrawn) => true,     // Student withdraws before interest
                _ => false
            };
        }
    }
}