using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Blind_Match_PAS.Controllers;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using Blind_Match_PAS.Repositories;
using Blind_Match_PAS.Services;
using Xunit;

namespace BlindMatch.Tests
{
    /// <summary>
    /// Integration tests for SupervisorController
    /// Tests: Match projects, confirm matches, identity reveal, dashboard
    /// </summary>
    public class SupervisorControllerTests
    {
        private static SupervisorController CreateController(
            CustomDbContext customContext,
            ApplicationDbContext applicationContext,
            string supervisorId)
        {
            var userMock = new Mock<ClaimsPrincipal>();
            userMock.Setup(u => u.FindFirstValue(ClaimTypes.NameIdentifier)).Returns(supervisorId);

            var httpContext = new DefaultHttpContext
            {
                User = userMock.Object
            };

            var matchingRepoMock = new Mock<IMatchingRequestRepository>();
            var matchingServiceMock = new Mock<IMatchingService>();

            return new SupervisorController(customContext, applicationContext, matchingRepoMock.Object, matchingServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        private static CustomDbContext CreateInMemoryCustomContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CustomDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new CustomDbContext(options);
        }

        private static ApplicationDbContext CreateInMemoryApplicationContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task MatchProject_SetsSupervisorIdCorrectly()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var customContext = CreateInMemoryCustomContext(dbName);
            var appContext = CreateInMemoryApplicationContext(dbName);

            customContext.ProjectProposals.Add(new ProjectProposal
            {
                Id = 1,
                Title = "Test Project",
                Abstract = "Test abstract with sufficient length for testing",
                TechnicalStack = "ASP.NET Core",
                ResearchAreaId = 1,
                StudentId = "student-123",
                Status = ProjectStatus.Pending
            });
            await customContext.SaveChangesAsync();

            var controller = CreateController(customContext, appContext, "supervisor-001");

            // Act
            var result = await controller.MatchProject(1);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);

            var updatedProposal = await customContext.ProjectProposals.FindAsync(1);
            Assert.NotNull(updatedProposal);
            Assert.Equal("supervisor-001", updatedProposal!.SupervisorId);
            Assert.True(updatedProposal.Status == ProjectStatus.Interested || updatedProposal.Status == ProjectStatus.UnderReview);
        }

        [Fact]
        public async Task ConfirmMatch_ChangesStatusToMatched()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var customContext = CreateInMemoryCustomContext(dbName);
            var appContext = CreateInMemoryApplicationContext(dbName);

            customContext.ProjectProposals.Add(new ProjectProposal
            {
                Id = 2,
                Title = "Match Project",
                Abstract = "Match abstract with sufficient length for testing",
                TechnicalStack = "EF Core",
                ResearchAreaId = 2,
                StudentId = "student-abc",
                SupervisorId = "supervisor-002",
                Status = ProjectStatus.Interested,
                IsIdentityRevealed = false
            });
            await customContext.SaveChangesAsync();

            var controller = CreateController(customContext, appContext, "supervisor-002");

            // Act
            var result = await controller.ConfirmMatch(2);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);

            var updatedProposal = await customContext.ProjectProposals.FindAsync(2);
            Assert.NotNull(updatedProposal);
            Assert.Equal(ProjectStatus.Matched, updatedProposal!.Status);
            Assert.True(updatedProposal.IsIdentityRevealed);
        }

        [Fact]
        public async Task ConfirmMatch_OnlyAssignedSupervisorCanConfirmMatch()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var customContext = CreateInMemoryCustomContext(dbName);
            var appContext = CreateInMemoryApplicationContext(dbName);

            customContext.ProjectProposals.Add(new ProjectProposal
            {
                Id = 3,
                Title = "Restricted Project",
                Abstract = "Restricted abstract with sufficient length for testing",
                TechnicalStack = "Blazor",
                ResearchAreaId = 3,
                StudentId = "student-xyz",
                SupervisorId = "supervisor-003",
                Status = ProjectStatus.Interested,
                IsIdentityRevealed = false
            });
            await customContext.SaveChangesAsync();

            var controller = CreateController(customContext, appContext, "other-supervisor");

            // Act
            var result = await controller.ConfirmMatch(3);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var updatedProposal = await customContext.ProjectProposals.FindAsync(3);
            Assert.NotNull(updatedProposal);
            Assert.Equal(ProjectStatus.Interested, updatedProposal!.Status);
            Assert.Equal("supervisor-003", updatedProposal.SupervisorId);
            Assert.False(updatedProposal.IsIdentityRevealed);
        }

        [Fact]
        public async Task ConfirmMatch_RevealsIdentityOnlyAfterMatching()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var customContext = CreateInMemoryCustomContext(dbName);
            var appContext = CreateInMemoryApplicationContext(dbName);

            customContext.ProjectProposals.Add(new ProjectProposal
            {
                Id = 4,
                Title = "Reveal Project",
                Abstract = "Reveal abstract with sufficient length for testing",
                TechnicalStack = "React",
                ResearchAreaId = 4,
                StudentId = "student-456",
                SupervisorId = "supervisor-004",
                Status = ProjectStatus.Interested,
                IsIdentityRevealed = false
            });
            await customContext.SaveChangesAsync();

            var controller = CreateController(customContext, appContext, "supervisor-004");

            // Act
            await controller.ConfirmMatch(4);

            // Assert
            var updatedProposal = await customContext.ProjectProposals.FindAsync(4);
            Assert.NotNull(updatedProposal);
            Assert.True(updatedProposal!.IsIdentityRevealed);
            Assert.Equal(ProjectStatus.Matched, updatedProposal.Status);
        }
    }
}
