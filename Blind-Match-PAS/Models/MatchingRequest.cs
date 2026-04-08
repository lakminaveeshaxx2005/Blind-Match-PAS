using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blind_Match_PAS.Models
{
    public enum MatchingRequestStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class MatchingRequest
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Student ID is required.")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "Supervisor ID is required.")]
        public string SupervisorId { get; set; }

        [Required(ErrorMessage = "Proposal ID is required.")]
        public int ProposalId { get; set; }

        public MatchingRequestStatus Status { get; set; } = MatchingRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ApplicationUser Student { get; set; }
        public ApplicationUser Supervisor { get; set; }
    }
}