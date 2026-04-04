using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    public enum ProjectStatus { Pending, UnderReview, Matched, Withdrawn }

    public class ProjectProposal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Abstract { get; set; }

        [Required]
        public string TechnicalStack { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;

        public int ResearchAreaId { get; set; }
        public ResearchArea? ResearchArea { get; set; }

        public string StudentId { get; set; }
        public string? SupervisorId { get; set; }

        public bool IsIdentityRevealed { get; set; } = false;
    }
}