using Application.DTOs.RobotDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IRobotService
    {
        Task<RobotResponseDTO> CreateRobotAsync(CreateRobotDTO robotDto);
        Task<RobotResponseDTO?> GetRobotByIdAsync(int robotId);
        Task<IEnumerable<RobotResponseDTO>> GetAllRobotsAsync();
        Task<IEnumerable<RobotResponseDTO>> GetByStatusAsync(RobotStatus status);
        Task<IEnumerable<RobotResponseDTO>> GetByTypeAsync(RobotType type);
        Task<IEnumerable<RobotResponseDTO>> GetAvailableRobotsAsync();
        Task<bool> UpdateRobotAsync(UpdateRobotDTO robotDto);
        Task<bool> DeleteRobotAsync(int robotId);

        // IoT Device Authentication
        Task<(bool Success, int? RobotId, string? ErrorMessage)> RegisterRobotAsync(RobotRegisterDTO registerDto);
        Task<(bool Success, int? RobotId, string? ErrorMessage)> AuthenticateRobotAsync(RobotLoginDTO loginDto);
    }
}
