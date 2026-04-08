using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using Blind_Match_PAS.Services;
using Blind_Match_PAS.Repositories;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly CustomDbContext _context;
        private readonly ApplicationDbContext _applicationContext;
        private readonly IMatchingService _matchingService;
        private readonly IMatchingRequestRepository _matchingRequestRepository;

        public StudentController(CustomDbContext context, ApplicationDbContext applicationContext, IMatchingService matchingService, IMatchingRequestRepository matchingRequestRepository)
        {
            _context = context;
            _applicationContext = applicationContext;
            _matchingService = matchingService;
            _matchingRequestRepository = matchingRequestRepository;
        }

        // 1. Dashboard: Displays the student's submitted proposals
        public async Task<IActionResult> Dashboard()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var proposals = await _context.ProjectProposals
                .Where(p => p.StudentId == studentId)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            return View(proposals);
        }

        /// <summary>
        /// GET: Student/FindMatches/{proposalId}
        /// Finds best matching supervisors for a specific project proposal
        /// Uses proposal abstract + technical stack + research area for matching
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> FindMatches(int proposalId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            // Verify proposal belongs to current student
            var proposal = await _context.ProjectProposals.FindAsync(proposalId);
            if (proposal == null || proposal.StudentId != studentId)
            {
                return NotFound();
            }

            // Get best matching supervisors based on proposal content
            var matches = await _matchingService.GetBestMatchesForProposal(proposalId);

            // Check which supervisors the student has already applied to for this proposal
            var existingRequests = await _matchingRequestRepository.GetByStudentIdAsync(studentId);
            var appliedSupervisorIds = existingRequests
                .Where(r => r.ProposalId == proposalId)
                .Select(r => r.SupervisorId)
                .ToHashSet();

            // Store the proposalId in ViewBag for the view
            ViewBag.ProposalId = proposalId;
            ViewBag.ProposalTitle = proposal.Title;
            ViewBag.MatchCount = matches.Count;
            ViewBag.AppliedSupervisorIds = appliedSupervisorIds;

            return View(matches);
        }

        /// <summary>
        /// POST: Student/ApplyToSupervisor
        /// Student applies to a supervisor for a specific proposal
        /// Creates a matching request and saves it to the database
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ApplyToSupervisor(string supervisorId, int proposalId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            Console.WriteLine($"DEBUG: ApplyToSupervisor called - StudentId: {studentId}, SupervisorId: {supervisorId}, ProposalId: {proposalId}");

            // Verify proposal belongs to current student
            var proposal = await _context.ProjectProposals.FindAsync(proposalId);
            if (proposal == null || proposal.StudentId != studentId)
            {
                Console.WriteLine($"DEBUG: Invalid proposal - proposal is null: {proposal == null}, student mismatch: {proposal?.StudentId != studentId}");
                TempData["ErrorMessage"] = "Invalid proposal.";
                return RedirectToAction("Dashboard");
            }

            // Check if already applied to this supervisor for this proposal
            var existingRequest = await _matchingRequestRepository.GetByProposalAndSupervisorAsync(proposalId, supervisorId);
            if (existingRequest != null)
            {
                Console.WriteLine($"DEBUG: Duplicate application found - existing request ID: {existingRequest.Id}");
                TempData["ErrorMessage"] = "You have already applied to this supervisor for this proposal.";
                return RedirectToAction("FindMatches", new { proposalId });
            }

            try
            {
                var matchingRequest = new MatchingRequest
                {
                    StudentId = studentId,
                    SupervisorId = supervisorId,
                    ProposalId = proposalId,
                    Status = MatchingRequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"DEBUG: Creating matching request - StudentId: {matchingRequest.StudentId}, SupervisorId: {matchingRequest.SupervisorId}, ProposalId: {matchingRequest.ProposalId}");

                await _matchingRequestRepository.AddAsync(matchingRequest);
                Console.WriteLine($"DEBUG: Matching request saved successfully with ID: {matchingRequest.Id}");

                TempData["SuccessMessage"] = "Your application has been sent to the supervisor!";
                return RedirectToAction("MyRequests", "Matching");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error applying to supervisor: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while submitting your application.";
                return RedirectToAction("FindMatches", new { proposalId });
            }
        }

        // 2. GET: Displays the Proposal submission page
        public async Task<IActionResult> SubmitProposal()
        {
            ViewBag.ResearchAreas = new SelectList(await _context.ResearchAreas.ToListAsync(), "Id", "Name");
            return View();
        }

        // 3. POST: Handles the proposal form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitProposal(ProjectProposal model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdString != null)
            {
                model.StudentId = userIdString;
                model.Status = ProjectStatus.Pending;

                _context.ProjectProposals.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("Dashboard");
            }

            ViewBag.ResearchAreas = new SelectList(await _context.ResearchAreas.ToListAsync(), "Id", "Name");
            return View(model);
        }

        // 4. GET: Create Project
        public IActionResult CreateProject()
        {
            return View();
        }

        // 5. POST: Create Project
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(Project model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            model.StudentId = studentId;
            _context.Projects.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your project was submitted successfully.";
            return RedirectToAction("Dashboard");
        }
    }
}