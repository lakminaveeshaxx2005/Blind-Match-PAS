using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Blind_Match_PAS.Models; // This allows the context to see your models

namespace Blind_Match_PAS.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Register your tables here:
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ProjectProposal> ProjectProposals { get; set; }
        public DbSet<ResearchArea> ResearchAreas { get; set; }
    }
}