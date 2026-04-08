using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Blind_Match_PAS.Models; // This allows the context to see your models

namespace Blind_Match_PAS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Only register the ApplicationUser here for Identity purposes
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        // Add MatchingRequest to ApplicationDbContext
        public DbSet<MatchingRequest> MatchingRequests { get; set; }

        // Custom tables are now handled by CustomDbContext
        // public DbSet<ProjectProposal> ProjectProposals { get; set; }
        // public DbSet<ResearchArea> ResearchAreas { get; set; }
    }
}