using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blind_Match_PAS.Services
{
    public interface IMatchingService
    {
        Task<List<SupervisorMatch>> GetBestMatchesForStudent(string studentId);
        Task<List<SupervisorMatch>> GetBestMatchesForProposal(int proposalId);
        int CalculateMatchScore(string studentInterest, string supervisorExpertise);
    }

    public class MatchingService : IMatchingService
    {
        private readonly ApplicationDbContext _context;
        private readonly CustomDbContext? _customContext;

        public MatchingService(ApplicationDbContext context, CustomDbContext? customContext = null)
        {
            _context = context;
            _customContext = customContext;
        }

        /// <summary>
        /// Gets best supervisor matches based on PROJECT PROPOSAL data
        /// Uses Abstract + TechnicalStack + ResearchArea keywords
        /// Only returns supervisors with score > 0
        /// </summary>
        public async Task<List<SupervisorMatch>> GetBestMatchesForProposal(int proposalId)
        {
            if (_customContext == null)
            {
                return new List<SupervisorMatch>();
            }

            // Get the proposal with its research area
            var proposal = await _customContext.ProjectProposals
                .Include(p => p.ResearchArea)
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (proposal == null)
            {
                return new List<SupervisorMatch>();
            }

            // Combine proposal data: Abstract + TechnicalStack + ResearchArea name
            var proposalKeywords = new List<string>();

            if (!string.IsNullOrEmpty(proposal.Abstract))
                proposalKeywords.Add(proposal.Abstract);

            if (!string.IsNullOrEmpty(proposal.TechnicalStack))
                proposalKeywords.Add(proposal.TechnicalStack);

            if (proposal.ResearchArea != null && !string.IsNullOrEmpty(proposal.ResearchArea.Name))
                proposalKeywords.Add(proposal.ResearchArea.Name);

            var proposalContent = string.Join(" ", proposalKeywords);
            Console.WriteLine($"DEBUG: Proposal combined text: '{proposalContent}'");

            // Get all supervisors
            var supervisors = await _context.Users
                .Where(u => u.UserRole == "Supervisor")
                .ToListAsync();

            // If the proposal has a research area, prefer supervisors whose Expertise includes that area.
            var researchAreaName = proposal.ResearchArea?.Name?.Trim();
            if (!string.IsNullOrEmpty(researchAreaName))
            {
                var normalizedArea = researchAreaName.ToLower();
                var matchingByArea = supervisors.Where(s => !string.IsNullOrEmpty(s.Expertise) &&
                    s.Expertise.ToLower().Split(new[] { ',', ' ', ';', '.', '-', '&', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => k.Trim())
                        .Any(k => k.Contains(normalizedArea) || normalizedArea.Contains(k))).ToList();

                // If we found supervisors whose expertise explicitly includes the research area, narrow the list to them.
                if (matchingByArea.Any())
                {
                    supervisors = matchingByArea;
                    Console.WriteLine($"DEBUG: Filtered supervisors to {supervisors.Count} matching research area '{researchAreaName}'");
                }
            }

            var matches = new List<SupervisorMatch>();

            foreach (var supervisor in supervisors)
            {
                if (string.IsNullOrEmpty(supervisor.Expertise))
                {
                    Console.WriteLine($"DEBUG: Skipping supervisor {supervisor.FullName} - no expertise");
                    continue;
                }

                // Calculate match score based on proposal vs supervisor expertise
                var matchScore = CalculateMatchScore(proposalContent, supervisor.Expertise);
                Console.WriteLine($"DEBUG: Supervisor {supervisor.FullName} expertise: '{supervisor.Expertise}' - Score: {matchScore}");

                // IMPORTANT: Only include supervisors with score > 0
                if (matchScore > 0)
                {
                    matches.Add(new SupervisorMatch
                    {
                        SupervisorId = supervisor.Id,
                        SupervisorName = supervisor.FullName,
                        Expertise = supervisor.Expertise,
                        MatchScore = matchScore
                    });
                }
            }

            // Sort by highest match score first (descending)
            return matches.OrderByDescending(m => m.MatchScore).ToList();
        }

        public async Task<List<SupervisorMatch>> GetBestMatchesForStudent(string studentId)
        {
            var student = await _context.Users.FindAsync(studentId);
            if (student == null)
            {
                return new List<SupervisorMatch>();
            }

            var supervisors = await _context.Users
                .Where(u => u.UserRole == "Supervisor")
                .ToListAsync();

            var matches = new List<SupervisorMatch>();

            foreach (var supervisor in supervisors)
            {
                var matchScore = 0;

                // Score based on expertise/research interest match
                if (!string.IsNullOrEmpty(student.ResearchInterest) && !string.IsNullOrEmpty(supervisor.Expertise))
                {
                    matchScore += CalculateMatchScore(student.ResearchInterest, supervisor.Expertise);
                }

                // Only include supervisors with some matching score or any expertise
                if (matchScore > 0 || !string.IsNullOrEmpty(supervisor.Expertise))
                {
                    matches.Add(new SupervisorMatch
                    {
                        SupervisorId = supervisor.Id,
                        SupervisorName = supervisor.FullName,
                        Expertise = supervisor.Expertise ?? "General",
                        MatchScore = matchScore
                    });
                }
            }

            return matches.OrderByDescending(m => m.MatchScore).ToList();
        }

        public int CalculateMatchScore(string studentInterest, string supervisorExpertise)
        {
            if (string.IsNullOrEmpty(studentInterest) || string.IsNullOrEmpty(supervisorExpertise))
                return 0;

            var studentKeywords = studentInterest.ToLower().Split(new[] { ',', ' ', ';', '.', '-', '&', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 2)
                .ToArray();

            var supervisorKeywords = supervisorExpertise.ToLower().Split(new[] { ',', ' ', ';', '.', '-', '&', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 2)
                .ToArray();

            Console.WriteLine($"DEBUG: Extracted student keywords: {string.Join(", ", studentKeywords)}");
            Console.WriteLine($"DEBUG: Extracted supervisor keywords: {string.Join(", ", supervisorKeywords)}");

            int score = 0;
            foreach (var studentKeyword in studentKeywords)
            {
                foreach (var supervisorKeyword in supervisorKeywords)
                {
                    if (supervisorKeyword.Contains(studentKeyword) || studentKeyword.Contains(supervisorKeyword))
                    {
                        score += 10;
                        break; // Don't double count for same keyword
                    }
                }
            }

            Console.WriteLine($"DEBUG: Final score: {score}");
            return score;
        }

        // Added overload to calculate match score based on a Project's research area
        public double CalculateMatchScore(Project project, string expertise)
        {
            if (project == null || expertise == null)
                return 0;

            return project.ResearchArea == expertise ? 1 : 0;
        }
    }

    public class SupervisorMatch
    {
        public string SupervisorId { get; set; }
        public string SupervisorName { get; set; }
        public string Expertise { get; set; }
        public int MatchScore { get; set; }
    }
}