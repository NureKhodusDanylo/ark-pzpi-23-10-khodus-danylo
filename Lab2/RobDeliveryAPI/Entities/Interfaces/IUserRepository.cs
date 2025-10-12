using Entities.Models;
using System.Threading.Tasks;

namespace Application.Abstractions.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByGoogleIdAsync(string googleId);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> IsPasswordValidByEmailAsync(string email, string passwordHash);
        Task AddAsync(User user);
    }
}