using System;
using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    public class SupervisorProjectProposalViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Abstract { get; set; }
        public required string TechnicalStack { get; set; }
        public string? ResearchAreaName { get; set; }
        public ProjectStatus Status { get; set; }
        public string? StudentId { get; set; }
        public string? StudentName { get; set; }
        public bool IsIdentityRevealed { get; set; }
        public string? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public string? SupervisorEmail { get; set; }
        public DateTime? MatchedAt { get; set; }
    }

    public class StudentProjectProposalViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Abstract { get; set; }
        public required string TechnicalStack { get; set; }
        public string? ResearchAreaName { get; set; }
        public ProjectStatus Status { get; set; }
        public string? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public string? SupervisorEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? MatchedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
    }

    public class SupervisorPreferencesViewModel
    {
        [StringLength(500, ErrorMessage = "Expertise field cannot exceed 500 characters.")]
        [Display(Name = "Preferred Research Areas")]
        public string? Expertise { get; set; }
    }
}
