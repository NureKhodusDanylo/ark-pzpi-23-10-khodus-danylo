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
        private readonly IOrderRepository _orderRepository;
        private readonly INodeRepository _nodeRepository;

        public RobotService(
            IRobotRepository robotRepository,
            IPasswordHasher passwordHasher,
            IOrderRepository orderRepository,
            INodeRepository nodeRepository)
        {
            _robotRepository = robotRepository;
            _passwordHasher = passwordHasher;
            _orderRepository = orderRepository;
            _nodeRepository = nodeRepository;
        }

        public async Task<RobotResponseDTO> CreateRobotAsync(CreateRobotDTO robotDto)
        {
            if (string.IsNullOrWhiteSpace(robotDto.Name))
                throw new ArgumentException("Robot name is required");
            if (string.IsNullOrWhiteSpace(robotDto.Model))
                throw new ArgumentException("Robot model is required");

            // Validate CurrentNodeId if provided
            if (robotDto.CurrentNodeId.HasValue)
            {
                var nodeExists = await _nodeRepository.ExistsAsync(robotDto.CurrentNodeId.Value);
                if (!nodeExists)
                    throw new ArgumentException($"Node with ID {robotDto.CurrentNodeId.Value} does not exist");
            }

            var robot = new Robot
            {
                Name = robotDto.Name,
                Model = robotDto.Model,
                Type = robotDto.Type,
                Status = RobotStatus.Charging,
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

            // Validate CurrentNodeId if provided
            if (robotDto.CurrentNodeId.HasValue)
            {
                var nodeExists = await _nodeRepository.ExistsAsync(robotDto.CurrentNodeId.Value);
                if (!nodeExists)
                    throw new ArgumentException($"Node with ID {robotDto.CurrentNodeId.Value} does not exist");
            }

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

            // Validate CurrentNodeId if provided
            if (registerDto.CurrentNodeId.HasValue)
            {
                var nodeExists = await _nodeRepository.ExistsAsync(registerDto.CurrentNodeId.Value);
                if (!nodeExists)
                    return (false, null, $"Node with ID {registerDto.CurrentNodeId.Value} does not exist");
            }

            // Hash the access key
            var accessKeyHash = _passwordHasher.Hash(registerDto.AccessKey);

            // Create new robot entity
            var robot = new Robot
            {
                Name = registerDto.Name,
                Model = registerDto.Model,
                Type = robotType,
                Status = RobotStatus.Charging,
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

            // Validate CurrentNodeId if provided
            if (statusUpdate.CurrentNodeId.HasValue)
            {
                var nodeExists = await _nodeRepository.ExistsAsync(statusUpdate.CurrentNodeId.Value);
                if (!nodeExists)
                    throw new ArgumentException($"Node with ID {statusUpdate.CurrentNodeId.Value} does not exist");
            }

            // Validate TargetNodeId if provided
            if (statusUpdate.TargetNodeId.HasValue)
            {
                var nodeExists = await _nodeRepository.ExistsAsync(statusUpdate.TargetNodeId.Value);
                if (!nodeExists)
                    throw new ArgumentException($"Target node with ID {statusUpdate.TargetNodeId.Value} does not exist");
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

        // IoT Order Management

        public async Task<List<OrderAssignmentDTO>> GetMyOrdersAsync(int robotId)
        {
            // Verify robot exists
            var robot = await _robotRepository.GetByIdAsync(robotId);
            if (robot == null)
            {
                throw new ArgumentException($"Robot with ID {robotId} not found");
            }

            // Get all orders assigned to this robot that are not completed or cancelled
            var orders = robot.ActiveOrders?.Where(o =>
                o.Status != OrderStatus.Delivered &&
                o.Status != OrderStatus.Cancelled
            ).ToList() ?? new List<Order>();

            var assignments = new List<OrderAssignmentDTO>();

            foreach (var order in orders)
            {
                // Get nodes
                var pickupNode = await _nodeRepository.GetByIdAsync(order.PickupNodeId);
                var dropoffNode = await _nodeRepository.GetByIdAsync(order.DropoffNodeId);

                if (pickupNode == null || dropoffNode == null)
                    continue;

                // Build route (simplified - in real implementation, fetch from order metadata or recalculate)
                var route = new List<RouteWaypointDTO>
                {
                    new RouteWaypointDTO
                    {
                        SequenceNumber = 1,
                        Latitude = pickupNode.Latitude,
                        Longitude = pickupNode.Longitude,
                        Action = "travel",
                        DistanceMeters = 0
                    },
                    new RouteWaypointDTO
                    {
                        SequenceNumber = 2,
                        Latitude = dropoffNode.Latitude,
                        Longitude = dropoffNode.Longitude,
                        Action = "deliver",
                        DistanceMeters = CalculateDistance(
                            pickupNode.Latitude, pickupNode.Longitude,
                            dropoffNode.Latitude, dropoffNode.Longitude
                        )
                    }
                };

                assignments.Add(new OrderAssignmentDTO
                {
                    OrderId = order.Id,
                    OrderName = order.Name,
                    Description = order.Description ?? string.Empty,
                    Weight = order.Weight,
                    PickupNodeId = pickupNode.Id,
                    PickupNodeName = pickupNode.Name,
                    PickupLatitude = pickupNode.Latitude,
                    PickupLongitude = pickupNode.Longitude,
                    DropoffNodeId = dropoffNode.Id,
                    DropoffNodeName = dropoffNode.Name,
                    DropoffLatitude = dropoffNode.Latitude,
                    DropoffLongitude = dropoffNode.Longitude,
                    Route = route,
                    TotalDistanceMeters = route.Sum(r => r.DistanceMeters),
                    EstimatedBatteryUsagePercent = CalculateBatteryUsage(robot, route.Sum(r => r.DistanceMeters), order.Weight),
                    OrderStatus = order.Status.ToString(),
                    AssignedAt = order.CreatedAt
                });
            }

            return assignments;
        }

        public async Task<AcceptOrderResponseDTO> AcceptOrderAsync(int robotId, int orderId)
        {
            // Verify robot exists
            var robot = await _robotRepository.GetByIdAsync(robotId);
            if (robot == null)
            {
                throw new ArgumentException($"Robot with ID {robotId} not found");
            }

            // Get order
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found");
            }

            // Verify order is assigned to this robot
            if (order.RobotId != robotId)
            {
                throw new InvalidOperationException($"This order is not assigned to this robot. Order.RobotId={order.RobotId}, Expected={robotId}");
            }

            // Verify order status
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
            {
                throw new InvalidOperationException($"Cannot accept order with status {order.Status}");
            }

            // If order is already in Processing and robot is already Delivering, just return success
            // This handles the case where the order was already assigned via AssignRobotToOrderAsync
            bool alreadyAccepted = order.Status == OrderStatus.Processing && robot.Status == RobotStatus.Delivering;

            // Update order status to Processing only if it was Pending
            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Processing;
                await _orderRepository.UpdateAsync(order);
            }

            // Update robot status to Delivering only if it's not already
            if (robot.Status != RobotStatus.Delivering)
            {
                robot.Status = RobotStatus.Delivering;
                await _robotRepository.UpdateAsync(robot);
            }

            return new AcceptOrderResponseDTO
            {
                Message = alreadyAccepted ? "Order already accepted and in progress" : "Order accepted successfully",
                OrderId = orderId,
                OrderStatus = order.Status.ToString(),
                AcceptedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> UpdateOrderPhaseAsync(int robotId, int orderId, OrderPhaseUpdateDTO phaseUpdate)
        {
            // Verify robot exists
            var robot = await _robotRepository.GetByIdAsync(robotId);
            if (robot == null)
            {
                throw new ArgumentException($"Robot with ID {robotId} not found");
            }

            // Get order
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found");
            }

            // Verify order is assigned to this robot
            if (order.RobotId != robotId)
            {
                throw new InvalidOperationException("This order is not assigned to this robot");
            }

            // Update order status based on phase
            switch (phaseUpdate.Phase.ToUpper())
            {
                case "FLIGHT_TO_PICKUP":
                case "AT_PICKUP":
                case "LOADING":
                    order.Status = OrderStatus.Processing;
                    break;

                case "FLIGHT_TO_DROPOFF":
                case "AT_DROPOFF":
                case "UNLOADING":
                    order.Status = OrderStatus.EnRoute;
                    break;

                case "PACKAGE_DELIVERED":
                    order.Status = OrderStatus.Delivered;
                    robot.Status = RobotStatus.Idle;
                    await _robotRepository.UpdateAsync(robot);
                    break;

                case "FLIGHT_TO_CHARGING":
                    robot.Status = RobotStatus.Charging;
                    await _robotRepository.UpdateAsync(robot);
                    break;

                default:
                    throw new ArgumentException($"Unknown phase: {phaseUpdate.Phase}");
            }

            await _orderRepository.UpdateAsync(order);

            return true;
        }

        // Helper methods

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula
            const double R = 6371000; // Earth radius in meters
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private static double CalculateBatteryUsage(Robot robot, double distanceMeters, double packageWeight)
        {
            // Calculate energy consumption
            var baseConsumption = robot.EnergyConsumptionPerMeterJoules * distanceMeters;

            // Add weight penalty (10% more consumption per kg)
            var weightPenalty = 1.0 + (packageWeight * 0.1);
            var totalEnergyConsumed = baseConsumption * weightPenalty;

            // Convert to percentage
            var batteryUsagePercent = (totalEnergyConsumed / robot.BatteryCapacityJoules) * 100.0;

            return Math.Round(batteryUsagePercent, 2);
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
