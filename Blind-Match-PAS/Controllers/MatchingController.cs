using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;
using System.Linq;
using Blind_Match_PAS.Repositories;
using Blind_Match_PAS.Services;

namespace Blind_Match_PAS.Controllers
{
    [Authorize]
    public class MatchingController : Controller
    {
        private readonly IMatchingRequestRepository _matchingRequestRepository;
        private readonly IMatchingService _matchingService;
        private readonly ApplicationDbContext _context;

        public MatchingController(IMatchingRequestRepository matchingRequestRepository, IMatchingService matchingService, ApplicationDbContext context)
        {
            _matchingRequestRepository = matchingRequestRepository;
            _matchingService = matchingService;
            _context = context;
        }

        /// <summary>
        /// GET: Matching/Index
        /// Displays best supervisor matches for the current student based on research interests
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Index()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var matches = await _matchingService.GetBestMatchesForStudent(studentId);
            return View(matches);
        }

        /// <summary>
        /// POST: Matching/ApplyForSupervisor
        /// Student applies for a supervisor by creating a MatchingRequest
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ApplyForSupervisor(string supervisorId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            // Check if request already exists
            var existingRequests = await _matchingRequestRepository.GetByStudentIdAsync(studentId);
            if (existingRequests.Any(r => r.SupervisorId == supervisorId))
            {
                TempData["ErrorMessage"] = "You have already applied to this supervisor.";
                return RedirectToAction("Index");
            }

            var request = new MatchingRequest
            {
                StudentId = studentId,
                SupervisorId = supervisorId,
                Status = MatchingRequestStatus.Pending
            };

            await _matchingRequestRepository.AddAsync(request);

            TempData["SuccessMessage"] = "Application sent successfully.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// GET: Matching/Requests
        /// Supervisor views their matching requests
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> Requests()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized();
            }

            var requests = await _matchingRequestRepository.GetBySupervisorIdAsync(supervisorId);
            return View(requests);
        }

        /// <summary>
        /// GET: Matching/MyRequests
        /// Student views their own matching requests.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyRequests()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            Console.WriteLine($"DEBUG: MyRequests called for student: {studentId}");

            var requests = await _matchingRequestRepository.GetByStudentIdAsync(studentId);
            var requestList = requests.ToList();

            Console.WriteLine($"DEBUG: Found {requestList.Count} matching requests");

            foreach (var request in requestList)
            {
                Console.WriteLine($"DEBUG: Request ID: {request.Id}, Supervisor: {request.Supervisor?.FullName ?? "null"}, Status: {request.Status}, Created: {request.CreatedAt}");
            }

            return View("StudentRequests", requestList);
        }

        /// <summary>
        /// GET: Matching/EditProposal
        /// Student edits a pending or approved matching request.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EditProposal(int requestId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var request = await _matchingRequestRepository.GetByIdAsync(requestId);
            if (request == null || request.StudentId != studentId)
            {
                TempData["ErrorMessage"] = "Matching request not found.";
                return RedirectToAction("MyRequests");
            }

            if (request.Status == MatchingRequestStatus.Accepted || request.Status == MatchingRequestStatus.Rejected)
            {
                TempData["ErrorMessage"] = "Only pending requests can be edited.";
                return RedirectToAction("MyRequests");
            }

            var supervisors = await _context.Users
                .Where(u => u.UserRole == "Supervisor")
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();

            ViewBag.Supervisors = new SelectList(supervisors, "Id", "FullName", request.SupervisorId);
            return View(request);
        }

        /// <summary>
        /// POST: Matching/EditProposal
        /// Student saves the updated matching request supervisor selection.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EditProposal(int requestId, string supervisorId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var request = await _matchingRequestRepository.GetByIdAsync(requestId);
            if (request == null || request.StudentId != studentId)
            {
                TempData["ErrorMessage"] = "Matching request not found.";
                return RedirectToAction("MyRequests");
            }

            if (request.Status == MatchingRequestStatus.Accepted || request.Status == MatchingRequestStatus.Rejected)
            {
                TempData["ErrorMessage"] = "Only pending requests can be edited.";
                return RedirectToAction("MyRequests");
            }

            var supervisor = await _context.Users.FirstOrDefaultAsync(u => u.Id == supervisorId && u.UserRole == "Supervisor");
            if (supervisor == null)
            {
                TempData["ErrorMessage"] = "Selected supervisor is invalid.";
                return RedirectToAction("MyRequests");
            }

            request.SupervisorId = supervisorId;
            await _matchingRequestRepository.UpdateAsync(request);

            TempData["SuccessMessage"] = "Matching request updated successfully.";
            return RedirectToAction("MyRequests");
        }

        /// <summary>
        /// POST: Matching/WithdrawProposal
        /// Student withdraws a pending or approved matching request.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> WithdrawProposal(int requestId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var request = await _matchingRequestRepository.GetByIdAsync(requestId);
            if (request == null || request.StudentId != studentId)
            {
                TempData["ErrorMessage"] = "Matching request not found.";
                return RedirectToAction("MyRequests");
            }

            if (request.Status == MatchingRequestStatus.Accepted || request.Status == MatchingRequestStatus.Rejected)
            {
                TempData["ErrorMessage"] = "Only pending requests can be withdrawn.";
                return RedirectToAction("MyRequests");
            }

            await _matchingRequestRepository.DeleteAsync(requestId);
            TempData["SuccessMessage"] = "Matching request withdrawn successfully.";
            return RedirectToAction("MyRequests");
        }

        /// <summary>
        /// POST: Matching/ApproveRequest
        /// Supervisor approves a matching request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            return await UpdateRequestStatus(requestId, MatchingRequestStatus.Accepted);
        }

        /// <summary
        /// POST: Matching/RejectRequest
        /// Supervisor rejects a matching request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            return await UpdateRequestStatus(requestId, MatchingRequestStatus.Rejected);
        }

        private async Task<IActionResult> UpdateRequestStatus(int requestId, MatchingRequestStatus status)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized();
            }

            var request = await _matchingRequestRepository.GetByIdAsync(requestId);
            if (request == null || request.SupervisorId != supervisorId)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("Requests");
            }

            if (request.Status != MatchingRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Request has already been processed.";
                return RedirectToAction("Requests");
            }

            request.Status = status;
            await _matchingRequestRepository.UpdateAsync(request);

            TempData["SuccessMessage"] = $"Request {status.ToString().ToLower()} successfully.";
            return RedirectToAction("Requests");
        }
    }
}