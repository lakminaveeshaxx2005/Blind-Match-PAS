namespace Blind_Match_PAS.Models
{
    public class ProjectSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Abstract { get; set; }
        public string? TechStack { get; set; }
        public string? ResearchArea { get; set; }
        public string? Status { get; set; }
    }
}
