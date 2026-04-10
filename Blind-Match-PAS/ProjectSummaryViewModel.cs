// File: Blind-Match-PAS/Models/ProjectSummaryViewModel.cs
public class ProjectSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? TechStack { get; set; }
    public string? ResearchArea { get; set; }
    public string? Status { get; set; }
}

// In your Supervisor controller
[Authorize(Roles = "Supervisor")]
public async Task<IActionResult> AvailableProjects()
{
    var projects = await _context.Projects
        .AsNoTracking()
        .Where(p => p.Status == "Pending")
        .Select(p => new ProjectSummaryViewModel
        {
            Id = p.Id,
            Title = p.Title,
            Abstract = p.Abstract,
            TechStack = p.TechStack,
            ResearchArea = p.ResearchArea,
            Status = p.Status
        })
        .ToListAsync();

    return View(projects);
}