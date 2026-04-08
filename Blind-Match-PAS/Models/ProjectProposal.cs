using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blind_Match_PAS.Models
{
    public enum ProjectStatus
    {
        Pending,
        Interested,
        UnderReview,
        Approved,
        Rejected,
        Matched
    }

    public class ProjectProposal
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Project title is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
        [Display(Name = "Project Title")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Project abstract is required.")]
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "Abstract must be between 20 and 1000 characters.")]
        [Display(Name = "Project Abstract")]
        public required string Abstract { get; set; }

        [Required(ErrorMessage = "Technical stack is required.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Technical stack must be between 5 and 500 characters.")]
        [Display(Name = "Technical Stack")]
        public required string TechnicalStack { get; set; }

        [Required]
        public required string StudentId { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;

        public int? ResearchAreaId { get; set; }

        [ForeignKey("ResearchAreaId")]
        public ResearchArea? ResearchArea { get; set; }

        public string? SupervisorId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedAt { get; set; }

        public DateTime? MatchedAt { get; set; }

        public bool IsIdentityRevealed { get; set; } = false;

        public bool CanEdit => Status == ProjectStatus.Pending;

        public bool IsValidStateTransition(ProjectStatus newStatus)
        {
            return (Status == ProjectStatus.Pending && (newStatus == ProjectStatus.Interested || newStatus == ProjectStatus.Approved || newStatus == ProjectStatus.Rejected)) ||
                   (Status == ProjectStatus.Interested && (newStatus == ProjectStatus.UnderReview || newStatus == ProjectStatus.Rejected)) ||
                   (Status == ProjectStatus.UnderReview && (newStatus == ProjectStatus.Approved || newStatus == ProjectStatus.Rejected)) ||
                   (Status == ProjectStatus.Approved && newStatus == ProjectStatus.Matched);
        }
    }
}