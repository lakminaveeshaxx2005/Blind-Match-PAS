using Microsoft.AspNetCore.Identity;

namespace Blind_Match_PAS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? UserRole { get; set; } // Student, Supervisor, or Admin
        public string? Expertise { get; set; }
    }
}