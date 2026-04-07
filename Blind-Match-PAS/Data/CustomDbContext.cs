using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Models;

namespace Blind_Match_PAS.Data
{
    public class CustomDbContext : DbContext
    {
        public CustomDbContext(DbContextOptions<CustomDbContext> options)
            : base(options)
        {
        }

        // Register your custom tables here:
        public DbSet<ProjectProposal> ProjectProposals { get; set; }
        public DbSet<ResearchArea> ResearchAreas { get; set; }
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure your custom entities here if needed
        }
    }
}