using System.Collections.Generic;

namespace Blind_Match_PAS.Models
{
    /// <summary>
    /// ViewModel for Supervisor Dashboard displaying anonymous proposals and matched projects.
    /// </summary>
    public class SupervisorDashboardViewModel
    {
        /// <summary>
        /// List of anonymous pending proposals (blind review - no student identification).
        /// </summary>
        public List<AnonymousProposalDto> PendingProposals { get; set; } = new List<AnonymousProposalDto>();

        /// <summary>
        /// List of proposals the supervisor has expressed interest in but has not confirmed yet.
        /// </summary>
        public List<InterestedProposalDto> InterestedProposals { get; set; } = new List<InterestedProposalDto>();

        /// <summary>
        /// List of matching requests from students (anonymous until accepted).
        /// </summary>
        public List<MatchingRequestDto> MatchingRequests { get; set; } = new List<MatchingRequestDto>();

        /// <summary>
        /// List of matched proposals where identities are revealed.
        /// </summary>
        public List<RevealedProposalDto> MatchedProposals { get; set; } = new List<RevealedProposalDto>();

        /// <summary>
        /// Total number of pending proposals available for review.
        /// </summary>
        public int TotalPendingProposals { get; set; }

        /// <summary>
        /// Total number of pending matching requests from students.
        /// </summary>
        public int TotalPendingRequests { get; set; }

        /// <summary>
        /// Total number of express-interest proposals pending confirmation.
        /// </summary>
        public int TotalInterestedProposals { get; set; }

        /// <summary>
        /// Total number of accepted matching requests.
        /// </summary>
        public int TotalAcceptedRequests { get; set; }

        /// <summary>
        /// Total number of rejected matching requests.
        /// </summary>
        public int TotalRejectedRequests { get; set; }

        /// <summary>
        /// Total number of matching requests (pending + accepted + rejected).
        /// </summary>
        public int TotalMatchingRequests { get; set; }

        /// <summary>
        /// Total number of matches made by this supervisor (same as matched proposals).
        /// </summary>
        public int TotalMatches { get; set; }

        /// <summary>
        /// Maximum number of students a supervisor can accept (configurable).
        /// </summary>
        public int MaxStudentLimit { get; set; } = 5;

        /// <summary>
        /// Error message for supervisor limit validation.
        /// </summary>
        public string? SupervisorLimitError { get; set; }
    }

    /// <summary>
    /// DTO for anonymous proposal display (blind review).
    /// Does NOT include student name or ID.
    /// </summary>
    public class AnonymousProposalDto
    {
        public int ProposalId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Abstract { get; set; } = string.Empty;

        public string TechnicalStack { get; set; } = string.Empty;

        public string? ResearchArea { get; set; }

        public DateTime CreatedAt { get; set; }

        public ProjectStatus Status { get; set; }
    }

    /// <summary>
    /// DTO for revealed proposal display (after matching).
    /// Includes student name for communication purposes.
    /// </summary>
    public class RevealedProposalDto
    {
        public int ProposalId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Abstract { get; set; } = string.Empty;

        public string TechnicalStack { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string StudentEmail { get; set; } = string.Empty;

        public string? ResearchArea { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? MatchedAt { get; set; }

        public ProjectStatus Status { get; set; }
    }

    /// <summary>
    /// DTO for matching requests from students (anonymous until accepted).
    /// </summary>
    public class InterestedProposalDto
    {
        public int ProposalId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Abstract { get; set; } = string.Empty;

        public string TechnicalStack { get; set; } = string.Empty;

        public string? ResearchArea { get; set; }

        public DateTime CreatedAt { get; set; }

        public ProjectStatus Status { get; set; }
    }

    public class MatchingRequestDto
    {
        public int RequestId { get; set; }

        public int ProposalId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Abstract { get; set; } = string.Empty;

        public string TechnicalStack { get; set; } = string.Empty;

        public string? ResearchArea { get; set; }

        public DateTime RequestedAt { get; set; }

        public int MatchScore { get; set; }

        public MatchingRequestStatus Status { get; set; }
    }
}
