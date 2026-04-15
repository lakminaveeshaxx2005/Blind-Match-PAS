using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;
using Blind_Match_PAS.Repositories;
using Blind_Match_PAS.Services;

namespace Blind_Match_PAS.Controllers
{
    /// <summary>
    /// Supervisor controller for browsing student project proposals and expressing interest.
    /// Implements blind matching: supervisors see anonymous pending proposals, and identities
    /// are revealed only after a match is confirmed.
    /// </summary>
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly CustomDbContext _customContext;
        private readonly ApplicationDbContext _applicationContext;
        private readonly IMatchingRequestRepository _matchingRequestRepository;
        private readonly IMatchingService _matchingService;

        public SupervisorController(CustomDbContext customContext, ApplicationDbContext applicationContext, IMatchingRequestRepository matchingRequestRepository, IMatchingService matchingService)
        {
            _customContext = customContext;
            _applicationContext = applicationContext;
            _matchingRequestRepository = matchingRequestRepository;
            _matchingService = matchingService;
        }

        private async Task AddMatchRecordAsync(ProjectProposal proposal)
        {
            if (proposal == null || string.IsNullOrEmpty(proposal.StudentId) || string.IsNullOrEmpty(proposal.SupervisorId))
            {
                return;
            }

            var existingMatch = await _customContext.Matches.FirstOrDefaultAsync(m => m.ProposalId == proposal.Id);
            if (existingMatch != null)
            {
                return;
            }

            var match = new Match
            {
                ProposalId = proposal.Id,
                StudentId = proposal.StudentId,
                SupervisorId = proposal.SupervisorId,
                MatchedAt = proposal.MatchedAt ?? DateTime.UtcNow
            };

            await _customContext.Matches.AddAsync(match);
        }

        /// <summary>
        /// GET: Supervisor/Index
        /// Displays the supervisor dashboard with:
        /// - Dashboard statistics (pending, accepted, rejected requests)
        /// - Pending proposals for blind review (anonymous)
        /// - Matching requests from students (anonymous until accepted)
        /// - Matched proposals with identity revealed
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> Index()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var viewModel = new SupervisorDashboardViewModel();

            try
            {
                Console.WriteLine($"DEBUG: Loading dashboard for supervisor: {supervisorId}");

                // Get pending proposals for blind review (no student info)
                var pendingProposals = await _customContext.ProjectProposals
                    .Where(p => p.Status == ProjectStatus.Pending)
                    .Include(p => p.ResearchArea)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                viewModel.PendingProposals = pendingProposals.Select(p => new AnonymousProposalDto
                {
                    ProposalId = p.Id,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    TechnicalStack = p.TechnicalStack,
                    ResearchArea = p.ResearchArea?.Name,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status
                }).ToList();

                viewModel.TotalPendingProposals = viewModel.PendingProposals.Count;
                Console.WriteLine($"DEBUG: Found {viewModel.TotalPendingProposals} pending proposals");

                // Get proposals matched by this supervisor (identity revealed)
                var matchedProposals = await _customContext.ProjectProposals
                    .Where(p => p.SupervisorId == supervisorId && p.Status == ProjectStatus.Matched)
                    .Include(p => p.ResearchArea)
                    .OrderByDescending(p => p.MatchedAt)
                    .ToListAsync();

                var revealedProposals = new List<RevealedProposalDto>();
                foreach (var proposal in matchedProposals)
                {
                    var student = await _applicationContext.Users.FindAsync(proposal.StudentId);
                    revealedProposals.Add(new RevealedProposalDto
                    {
                        ProposalId = proposal.Id,
                        Title = proposal.Title,
                        Abstract = proposal.Abstract,
                        TechnicalStack = proposal.TechnicalStack,
                        StudentName = student?.FullName ?? "Unknown",
                        StudentEmail = student?.Email ?? "N/A",
                        ResearchArea = proposal.ResearchArea?.Name,
                        CreatedAt = proposal.CreatedAt,
                        MatchedAt = proposal.MatchedAt,
                        Status = proposal.Status
                    });
                }

                viewModel.MatchedProposals = revealedProposals;
                viewModel.TotalMatches = revealedProposals.Count;
                Console.WriteLine($"DEBUG: Found {viewModel.TotalMatches} matched proposals");

                // Get all matching requests for this supervisor
                var allMatchingRequests = (await _matchingRequestRepository.GetBySupervisorIdAsync(supervisorId)).ToList();

                // Calculate statistics for pending, accepted, and rejected requests
                var pendingRequests = allMatchingRequests.Where(r => r.Status == MatchingRequestStatus.Pending).ToList();
                var acceptedRequests = allMatchingRequests.Where(r => r.Status == MatchingRequestStatus.Accepted).ToList();
                var rejectedRequests = allMatchingRequests.Where(r => r.Status == MatchingRequestStatus.Rejected).ToList();

                viewModel.TotalPendingRequests = pendingRequests.Count;
                viewModel.TotalAcceptedRequests = acceptedRequests.Count;
                viewModel.TotalRejectedRequests = rejectedRequests.Count;
                viewModel.TotalMatchingRequests = allMatchingRequests.Count;

                Console.WriteLine($"DEBUG: Pending: {viewModel.TotalPendingRequests}, Accepted: {viewModel.TotalAcceptedRequests}, Rejected: {viewModel.TotalRejectedRequests}");

                // Check supervisor limit
                if (viewModel.TotalAcceptedRequests >= viewModel.MaxStudentLimit)
                {
                    viewModel.SupervisorLimitError = $"You have reached the maximum limit of {viewModel.MaxStudentLimit} students. You cannot accept more requests.";
                    Console.WriteLine($"DEBUG: Supervisor limit reached: {viewModel.TotalAcceptedRequests}/{viewModel.MaxStudentLimit}");
                }

                // Get matching request DTOs for display
                var requestDtos = new List<MatchingRequestDto>();
                foreach (var request in pendingRequests)
                {
                    var proposal = await _customContext.ProjectProposals.FindAsync(request.ProposalId);
                    if (proposal != null)
                    {
                        requestDtos.Add(new MatchingRequestDto
                        {
                            RequestId = request.Id,
                            ProposalId = request.ProposalId,
                            Title = proposal.Title,
                            Abstract = proposal.Abstract,
                            TechnicalStack = proposal.TechnicalStack,
                            ResearchArea = proposal.ResearchArea?.Name,
                            RequestedAt = request.CreatedAt,
                            MatchScore = 0, // TODO: Calculate match score based on research area/technical stack
                            Status = request.Status
                        });
                    }
                }

                viewModel.MatchingRequests = requestDtos;
                Console.WriteLine($"DEBUG: Loaded {requestDtos.Count} matching request DTOs");

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Exception loading supervisor dashboard: {ex.Message}");
                Console.WriteLine($"ERROR: Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while loading your dashboard.";
                return View("Index", viewModel);
            }
        }

        // GET: Supervisor/ReviewProposals (Legacy - redirects to Index)
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public IActionResult ReviewProposals()
        {
            return RedirectToAction("Index");
        }

        // GET: Supervisor/EditProfile
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> EditProfile()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var supervisor = await _applicationContext.Users.FindAsync(supervisorId);
            if (supervisor == null)
            {
                return NotFound("Supervisor profile not found.");
            }

            var model = new SupervisorProfileViewModel
            {
                Id = supervisor.Id,
                Expertise = supervisor.Expertise,
                ResearchInterest = supervisor.ResearchInterest
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> EditProfile(SupervisorProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var supervisor = await _applicationContext.Users.FindAsync(model.Id);
            if (supervisor == null)
            {
                return NotFound("Supervisor profile not found.");
            }

            supervisor.Expertise = model.Expertise;
            supervisor.ResearchInterest = model.ResearchInterest;
            _applicationContext.Users.Update(supervisor);
            await _applicationContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Supervisor profile updated successfully.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// POST: Supervisor/MatchProject
        /// Supervisor expresses interest in a pending proposal.
        /// Updates the proposal status to 'Matched' and assigns the SupervisorId.
        /// Triggers the identity reveal for communication purposes.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> MatchProject(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var proposal = await _customContext.ProjectProposals.FindAsync(id);
            if (proposal == null)
            {
                TempData["ErrorMessage"] = "Proposal not found.";
                return RedirectToAction("Index");
            }

            if (proposal.Status != ProjectStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending proposals can be expressed interest in.";
                return RedirectToAction("Index");
            }

            // Express interest in the proposal without confirming the match yet
            proposal.SupervisorId = supervisorId;
            proposal.Status = ProjectStatus.Interested;
            proposal.IsIdentityRevealed = false;
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _customContext.ProjectProposals.Update(proposal);
                await _customContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Interest expressed successfully. Confirm the match to reveal student identity.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error expressing interest: {ex.Message}";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// POST: Supervisor/AcceptRequest
        /// Supervisor accepts a matching request from a student.
        /// Checks supervisor limit before accepting.
        /// Updates the request status to 'Accepted' and reveals student identity.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            Console.WriteLine($"DEBUG: AcceptRequest called for requestId: {requestId}, supervisorId: {supervisorId}");

            var request = await _matchingRequestRepository.GetByIdAsync(requestId);
            if (request == null || request.SupervisorId != supervisorId)
            {
                TempData["ErrorMessage"] = "Request not found or access denied.";
                Console.WriteLine($"DEBUG: Request not found or access denied. Request is null: {request == null}");
                return RedirectToAction("Index");
            }

            if (request.Status != MatchingRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending requests can be accepted.";
                Console.WriteLine($"DEBUG: Request is not pending. Current status: {request.Status}");
                return RedirectToAction("Index");
            }

            // Check supervisor limit
            var acceptedRequestsCount = (await _matchingRequestRepository.GetBySupervisorIdAsync(supervisorId))
                .Count(r => r.Status == MatchingRequestStatus.Accepted);

            const int maxStudentLimit = 5;
            if (acceptedRequestsCount >= maxStudentLimit)
            {
                TempData["ErrorMessage"] = $"You have reached the maximum limit of {maxStudentLimit} students. You cannot accept more requests.";
                Console.WriteLine($"DEBUG: Supervisor limit reached: {acceptedRequestsCount}/{maxStudentLimit}");
                return RedirectToAction("Index");
            }

            // Update request status
            request.Status = MatchingRequestStatus.Accepted;
            request.UpdatedAt = DateTime.UtcNow;

            // Update proposal to matched status and assign supervisor
            var proposal = await _customContext.ProjectProposals.FindAsync(request.ProposalId);
            if (proposal != null)
            {
                proposal.SupervisorId = supervisorId;
                proposal.Status = ProjectStatus.Matched;
                proposal.IsIdentityRevealed = true;
                proposal.MatchedAt = DateTime.UtcNow;
                proposal.LastModifiedAt = DateTime.UtcNow;
                await AddMatchRecordAsync(proposal);
                _customContext.ProjectProposals.Update(proposal);

                Console.WriteLine($"DEBUG: Updated proposal {proposal.Id} to Matched status with supervisor {supervisorId}");
            }

            try
            {
                await _matchingRequestRepository.UpdateAsync(request);
                await _customContext.SaveChangesAsync();

                // Get student info for notification
                var student = await _applicationContext.Users.FindAsync(proposal?.StudentId);
                Console.WriteLine($"DEBUG: Request accepted successfully. Student: {student?.FullName ?? "Unknown"}");

                TempData["SuccessMessage"] = $"Request accepted! {student?.FullName ?? "The student"}'s identity is now revealed. You can now communicate directly.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error accepting request: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error accepting request: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// POST: Supervisor/RejectRequest
        /// Supervisor rejects a matching request from a student.
        /// Updates the request status to 'Rejected'.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            Console.WriteLine($"DEBUG: RejectRequest called for requestId: {requestId}, supervisorId: {supervisorId}");

            var request = await _matchingRequestRepository.GetByIdAsync(requestId);
            if (request == null || request.SupervisorId != supervisorId)
            {
                TempData["ErrorMessage"] = "Request not found or access denied.";
                Console.WriteLine($"DEBUG: Request not found or access denied.");
                return RedirectToAction("Index");
            }

            if (request.Status != MatchingRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending requests can be rejected.";
                Console.WriteLine($"DEBUG: Request is not pending. Current status: {request.Status}");
                return RedirectToAction("Index");
            }

            // Update request status
            request.Status = MatchingRequestStatus.Rejected;
            request.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _matchingRequestRepository.UpdateAsync(request);
                Console.WriteLine($"DEBUG: Request {requestId} rejected successfully");
                TempData["SuccessMessage"] = "Request rejected. The student will be notified.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error rejecting request: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error rejecting request: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: Supervisor/Approve
        // Supervisor approves a pending proposal.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var proposal = await _customContext.ProjectProposals.FindAsync(id);
            if (proposal == null)
            {
                TempData["ErrorMessage"] = "Proposal not found.";
                return RedirectToAction("ReviewProposals");
            }

            if (proposal.Status != ProjectStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending proposals can be approved.";
                return RedirectToAction("ReviewProposals");
            }

            proposal.SupervisorId = supervisorId;
            proposal.Status = ProjectStatus.Approved;
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _customContext.ProjectProposals.Update(proposal);
                await _customContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Proposal approved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving proposal: {ex.Message}";
            }

            return RedirectToAction("ReviewProposals");
        }

        // POST: Supervisor/Reject
        // Supervisor rejects a pending proposal.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var proposal = await _customContext.ProjectProposals.FindAsync(id);
            if (proposal == null)
            {
                TempData["ErrorMessage"] = "Proposal not found.";
                return RedirectToAction("ReviewProposals");
            }

            if (proposal.Status != ProjectStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending proposals can be rejected.";
                return RedirectToAction("ReviewProposals");
            }

            proposal.SupervisorId = supervisorId;
            proposal.Status = ProjectStatus.Rejected;
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _customContext.ProjectProposals.Update(proposal);
                await _customContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Proposal rejected successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rejecting proposal: {ex.Message}";
            }

            return RedirectToAction("ReviewProposals");
        }

        /// <summary>
        /// POST: Supervisor/ConfirmMatch
        /// Supervisor confirms a pending matching request and marks it as Matched.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> ConfirmMatch(int requestId, bool isRequest = true)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var request = await _matchingRequestRepository.GetByIdAsync(requestId);
            if (request == null || request.SupervisorId != supervisorId)
            {
                TempData["ErrorMessage"] = "Matching request not found or not authorized.";
                return RedirectToAction("BlindReview");
            }

            if (request.Status != MatchingRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending requests can be confirmed.";
                return RedirectToAction("BlindReview");
            }

            request.Status = MatchingRequestStatus.Accepted;

            try
            {
                await _matchingRequestRepository.UpdateAsync(request);
                TempData["SuccessMessage"] = "Match confirmed successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error confirming match: {ex.Message}";
            }

            return RedirectToAction("BlindReview");
        }

        // GET: Supervisor/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proposal = await _customContext.ProjectProposals
                .Include(p => p.ResearchArea)
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (proposal == null)
            {
                return NotFound();
            }

            var currentSupervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (proposal.Status != ProjectStatus.Pending && proposal.SupervisorId != currentSupervisorId)
            {
                return Forbid();
            }

            // Only reveal user identity when the proposal is matched (or identity flag set),
            // otherwise keep student/supervisor details hidden.
            string? studentName = null;
            string? studentEmail = null;
            string? supervisorName = null;
            string? supervisorEmail = null;

            if (proposal.Status == ProjectStatus.Matched || proposal.IsIdentityRevealed || proposal.SupervisorId == currentSupervisorId)
            {
                // reveal identities for matched proposals or when identity flag is set
                var student = await _applicationContext.Users.FindAsync(proposal.StudentId);
                studentName = student?.FullName;
                studentEmail = student?.Email;

                if (!string.IsNullOrEmpty(proposal.SupervisorId))
                {
                    var supervisor = await _applicationContext.Users.FindAsync(proposal.SupervisorId);
                    supervisorName = supervisor?.FullName;
                    supervisorEmail = supervisor?.Email;
                }
            }

            ViewBag.StudentName = studentName;
            ViewBag.StudentEmail = studentEmail;
            ViewBag.SupervisorName = supervisorName;
            ViewBag.SupervisorEmail = supervisorEmail;

            return View(proposal);
        }

        // POST: Supervisor/ConfirmMatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMatch(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var proposal = await _customContext.ProjectProposals.FindAsync(id);
            if (proposal == null)
            {
                TempData["ErrorMessage"] = "Proposal not found.";
                return RedirectToAction("Index");
            }

            if (proposal.SupervisorId != supervisorId)
            {
                TempData["ErrorMessage"] = "Only the supervisor who expressed interest can confirm this match.";
                return RedirectToAction("Index");
            }

            if (proposal.Status != ProjectStatus.Interested)
            {
                TempData["ErrorMessage"] = "Only proposals with expressed interest can be confirmed.";
                return RedirectToAction("Index");
            }

            proposal.Status = ProjectStatus.Matched;
            proposal.IsIdentityRevealed = true;
            proposal.MatchedAt = DateTime.UtcNow;
            proposal.LastModifiedAt = DateTime.UtcNow;
            await AddMatchRecordAsync(proposal);

            try
            {
                _customContext.ProjectProposals.Update(proposal);
                await _customContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error confirming match: {ex.Message}";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Match confirmed successfully.";
            return RedirectToAction("Details", new { id = proposal.Id });
        }

        /// <summary>
        /// GET: Supervisor/BlindReview
        /// Displays pending matching requests anonymously for blind review
        /// Shows only project details and match scores, no student personal information
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> BlindReview()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var viewModel = new BlindReviewViewModel();

            // Get the supervisor's research interests for match score calculation
            var supervisor = await _applicationContext.Users.FindAsync(supervisorId);
            if (supervisor == null)
            {
                return Unauthorized("Supervisor not found.");
            }

            // Get pending requests for this supervisor
            var pendingRequests = await _matchingRequestRepository.GetBySupervisorIdAsync(supervisorId);
            var pendingRequestList = pendingRequests.Where(r => r.Status == MatchingRequestStatus.Pending).ToList();

            foreach (var request in pendingRequestList)
            {
                // Get the student's latest proposal (blind - no student info shown)
                var studentProposals = await _customContext.ProjectProposals
                    .Where(p => p.StudentId == request.StudentId)
                    .Include(p => p.ResearchArea)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var latestProposal = studentProposals.FirstOrDefault();
                if (latestProposal != null)
                {
                    // Calculate match score using proposal details vs supervisor expertise
                    var studentInterest = $"{latestProposal.TechnicalStack} {latestProposal.ResearchArea?.Name}".Trim();
                    var matchScore = _matchingService.CalculateMatchScore(studentInterest, supervisor.ResearchInterest ?? "");

                    var reviewItem = new BlindReviewItem
                    {
                        RequestId = request.Id,
                        ProjectTitle = latestProposal.Title,
                        ProjectAbstract = latestProposal.Abstract,
                        TechStack = latestProposal.TechnicalStack,
                        ResearchAreas = latestProposal.ResearchArea?.Name ?? "Not specified",
                        MatchScore = matchScore
                    };

                    viewModel.ReviewItems.Add(reviewItem);
                }
            }

            // Sort by match score descending
            viewModel.ReviewItems = viewModel.ReviewItems.OrderByDescending(r => r.MatchScore).ToList();

            return View(viewModel);
        }

        // GET: Supervisor/AvailableProjects
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> AvailableProjects()
        {
            // Return a lightweight summary of pending proposals for supervisors to browse
            var projects = await _customContext.ProjectProposals
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.Pending)
                .Include(p => p.ResearchArea)
                .Select(p => new Models.ProjectSummaryViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    TechStack = p.TechnicalStack,
                    ResearchArea = p.ResearchArea != null ? p.ResearchArea.Name : null,
                    Status = p.Status.ToString()
                })
                .ToListAsync();

            return View(projects);
        }
    }
}
