using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Models; // Ensure the Models namespace is referenced here

namespace Blind_Match_PAS.Data
{
    // Inherit from IdentityDbContext to include built-in ASP.NET Identity tables (Users, Roles, etc.)
    // We pass <ApplicationUser> to tell Identity to use our custom User model.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // This DbSet creates the 'ProjectProposals' table in the database
        // It allows the system to store and retrieve student project submissions.
        public DbSet<ProjectProposal> ProjectProposals { get; set; }
    }
}