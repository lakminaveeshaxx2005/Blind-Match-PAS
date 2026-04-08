using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blind_Match_PAS.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProposalId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public string SupervisorId { get; set; } = string.Empty;

        public DateTime MatchedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProposalId")]
        public ProjectProposal? Proposal { get; set; }

        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }

        [ForeignKey("SupervisorId")]
        public ApplicationUser? Supervisor { get; set; }
    }
}
