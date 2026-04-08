using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blind_Match_PAS.Repositories
{
    public class MatchingRequestRepository : IMatchingRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public MatchingRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MatchingRequest>> GetAllAsync()
        {
            return await _context.MatchingRequests
                .Include(r => r.Student)
                .Include(r => r.Supervisor)
                .ToListAsync();
        }

        public async Task<MatchingRequest> GetByIdAsync(int id)
        {
            return await _context.MatchingRequests
                .Include(r => r.Student)
                .Include(r => r.Supervisor)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<MatchingRequest>> GetByStudentIdAsync(string studentId)
        {
            return await _context.MatchingRequests
                .Where(r => r.StudentId == studentId)
                .Include(r => r.Supervisor)
                .ToListAsync();
        }

        public async Task<IEnumerable<MatchingRequest>> GetBySupervisorIdAsync(string supervisorId)
        {
            return await _context.MatchingRequests
                .Where(r => r.SupervisorId == supervisorId)
                .Include(r => r.Student)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<MatchingRequest> GetByProposalAndSupervisorAsync(int proposalId, string supervisorId)
        {
            return await _context.MatchingRequests
                .Where(r => r.ProposalId == proposalId && r.SupervisorId == supervisorId)
                .Include(r => r.Student)
                .Include(r => r.Supervisor)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(MatchingRequest request)
        {
            await _context.MatchingRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(MatchingRequest request)
        {
            _context.MatchingRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var request = await GetByIdAsync(id);
            if (request != null)
            {
                _context.MatchingRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.MatchingRequests.AnyAsync(r => r.Id == id);
        }
    }
}