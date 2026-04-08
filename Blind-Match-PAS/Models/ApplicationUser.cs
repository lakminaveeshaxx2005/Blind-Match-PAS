using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    /// <summary>
    /// Extended ASP.NET Core Identity user with role-based access control.
    /// Supports three roles: Student, Supervisor, Admin.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 100 characters.")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Full name can only contain letters, spaces, hyphens, and apostrophes.")]
        [Display(Name = "Full Name")]
        public required string FullName { get; set; }

        [RegularExpression(@"^(Student|Supervisor|Admin)$", ErrorMessage = "User role must be Student, Supervisor, or Admin.")]
        [Display(Name = "User Role")]
        public string? UserRole { get; set; } // Student, Supervisor, or Admin

        [StringLength(500, ErrorMessage = "Expertise field cannot exceed 500 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9\s,.\-&()]*$", ErrorMessage = "Expertise contains invalid characters.")]
        [Display(Name = "Expertise")]
        public string? Expertise { get; set; }

        [StringLength(500, ErrorMessage = "Research Interest field cannot exceed 500 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9\s,.\-&()]*$", ErrorMessage = "Research Interest contains invalid characters.")]
        [Display(Name = "Research Interest")]
        public string? ResearchInterest { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the user's role for authorization purposes.
        /// </summary>
        public bool IsStudent => UserRole == "Student";

        /// <summary>
        /// Gets whether the user is a supervisor.
        /// </summary>
        public bool IsSupervisor => UserRole == "Supervisor";

        /// <summary>
        /// Gets whether the user is an administrator.
        /// </summary>
        public bool IsAdmin => UserRole == "Admin";
    }
}