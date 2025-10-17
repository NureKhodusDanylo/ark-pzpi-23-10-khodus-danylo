using Entities.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class PartnerRepository : GenericRepository<Partner>, IPartnerRepository
    {
        private readonly MyDbContext _context;

        public PartnerRepository(MyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Partner?> GetByIdAsync(int partnerId)
        {
            return await _context.Partners
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.Id == partnerId);
        }

        public async Task<IEnumerable<Partner>> GetAllAsync()
        {
            return await _context.Partners
                .Include(p => p.Location)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Partner>> GetByNodeIdAsync(int nodeId)
        {
            return await _context.Partners
                .Include(p => p.Location)
                .Where(p => p.NodeId == nodeId)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task UpdateAsync(Partner partner)
        {
            _context.Partners.Update(partner);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int partnerId)
        {
            var partner = await _context.Partners.FindAsync(partnerId);
            if (partner != null)
            {
                _context.Partners.Remove(partner);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int partnerId)
        {
            return await _context.Partners.AnyAsync(p => p.Id == partnerId);
        }
    }
}
