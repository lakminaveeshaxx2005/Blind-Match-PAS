using Blind_Match_PAS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blind_Match_PAS.Repositories
{
    public interface IMatchingRequestRepository
    {
        Task<IEnumerable<MatchingRequest>> GetAllAsync();
        Task<MatchingRequest> GetByIdAsync(int id);
        Task<IEnumerable<MatchingRequest>> GetByStudentIdAsync(string studentId);
        Task<IEnumerable<MatchingRequest>> GetBySupervisorIdAsync(string supervisorId);
        Task<MatchingRequest> GetByProposalAndSupervisorAsync(int proposalId, string supervisorId);
        Task AddAsync(MatchingRequest request);
        Task UpdateAsync(MatchingRequest request);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}