using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    /// <summary>
    /// ViewModel for Admin Dashboard displaying comprehensive system overview.
    /// </summary>
    public class AdminDashboardViewModel
    {
        /// <summary>
        /// System statistics overview.
        /// </summary>
        public SystemStatistics Statistics { get; set; } = new SystemStatistics();

        /// <summary>
        /// Recent matches for quick overview.
        /// </summary>
        public List<RecentMatchDto> RecentMatches { get; set; } = new List<RecentMatchDto>();

        /// <summary>
        /// Pending proposals requiring attention.
        /// </summary>
        public List<PendingProposalDto> PendingProposals { get; set; } = new List<PendingProposalDto>();

        /// <summary>
        /// All research areas with usage statistics.
        /// </summary>
        public List<ResearchAreaDto> ResearchAreas { get; set; } = new List<ResearchAreaDto>();

        /// <summary>
        /// User distribution by roles.
        /// </summary>
        public UserRoleDistribution UserDistribution { get; set; } = new UserRoleDistribution();
    }

    /// <summary>
    /// System-wide statistics for the admin dashboard.
    /// </summary>
    public class SystemStatistics
    {
        public int TotalStudents { get; set; }
        public int TotalSupervisors { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalUsers { get; set; }
        public int TotalResearchAreas { get; set; }
        public int TotalProposals { get; set; }
        public int TotalMatches { get; set; }
        public int PendingRequests { get; set; }
        public int PendingProposals { get; set; }
        public int MatchedProposals { get; set; }
        public int ApprovedProposals { get; set; }
        public int RejectedProposals { get; set; }
        public double MatchRate { get; set; } // Percentage of proposals that are matched
    }

    /// <summary>
    /// DTO for recent matches display.
    /// </summary>
    public class RecentMatchDto
    {
        public int ProposalId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public DateTime? MatchedAt { get; set; }
    }

    /// <summary>
    /// DTO for pending proposals overview.
    /// </summary>
    public class PendingProposalDto
    {
        public int ProposalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int DaysPending { get; set; }
    }

    /// <summary>
    /// DTO for research areas with usage statistics.
    /// </summary>
    public class ResearchAreaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalProposals { get; set; }
        public int PendingProposals { get; set; }
        public int MatchedProposals { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// User distribution by roles.
    /// </summary>
    public class UserRoleDistribution
    {
        public int Students { get; set; }
        public int Supervisors { get; set; }
        public int Admins { get; set; }
        public int Unassigned { get; set; }
    }

    /// <summary>
    /// DTO for users with their roles.
    /// </summary>
    public class UserWithRole
    {
        public ApplicationUser User { get; set; } = null!;
        public List<string> Roles { get; set; } = new List<string>();
    }

    /// <summary>
    /// Research area with usage statistics for the research areas management view.
    /// </summary>
    public class ResearchAreaWithStats
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TotalProposals { get; set; }
        public int PendingProposals { get; set; }
        public int MatchedProposals { get; set; }
    }

    public class CreateUserModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Student";
    }

    public class EditUserModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Student";
    }

    public class RequestMonitorDto
    {
        public int Id { get; set; }
        public string ProposalTitle { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}