using Application.DTOs.UserDTOs;
using Application.DTOs.NodeDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDTO?> GetUserByIdAsync(int userId);
        Task<IEnumerable<UserProfileDTO>> GetAllUsersAsync();
        Task<IEnumerable<UserSearchResultDTO>> SearchUsersAsync(string query, int currentUserId);
        Task<UserProfileDTO?> GetProfileAsync(int userId);
        Task<NodeResponseDTO?> GetMyNodeAsync(int userId);
        Task<NodeResponseDTO> UpdateMyNodeAsync(int userId, UpdateMyNodeDTO updateDto);
        Task<UserProfileDTO> UpdateProfileAsync(int userId, UpdateUserProfileDTO updateDto);
    }
}
