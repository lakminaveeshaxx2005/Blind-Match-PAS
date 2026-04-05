using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Blind_Match_PAS.Controllers;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using Xunit;

namespace BlindMatch.Tests
{
    public class ProjectControllerTests
    {
        [Fact]
        public async Task Index_PendingProposal_HidesStudentIdentity()
        {
            // Arrange
            var proposals = new List<ProjectProposal>
            {
                new ProjectProposal
                {
                    Id = 1,
                    Title = "Automated Testing",
                    Abstract = "A sample abstract for a pending proposal.",
                    TechnicalStack = "C#",
                    ResearchAreaId = 1,
                    ResearchArea = new ResearchArea { Id = 1, Name = "AI" },
                    Status = ProjectStatus.Pending,
                    StudentId = "student-1"
                }
            };

            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "student-1",
                    FullName = "Jane Doe"
                }
            };

            var proposalsSet = CreateDbSetMock(proposals);
            var usersSet = CreateDbSetMock(users);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ProjectControllerTests_Index")
                .Options;

            var contextMock = new Mock<ApplicationDbContext>(options) { CallBase = true };
            contextMock.Object.ProjectProposals = proposalsSet.Object;
            contextMock.Object.ApplicationUsers = usersSet.Object;

            var controller = new ProjectController(contextMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "supervisor-1"),
                            new Claim(ClaimTypes.Role, "Supervisor")
                        }, "TestAuth"))
                    }
                }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ProjectController.SupervisorProjectProposalViewModel>>(viewResult.Model);
            var proposalViewModel = Assert.Single(model);

            Assert.Null(proposalViewModel.StudentId);
            Assert.Null(proposalViewModel.StudentName);
        }

        private static Mock<DbSet<T>> CreateDbSetMock<T>(IEnumerable<T> items) where T : class
        {
            var queryable = items.AsQueryable();

            var dbSetMock = new Mock<DbSet<T>>();
            dbSetMock.As<IAsyncEnumerable<T>>()
                .Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            return dbSetMock;
        }

        private class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;

            public TestAsyncQueryProvider(IQueryProvider inner)
            {
                _inner = inner;
            }

            public IQueryable CreateQuery(Expression expression)
            {
                return new TestAsyncEnumerable<TEntity>(expression);
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new TestAsyncEnumerable<TElement>(expression);
            }

            public object Execute(Expression expression)
            {
                return _inner.Execute(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                return _inner.Execute<TResult>(expression);
            }

            public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            {
                return Execute<TResult>(expression);
            }

            public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
            {
                return new TestAsyncEnumerable<TResult>(expression);
            }
        }

        private class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable)
                : base(enumerable)
            {
            }

            public TestAsyncEnumerable(Expression expression)
                : base(expression)
            {
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            }

            IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
        }

        private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner;
            }

            public T Current => _inner.Current;

            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return ValueTask.CompletedTask;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return new ValueTask<bool>(_inner.MoveNext());
            }
        }
    }
}
