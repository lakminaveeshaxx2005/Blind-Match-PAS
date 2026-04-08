using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Blind_Match_PAS.Models;
using Xunit;

namespace BlindMatch.Tests
{
    /// <summary>
    /// Unit tests for ProjectProposal model validation
    /// Tests: Data annotations, status transitions, CanEdit property, business rules
    /// </summary>
    public class ProjectProposalValidationTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void ProjectProposal_ValidProposal_PassesValidation()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#, EF Core",
                StudentId = "student-001",
                ResearchAreaId = 1
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void ProjectProposal_EmptyTitle_FailsValidation()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                ResearchAreaId = 1
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProjectProposal.Title)));
        }

        [Fact]
        public void ProjectProposal_TitleTooShort_FailsValidation()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "ABC",  // Minimum is 5 characters
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                ResearchAreaId = 1
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProjectProposal.Title))
                && r.ErrorMessage!.Contains("5"));
        }

        [Fact]
        public void ProjectProposal_TitleTooLong_FailsValidation()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = new string('a', 201),  // Maximum is 200 characters
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                ResearchAreaId = 1
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProjectProposal.Title))
                && r.ErrorMessage!.Contains("200"));
        }

        [Fact]
        public void ProjectProposal_AbstractTooShort_FailsValidation()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "Short",  // Minimum is 20 characters
                TechnicalStack = "ASP.NET",
                StudentId = "student-001",
                ResearchAreaId = 1
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProjectProposal.Abstract))
                && r.ErrorMessage!.Contains("20"));
        }

        [Fact]
        public void ProjectProposal_AbstractTooLong_FailsValidation()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = new string('a', 1001),  // Maximum is 1000 characters
                TechnicalStack = "ASP.NET",
                StudentId = "student-001",
                ResearchAreaId = 1
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProjectProposal.Abstract))
                && r.ErrorMessage!.Contains("1000"));
        }

        [Fact]
        public void ProjectProposal_TechnicalStackTooShort_FailsValidation()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "C#",  // Minimum is 5 characters
                StudentId = "student-001",
                ResearchAreaId = 1
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProjectProposal.TechnicalStack))
                && r.ErrorMessage!.Contains("5"));
        }

        [Fact]
        public void ProjectProposal_TechnicalStackTooLong_FailsValidation()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = new string('a', 501),  // Maximum is 500 characters
                StudentId = "student-001",
                ResearchAreaId = 1
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProjectProposal.TechnicalStack))
                && r.ErrorMessage!.Contains("500"));
        }

        [Fact]
        public void ProjectProposal_MissingStudentId_FailsValidation()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "",  // Required
                ResearchAreaId = 1
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(ProjectProposal.StudentId)));
        }

        [Fact]
        public void ProjectProposal_DefaultStatus_IsPending()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001"
            };

            // Act & Assert
            Assert.Equal(ProjectStatus.Pending, proposal.Status);
        }

        [Fact]
        public void ProjectProposal_StatusCanChangeFromPendingToInterested()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                Status = ProjectStatus.Pending
            };

            // Act
            proposal.Status = ProjectStatus.Interested;

            // Assert
            Assert.Equal(ProjectStatus.Interested, proposal.Status);
        }

        [Fact]
        public void ProjectProposal_StatusCanChangeToMatched()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                Status = ProjectStatus.Interested
            };

            // Act
            proposal.Status = ProjectStatus.Matched;

            // Assert
            Assert.Equal(ProjectStatus.Matched, proposal.Status);
        }

        [Fact]
        public void ProjectProposal_CanEdit_ReturnsTrueWhenPending()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                Status = ProjectStatus.Pending
            };

            // Act & Assert
            Assert.True(proposal.CanEdit);
        }

        [Fact]
        public void ProjectProposal_CanEdit_ReturnsFalseWhenInterested()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                Status = ProjectStatus.Interested
            };

            // Act & Assert
            Assert.False(proposal.CanEdit);
        }

        [Fact]
        public void ProjectProposal_CanEdit_ReturnsFalseWhenMatched()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                Status = ProjectStatus.Matched
            };

            // Act & Assert
            Assert.False(proposal.CanEdit);
        }

        [Fact]
        public void ProjectProposal_CreatedAtDefaultsToUtcNow()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001"
            };

            var afterCreation = DateTime.UtcNow;

            // Act & Assert
            Assert.True(proposal.CreatedAt >= beforeCreation && proposal.CreatedAt <= afterCreation,
                "CreatedAt should be set to current UTC time");
        }

        [Fact]
        public void ProjectProposal_AllowsOptionalResearchArea()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                ResearchAreaId = null  // Optional
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void ProjectProposal_AllowsOptionalSupervisorId()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "Valid Title",
                Abstract = "This is a valid abstract with sufficient length",
                TechnicalStack = "ASP.NET, C#",
                StudentId = "student-001",
                SupervisorId = null  // Optional initially
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void ProjectProposal_MultipleValidationErrors_ReturnsAllErrors()
        {
            // Arrange
            var proposal = new ProjectProposal
            {
                Title = "AB",  // Too short
                Abstract = "Short",  // Too short
                TechnicalStack = "C#",  // Too short
                StudentId = ""  // Missing
            };

            // Act
            var results = ValidateModel(proposal);

            // Assert
            Assert.True(results.Count >= 3, "Should have at least 3 validation errors");
        }
    }
}
