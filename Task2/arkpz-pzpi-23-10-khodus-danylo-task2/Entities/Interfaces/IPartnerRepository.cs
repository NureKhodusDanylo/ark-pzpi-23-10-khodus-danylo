using Entities.Models;

namespace Entities.Interfaces
{
    public interface IPartnerRepository
    {
        Task AddAsync(Partner partner);
        Task<Partner?> GetByIdAsync(int partnerId);
        Task<IEnumerable<Partner>> GetAllAsync();
        Task<IEnumerable<Partner>> GetByNodeIdAsync(int nodeId);
        Task UpdateAsync(Partner partner);
        Task DeleteAsync(int partnerId);
        Task<bool> ExistsAsync(int partnerId);
    }
}
