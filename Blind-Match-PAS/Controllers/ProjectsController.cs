using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly CustomDbContext _context;
        private readonly ApplicationDbContext _appContext;

        public ProjectsController(CustomDbContext context, ApplicationDbContext appContext)
        {
            _context = context;
            _appContext = appContext;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects.ToListAsync();
            return View(projects);
        }

        // GET: Projects/AvailableProjects
        // Supervisor-only: show pending projects filtered by the supervisor's expertise/research areas
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> AvailableProjects()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _appContext.ApplicationUsers.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var expertise = user.Expertise?.Trim();

            var query = _context.Projects.AsQueryable();
            // There is no Status on Project; show all projects and let supervisor filter by research area

            if (!string.IsNullOrEmpty(expertise))
            {
                var normalized = expertise.ToLower();
                query = query.Where(p => !string.IsNullOrEmpty(p.ResearchArea) && p.ResearchArea.ToLower().Contains(normalized));
            }

            var projects = await query.ToListAsync();
            return View(projects);
        }

        // GET: Projects/MyProjects
        // Shows only the projects belonging to the currently logged-in student
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyProjects()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var projects = await _context.Projects
                .Where(p => p.StudentId == userId)
                .ToListAsync();

            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(m => m.Id == id.Value);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            if (!ModelState.IsValid)
            {
                return View(project);
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            project.StudentId = studentId;
            _context.Add(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Projects/Edit/5
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId) || project.StudentId != studentId)
            {
                return Forbid();
            }

            return View(project);
        }

        // POST: Projects/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            if (id != project.Id)
            {
                return BadRequest();
            }

            var existingProject = await _context.Projects.FindAsync(id);
            if (existingProject == null)
            {
                return NotFound();
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId) || existingProject.StudentId != studentId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(project);
            }

            // Update allowed fields only
            existingProject.Title = project.Title;
            existingProject.Abstract = project.Abstract;
            existingProject.TechStack = project.TechStack;
            existingProject.ResearchArea = project.ResearchArea;

            try
            {
                _context.Update(existingProject);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(project.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction("MyProjects");
        }

        // GET: Projects/Delete/5 (Withdraw)
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId) || project.StudentId != studentId)
            {
                return Forbid();
            }

            return View(project);
        }

        // POST: Projects/Delete/5 (Withdraw)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId) || project.StudentId != studentId)
            {
                return Forbid();
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return RedirectToAction("MyProjects");
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}
