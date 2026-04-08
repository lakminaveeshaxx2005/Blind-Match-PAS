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
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ProjectProposal> ProjectProposals { get; set; }
        public DbSet<ResearchArea> ResearchAreas { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Match> Matches { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser as existing table (managed by ApplicationDbContext)
            builder.Entity<ApplicationUser>().ToTable("AspNetUsers", t => t.ExcludeFromMigrations());

            // Configure MatchingRequest foreign keys to avoid cascade cycles
            builder.Entity<MatchingRequest>()
                .HasOne(m => m.Student)
                .WithMany()
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<MatchingRequest>()
                .HasOne(m => m.Supervisor)
                .WithMany()
                .HasForeignKey(m => m.SupervisorId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Match entity relationships
            builder.Entity<Match>()
                .HasOne(m => m.Proposal)
                .WithMany()
                .HasForeignKey(m => m.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Match>()
                .HasOne(m => m.Student)
                .WithMany()
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Match>()
                .HasOne(m => m.Supervisor)
                .WithMany()
                .HasForeignKey(m => m.SupervisorId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure your custom entities here if needed
        }
    }
}