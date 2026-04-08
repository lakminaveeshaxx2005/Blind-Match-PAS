using System.Reflection;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlindMatch.Tests
{
    public class MatchingServiceTests
    {
        private static MatchingService CreateService()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "MatchingServiceTests")
                .Options;

            var context = new ApplicationDbContext(options);
            return new MatchingService(context);
        }

        private static int InvokeCalculateMatchScore(MatchingService service, string? studentInterest, string? supervisorExpertise)
        {
            var method = typeof(MatchingService).GetMethod("CalculateMatchScore", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            return (int)method.Invoke(service, new object?[] { studentInterest, supervisorExpertise })!;
        }

        [Fact]
        public void CalculateMatchScore_MultipleMatchingKeywords_Returns20()
        {
            // Arrange
            var service = CreateService();
            var studentInterest = "AI, ML";
            var supervisorExpertise = "AI, ML, Data Science";

            // Act
            var score = InvokeCalculateMatchScore(service, studentInterest, supervisorExpertise);

            // Assert
            Assert.Equal(20, score);
        }

        [Fact]
        public void CalculateMatchScore_OneMatchingKeyword_Returns10()
        {
            // Arrange
            var service = CreateService();
            var studentInterest = "AI, NLP";
            var supervisorExpertise = "AI, Data Science";

            // Act
            var score = InvokeCalculateMatchScore(service, studentInterest, supervisorExpertise);

            // Assert
            Assert.Equal(10, score);
        }

        [Fact]
        public void CalculateMatchScore_NoMatchingKeywords_Returns0()
        {
            // Arrange
            var service = CreateService();
            var studentInterest = "AI, ML";
            var supervisorExpertise = "Cloud, Security";

            // Act
            var score = InvokeCalculateMatchScore(service, studentInterest, supervisorExpertise);

            // Assert
            Assert.Equal(0, score);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("AI", "")]
        [InlineData("", "ML")]
        public void CalculateMatchScore_NullOrEmptyInput_Returns0(string? studentInterest, string? supervisorExpertise)
        {
            // Arrange
            var service = CreateService();

            // Act
            var score = InvokeCalculateMatchScore(service, studentInterest, supervisorExpertise);

            // Assert
            Assert.Equal(0, score);
        }
    }
}
