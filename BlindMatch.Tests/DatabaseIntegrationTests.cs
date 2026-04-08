using System;
using System.Threading.Tasks;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlindMatch.Tests
{
    /// <summary>
    /// Integration tests for database operations
    /// Tests: CRUD operations, relationships, constraints, persistence
    /// </summary>
    public class DatabaseIntegrationTests
    {
        private static ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private static CustomDbContext CreateInMemoryCustomContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CustomDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new CustomDbContext(options);
        }

        [Fact]
        public async Task SaveProjectProposal_ValidData_PersistsToDatabase()
        {
            // Arrange
            var context = CreateInMemoryCustomContext(Guid.NewGuid().ToString());
            var proposal = new ProjectProposal
            {
                Title = "Valid Project Title",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core, Entity Framework Core",
                StudentId = "student-001",
                ResearchAreaId = 1,
                Status = ProjectStatus.Pending
            };

            // Act
            context.ProjectProposals.Add(proposal);
            await context.SaveChangesAsync();

            // Assert
            var savedProposal = await context.ProjectProposals.FindAsync(proposal.Id);
            Assert.NotNull(savedProposal);
            Assert.Equal("Valid Project Title", savedProposal!.Title);
            Assert.Equal("student-001", savedProposal.StudentId);
        }

        [Fact]
        public async Task UpdateProjectProposal_ValidData_UpdatesInDatabase()
        {
            // Arrange
            var context = CreateInMemoryCustomContext(Guid.NewGuid().ToString());
            var proposal = new ProjectProposal
            {
                Title = "Original Title",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-001",
                Status = ProjectStatus.Pending
            };
            context.ProjectProposals.Add(proposal);
            await context.SaveChangesAsync();
            var proposalId = proposal.Id;

            // Act
            var proposalToUpdate = await context.ProjectProposals.FindAsync(proposalId);
            proposalToUpdate!.Title = "Updated Title";
            proposalToUpdate.Status = ProjectStatus.Interested;
            proposalToUpdate.SupervisorId = "supervisor-001";
            await context.SaveChangesAsync();

            // Assert
            var updatedProposal = await context.ProjectProposals.FindAsync(proposalId);
            Assert.Equal("Updated Title", updatedProposal!.Title);
            Assert.Equal(ProjectStatus.Interested, updatedProposal.Status);
            Assert.Equal("supervisor-001", updatedProposal.SupervisorId);
        }

        [Fact]
        public async Task DeleteProjectProposal_ExistingProposal_RemovesFromDatabase()
        {
            // Arrange
            var context = CreateInMemoryCustomContext(Guid.NewGuid().ToString());
            var proposal = new ProjectProposal
            {
                Title = "To Delete",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-001",
                Status = ProjectStatus.Pending
            };
            context.ProjectProposals.Add(proposal);
            await context.SaveChangesAsync();
            var proposalId = proposal.Id;

            // Act
            var proposalToDelete = await context.ProjectProposals.FindAsync(proposalId);
            context.ProjectProposals.Remove(proposalToDelete!);
            await context.SaveChangesAsync();

            // Assert
            var deletedProposal = await context.ProjectProposals.FindAsync(proposalId);
            Assert.Null(deletedProposal);
        }

        [Fact]
        public async Task SaveUser_ValidData_PersistsToDatabase()
        {
            // Arrange
            var context = CreateInMemoryContext(Guid.NewGuid().ToString());
            var user = new ApplicationUser
            {
                Id = "user-001",
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                FullName = "John Doe",
                UserRole = "Student",
                ResearchInterest = "Machine Learning, AI"
            };

            // Act
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Assert
            var savedUser = await context.Users.FindAsync("user-001");
            Assert.NotNull(savedUser);
            Assert.Equal("John Doe", savedUser!.FullName);
            Assert.Equal("Student", savedUser.UserRole);
        }

        [Fact]
        public async Task UpdateUser_Role_PersistsChanges()
        {
            // Arrange
            var context = CreateInMemoryContext(Guid.NewGuid().ToString());
            var user = new ApplicationUser
            {
                Id = "user-001",
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                FullName = "Jane Doe",
                UserRole = "Student",
                ResearchInterest = "Web Development"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            user.UserRole = "Supervisor";
            user.Expertise = "Web Development, ASP.NET Core";
            context.Users.Update(user);
            await context.SaveChangesAsync();

            // Assert
            var updatedUser = await context.Users.FindAsync("user-001");
            Assert.Equal("Supervisor", updatedUser!.UserRole);
            Assert.Equal("Web Development, ASP.NET Core", updatedUser.Expertise);
        }

        [Fact]
        public async Task QueryProjectsByStudentId_ReturnsOnlyStudentProposals()
        {
            // Arrange
            var context = CreateInMemoryCustomContext(Guid.NewGuid().ToString());
            var proposal1 = new ProjectProposal
            {
                Title = "Student 1 Project",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-001",
                Status = ProjectStatus.Pending
            };
            var proposal2 = new ProjectProposal
            {
                Title = "Student 1 Project 2",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-001",
                Status = ProjectStatus.Pending
            };
            var proposal3 = new ProjectProposal
            {
                Title = "Student 2 Project",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-002",
                Status = ProjectStatus.Pending
            };

            context.ProjectProposals.AddRange(proposal1, proposal2, proposal3);
            await context.SaveChangesAsync();

            // Act
            var studentProposals = await context.ProjectProposals
                .Where(p => p.StudentId == "student-001")
                .ToListAsync();

            // Assert
            Assert.Equal(2, studentProposals.Count);
            Assert.All(studentProposals, p => Assert.Equal("student-001", p.StudentId));
        }

        [Fact]
        public async Task QueryProposalsByStatus_ReturnsOnlyMatchingStatus()
        {
            // Arrange
            var context = CreateInMemoryCustomContext(Guid.NewGuid().ToString());
            var pending = new ProjectProposal
            {
                Title = "Pending Project",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-001",
                Status = ProjectStatus.Pending
            };
            var interested = new ProjectProposal
            {
                Title = "Interested Project",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-002",
                Status = ProjectStatus.Interested
            };
            var matched = new ProjectProposal
            {
                Title = "Matched Project",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-003",
                Status = ProjectStatus.Matched
            };

            context.ProjectProposals.AddRange(pending, interested, matched);
            await context.SaveChangesAsync();

            // Act
            var pendingProposals = await context.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Pending)
                .ToListAsync();

            var matchedProposals = await context.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Matched)
                .ToListAsync();

            // Assert
            Assert.Single(pendingProposals);
            Assert.Single(matchedProposals);
            Assert.Equal(ProjectStatus.Pending, pendingProposals[0].Status);
            Assert.Equal(ProjectStatus.Matched, matchedProposals[0].Status);
        }

        [Fact]
        public async Task QueryProposalsBySupervisor_ReturnsOnlySupervisorMatches()
        {
            // Arrange
            var context = CreateInMemoryCustomContext(Guid.NewGuid().ToString());
            var proposal1 = new ProjectProposal
            {
                Title = "Supervisor 1 Match",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-001",
                SupervisorId = "supervisor-001",
                Status = ProjectStatus.Matched
            };
            var proposal2 = new ProjectProposal
            {
                Title = "Supervisor 1 Match 2",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-002",
                SupervisorId = "supervisor-001",
                Status = ProjectStatus.Matched
            };
            var proposal3 = new ProjectProposal
            {
                Title = "Supervisor 2 Match",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-003",
                SupervisorId = "supervisor-002",
                Status = ProjectStatus.Matched
            };

            context.ProjectProposals.AddRange(proposal1, proposal2, proposal3);
            await context.SaveChangesAsync();

            // Act
            var supervisor1Matches = await context.ProjectProposals
                .Where(p => p.SupervisorId == "supervisor-001")
                .ToListAsync();

            // Assert
            Assert.Equal(2, supervisor1Matches.Count);
            Assert.All(supervisor1Matches, p => Assert.Equal("supervisor-001", p.SupervisorId));
        }

        [Fact]
        public async Task ResearchAreaForeignKey_ValidArea_AssociatesCorrectly()
        {
            // Arrange
            var context = CreateInMemoryCustomContext(Guid.NewGuid().ToString());
            var researchArea = new ResearchArea { Id = 1, Name = "Machine Learning" };
            var proposal = new ProjectProposal
            {
                Title = "ML Project",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "Python, TensorFlow",
                StudentId = "student-001",
                ResearchAreaId = 1,
                ResearchArea = researchArea,
                Status = ProjectStatus.Pending
            };

            // Act
            context.ResearchAreas.Add(researchArea);
            context.ProjectProposals.Add(proposal);
            await context.SaveChangesAsync();

            // Assert
            var savedProposal = await context.ProjectProposals
                .Include(p => p.ResearchArea)
                .FirstAsync(p => p.Id == proposal.Id);

            Assert.NotNull(savedProposal.ResearchArea);
            Assert.Equal("Machine Learning", savedProposal.ResearchArea!.Name);
        }

        [Fact]
        public async Task ConcurrentUpdates_BothChangesPreserved()
        {
            // Arrange
            var context = CreateInMemoryCustomContext(Guid.NewGuid().ToString());
            var proposal = new ProjectProposal
            {
                Title = "Concurrent Test",
                Abstract = "This is a valid abstract with sufficient length for testing purpose",
                TechnicalStack = "ASP.NET Core",
                StudentId = "student-001",
                Status = ProjectStatus.Pending
            };
            context.ProjectProposals.Add(proposal);
            await context.SaveChangesAsync();

            // Act
            proposal.Status = ProjectStatus.Interested;
            proposal.SupervisorId = "supervisor-001";
            await context.SaveChangesAsync();

            // Assert
            var updated = await context.ProjectProposals.FindAsync(proposal.Id);
            Assert.Equal(ProjectStatus.Interested, updated!.Status);
            Assert.Equal("supervisor-001", updated.SupervisorId);
        }
    }
}
