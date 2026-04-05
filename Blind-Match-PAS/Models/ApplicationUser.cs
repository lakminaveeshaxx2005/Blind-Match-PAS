using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 100 characters.")]
        public string FullName { get; set; }

        public string? UserRole { get; set; } // Student, Supervisor, or Admin

        [StringLength(500, ErrorMessage = "Expertise field cannot exceed 500 characters.")]
        public string? Expertise { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}