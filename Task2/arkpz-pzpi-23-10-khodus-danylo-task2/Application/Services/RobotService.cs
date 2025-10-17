using Application.Abstractions.Interfaces;
using Application.DTOs.RobotDTOs;
using Entities.Interfaces;
using Entities.Models;

namespace Application.Services
{
    public class RobotService : IRobotService
    {
        private readonly IRobotRepository _robotRepository;

        public RobotService(IRobotRepository robotRepository)
        {
            _robotRepository = robotRepository;
        }

        public async Task<RobotResponseDTO> CreateRobotAsync(CreateRobotDTO robotDto)
        {
            if (string.IsNullOrWhiteSpace(robotDto.Name))
                throw new ArgumentException("Robot name is required");
            if (string.IsNullOrWhiteSpace(robotDto.Model))
                throw new ArgumentException("Robot model is required");

            var robot = new Robot
            {
                Name = robotDto.Name,
                Model = robotDto.Model,
                Type = robotDto.Type,
                Status = RobotStatus.Idle,
                BatteryLevel = 100,
                CurrentNodeId = robotDto.CurrentNodeId
            };

            await _robotRepository.AddAsync(robot);
            var created = await _robotRepository.GetByIdAsync(robot.Id);
            return MapToResponseDTO(created!);
        }

        public async Task<RobotResponseDTO?> GetRobotByIdAsync(int robotId)
        {
            var robot = await _robotRepository.GetByIdAsync(robotId);
            return robot == null ? null : MapToResponseDTO(robot);
        }

        public async Task<IEnumerable<RobotResponseDTO>> GetAllRobotsAsync()
        {
            var robots = await _robotRepository.GetAllAsync();
            return robots.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<RobotResponseDTO>> GetByStatusAsync(RobotStatus status)
        {
            var robots = await _robotRepository.GetByStatusAsync(status);
            return robots.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<RobotResponseDTO>> GetByTypeAsync(RobotType type)
        {
            var robots = await _robotRepository.GetByTypeAsync(type);
            return robots.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<RobotResponseDTO>> GetAvailableRobotsAsync()
        {
            var robots = await _robotRepository.GetAvailableRobotsAsync();
            return robots.Select(MapToResponseDTO);
        }

        public async Task<bool> UpdateRobotAsync(UpdateRobotDTO robotDto)
        {
            var robot = await _robotRepository.GetByIdAsync(robotDto.Id);
            if (robot == null) return false;

            if (string.IsNullOrWhiteSpace(robotDto.Name))
                throw new ArgumentException("Robot name is required");
            if (robotDto.BatteryLevel < 0 || robotDto.BatteryLevel > 100)
                throw new ArgumentException("Battery level must be between 0 and 100");

            robot.Name = robotDto.Name;
            robot.Model = robotDto.Model;
            robot.Type = robotDto.Type;
            robot.Status = robotDto.Status;
            robot.BatteryLevel = robotDto.BatteryLevel;
            robot.CurrentNodeId = robotDto.CurrentNodeId;

            await _robotRepository.UpdateAsync(robot);
            return true;
        }

        public async Task<bool> DeleteRobotAsync(int robotId)
        {
            if (!await _robotRepository.ExistsAsync(robotId)) return false;
            await _robotRepository.DeleteAsync(robotId);
            return true;
        }

        private static RobotResponseDTO MapToResponseDTO(Robot robot)
        {
            return new RobotResponseDTO
            {
                Id = robot.Id,
                Name = robot.Name,
                Model = robot.Model,
                Type = robot.Type,
                TypeName = robot.Type.ToString(),
                Status = robot.Status,
                StatusName = robot.Status.ToString(),
                BatteryLevel = robot.BatteryLevel,
                CurrentNodeId = robot.CurrentNodeId,
                CurrentNodeName = robot.CurrentNode?.Name,
                ActiveOrdersCount = robot.ActiveOrders?.Count ?? 0
            };
        }
    }
}
