using Application.Abstractions.Interfaces;
using Application.DTOs.RobotDTOs;
using Entities.Interfaces;
using Entities.Models;

namespace Application.Services
{
    public class RobotService : IRobotService
    {
        private readonly IRobotRepository _robotRepository;
        private readonly IPasswordHasher _passwordHasher;

        public RobotService(IRobotRepository robotRepository, IPasswordHasher passwordHasher)
        {
            _robotRepository = robotRepository;
            _passwordHasher = passwordHasher;
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

        public async Task<(bool Success, int? RobotId, string? ErrorMessage)> RegisterRobotAsync(RobotRegisterDTO registerDto)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(registerDto.Name))
                return (false, null, "Robot name is required");
            if (string.IsNullOrWhiteSpace(registerDto.Model))
                return (false, null, "Robot model is required");
            if (string.IsNullOrWhiteSpace(registerDto.SerialNumber))
                return (false, null, "Serial number is required");
            if (string.IsNullOrWhiteSpace(registerDto.AccessKey))
                return (false, null, "Access key is required");

            // Validate robot type
            if (!Enum.TryParse<RobotType>(registerDto.Type, true, out var robotType))
                return (false, null, "Invalid robot type. Must be 'GroundCourier' or 'Drone'");

            // Check if serial number already exists
            if (await _robotRepository.SerialNumberExistsAsync(registerDto.SerialNumber))
                return (false, null, "Robot with this serial number already exists");

            // Validate battery characteristics
            if (registerDto.BatteryCapacityJoules.HasValue && registerDto.BatteryCapacityJoules.Value <= 0)
                return (false, null, "Battery capacity must be greater than 0");
            if (registerDto.EnergyConsumptionPerMeterJoules.HasValue && registerDto.EnergyConsumptionPerMeterJoules.Value <= 0)
                return (false, null, "Energy consumption must be greater than 0");

            // Validate port range
            if (registerDto.Port.HasValue && (registerDto.Port.Value < 1 || registerDto.Port.Value > 65535))
                return (false, null, "Port must be between 1 and 65535");

            // Hash the access key
            var accessKeyHash = _passwordHasher.Hash(registerDto.AccessKey);

            // Create new robot entity
            var robot = new Robot
            {
                Name = registerDto.Name,
                Model = registerDto.Model,
                Type = robotType,
                Status = RobotStatus.Idle,
                BatteryLevel = 100.0,
                SerialNumber = registerDto.SerialNumber,
                AccessKeyHash = accessKeyHash,

                // Battery characteristics (use provided values or defaults)
                BatteryCapacityJoules = registerDto.BatteryCapacityJoules ?? 360000, // Default 100Wh
                EnergyConsumptionPerMeterJoules = registerDto.EnergyConsumptionPerMeterJoules ?? 36, // Default 36J/m (10km range)

                // IoT Connection
                IpAddress = registerDto.IpAddress,
                Port = registerDto.Port ?? 80, // Default HTTP port

                // Initial location
                CurrentNodeId = registerDto.CurrentNodeId
            };

            // Save to database
            await _robotRepository.AddAsync(robot);
            await _robotRepository.SaveChangesAsync();

            return (true, robot.Id, null);
        }

        public async Task<(bool Success, int? RobotId, string? ErrorMessage)> AuthenticateRobotAsync(RobotLoginDTO loginDto)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(loginDto.SerialNumber))
                return (false, null, "Serial number is required");
            if (string.IsNullOrWhiteSpace(loginDto.AccessKey))
                return (false, null, "Access key is required");

            // Get robot by SerialNumber
            var robot = await _robotRepository.GetBySerialNumberAsync(loginDto.SerialNumber);

            if (robot == null)
                return (false, null, "Invalid serial number or access key");

            if (string.IsNullOrEmpty(robot.AccessKeyHash))
                return (false, null, "Robot authentication not configured");

            // Verify access key
            var hashedAccessKey = _passwordHasher.Hash(loginDto.AccessKey);
            if (robot.AccessKeyHash != hashedAccessKey)
                return (false, null, "Invalid serial number or access key");

            return (true, robot.Id, null);
        }

        public async Task<RobotResponseDTO?> UpdateRobotStatusAsync(int robotId, RobotStatusUpdateDTO statusUpdate)
        {
            var robot = await _robotRepository.GetByIdAsync(robotId);
            if (robot == null)
            {
                return null;
            }

            // Validate and parse status
            if (!Enum.TryParse<RobotStatus>(statusUpdate.Status, true, out var newStatus))
            {
                throw new ArgumentException("Invalid status. Must be: Idle, Delivering, Charging, or Maintenance");
            }

            // Update robot status and location
            robot.Status = newStatus;
            robot.BatteryLevel = statusUpdate.BatteryLevel;
            robot.CurrentNodeId = statusUpdate.CurrentNodeId;
            robot.CurrentLatitude = statusUpdate.CurrentLatitude;
            robot.CurrentLongitude = statusUpdate.CurrentLongitude;
            robot.TargetNodeId = statusUpdate.TargetNodeId;

            await _robotRepository.UpdateAsync(robot);

            return MapToResponseDTO(robot);
        }

        private static RobotResponseDTO MapToResponseDTO(Robot robot)
        {
            return new RobotResponseDTO
            {
                Id = robot.Id,
                Name = robot.Name,
                Model = robot.Model,
                SerialNumber = robot.SerialNumber,
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
