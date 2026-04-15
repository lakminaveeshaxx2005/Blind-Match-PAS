using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Blind_Match_PAS.Authorization
{
    /// <summary>
    /// Requirement for proposing-related authorization checks.
    /// Ensures users can only access/modify their own proposals.
    /// </summary>
    public class ProposalOwnershipRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Handler for verifying proposal ownership.
    /// Checks that the current user is the student who created the proposal.
    /// </summary>
    public class ProposalOwnershipHandler : AuthorizationHandler<ProposalOwnershipRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProposalOwnershipHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProposalOwnershipRequirement requirement)
        {
            // Extract proposal ID from route values
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.GetRouteValue("id") is string proposalId && int.TryParse(proposalId, out var id))
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Store for later validation in controller
                httpContext.Items["ProposalId"] = id;
                httpContext.Items["StudentId"] = userId;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Requirement for supervisor match operations.
    /// Ensures supervisors can only work with pending proposals they haven't already matched.
    /// </summary>
    public class SupervisorMatchingRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Handler for supervisor matching operations.
    /// Checks that supervisor hasn't already matched with another proposal for the same student.
    /// </summary>
    public class SupervisorMatchingHandler : AuthorizationHandler<SupervisorMatchingRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SupervisorMatchingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SupervisorMatchingRequirement requirement)
        {
            var supervisorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Store supervisor ID for controller-level validation
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                httpContext.Items["SupervisorId"] = supervisorId;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
