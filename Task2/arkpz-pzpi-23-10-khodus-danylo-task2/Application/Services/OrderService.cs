using Application.Abstractions.Interfaces;
using Application.DTOs.OrderDTOs;
using Application.DTOs.FileDTOs;
using Entities.Interfaces;
using Entities.Models;

namespace Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRobotRepository _robotRepository;
        private readonly INodeRepository _nodeRepository;
        private readonly IDroneConnectionService _droneConnectionService;

        public OrderService(
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            IRobotRepository robotRepository,
            INodeRepository nodeRepository,
            IDroneConnectionService droneConnectionService)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _robotRepository = robotRepository;
            _nodeRepository = nodeRepository;
            _droneConnectionService = droneConnectionService;
        }

        public async Task<OrderResponseDTO> CreateOrderAsync(int senderId, CreateOrderDTO orderDto)
        {
            // Validate sender exists
            var sender = await _userRepository.GetByIdAsync(senderId);
            if (sender == null)
            {
                throw new ArgumentException("Sender not found");
            }

            // Validate recipient exists
            var recipient = await _userRepository.GetByIdAsync(orderDto.RecipientId);
            if (recipient == null)
            {
                throw new ArgumentException("Recipient not found");
            }

            // Validate that sender and recipient are different
            if (senderId == orderDto.RecipientId)
            {
                throw new ArgumentException("Sender and recipient cannot be the same user");
            }

            // Validate sender has personal node
            if (!sender.PersonalNodeId.HasValue)
            {
                throw new ArgumentException("Sender does not have a personal node configured");
            }

            // Validate recipient has personal node
            if (!recipient.PersonalNodeId.HasValue)
            {
                throw new ArgumentException("Recipient does not have a personal node configured");
            }

            // Validate weight and product price
            if (orderDto.Weight <= 0)
            {
                throw new ArgumentException("Weight must be greater than zero");
            }

            if (orderDto.ProductPrice < 0)
            {
                throw new ArgumentException("Product price cannot be negative");
            }

            // Calculate delivery price based on weight
            // Formula: Base price (50) + weight-based price (10 per kg)
            decimal deliveryPrice = CalculateDeliveryPrice(orderDto.Weight);

            // Create new order
            var order = new Order
            {
                Name = orderDto.Name,
                Description = orderDto.Description,
                Weight = orderDto.Weight,
                DeliveryPrice = deliveryPrice,
                ProductPrice = orderDto.ProductPrice,
                IsProductPaid = orderDto.IsProductPaid,
                DeliveryPayer = orderDto.DeliveryPayer,
                IsDeliveryPaid = false,
                Status = OrderStatus.Pending,
                SenderId = senderId,
                RecipientId = orderDto.RecipientId,
                PickupNodeId = sender.PersonalNodeId.Value,
                DropoffNodeId = recipient.PersonalNodeId.Value,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddAsync(order);

            // Retrieve the created order with all related data
            var createdOrder = await _orderRepository.GetByIdAsync(order.Id);

            return MapToResponseDTO(createdOrder!);
        }

        private static decimal CalculateDeliveryPrice(double weight)
        {
            // Base delivery price
            const decimal basePrice = 50m;

            // Price per kilogram
            const decimal pricePerKg = 10m;

            // Calculate total delivery price
            decimal totalPrice = basePrice + (decimal)weight * pricePerKg;

            return Math.Round(totalPrice, 2);
        }

        public async Task<OrderResponseDTO?> GetOrderByIdAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            return order == null ? null : MapToResponseDTO(order);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetUserOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetByUserIdAsync(userId);
            return orders.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetSentOrdersAsync(int senderId)
        {
            var orders = await _orderRepository.GetBySenderIdAsync(senderId);
            return orders.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetReceivedOrdersAsync(int recipientId)
        {
            var orders = await _orderRepository.GetByRecipientIdAsync(recipientId);
            return orders.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetOrdersByStatusAsync(OrderStatus status)
        {
            var orders = await _orderRepository.GetByStatusAsync(status);
            return orders.Select(MapToResponseDTO);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return false;
            }

            // Validate status transition
            if (!IsValidStatusTransition(order.Status, newStatus))
            {
                throw new InvalidOperationException($"Invalid status transition from {order.Status} to {newStatus}");
            }

            order.Status = newStatus;

            // Set completion time if order is delivered or cancelled
            if (newStatus == OrderStatus.Delivered || newStatus == OrderStatus.Cancelled)
            {
                order.CompletedAt = DateTime.UtcNow;
            }

            await _orderRepository.UpdateAsync(order);
            return true;
        }

        public async Task<bool> AssignRobotToOrderAsync(int orderId, int robotId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return false;
            }

            // Validate robot exists
            var robot = await _robotRepository.GetByIdAsync(robotId);
            if (robot == null)
            {
                throw new ArgumentException($"Robot with ID {robotId} does not exist");
            }

            // Check if order can be assigned
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
            {
                throw new InvalidOperationException($"Cannot assign robot to order with status {order.Status}");
            }

            // Assign robot to order
            order.RobotId = robotId;

            // Update order status to Processing if it was Pending
            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Processing;
            }

            // Update robot status to Delivering
            robot.Status = RobotStatus.Delivering;
            robot.CurrentNodeId = order.PickupNodeId;

            await _orderRepository.UpdateAsync(order);
            await _robotRepository.UpdateAsync(robot);

            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return false;
            }

            // Only sender can cancel the order and only if it's not already in progress
            if (order.SenderId != userId)
            {
                throw new UnauthorizedAccessException("Only the sender can cancel the order");
            }

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
            {
                throw new InvalidOperationException("Cannot cancel order that is already en route or completed");
            }

            // If robot was assigned, set it back to Idle
            if (order.RobotId.HasValue)
            {
                var robot = await _robotRepository.GetByIdAsync(order.RobotId.Value);
                if (robot != null)
                {
                    robot.Status = RobotStatus.Idle;
                    await _robotRepository.UpdateAsync(robot);
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.CompletedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            return true;
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            if (!await _orderRepository.ExistsAsync(orderId))
            {
                return false;
            }

            await _orderRepository.DeleteAsync(orderId);
            return true;
        }

        private static OrderResponseDTO MapToResponseDTO(Order order)
        {
            return new OrderResponseDTO
            {
                Id = order.Id,
                Name = order.Name,
                Description = order.Description,
                Weight = order.Weight,
                DeliveryPrice = order.DeliveryPrice,
                ProductPrice = order.ProductPrice,
                IsProductPaid = order.IsProductPaid,
                DeliveryPayer = order.DeliveryPayer,
                DeliveryPayerName = order.DeliveryPayer.ToString(),
                IsDeliveryPaid = order.IsDeliveryPaid,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                CompletedAt = order.CompletedAt,
                SenderId = order.SenderId,
                SenderName = order.Sender?.UserName ?? "Unknown",
                RecipientId = order.RecipientId,
                RecipientName = order.Recipient?.UserName ?? "Unknown",
                RobotId = order.RobotId,
                RobotName = order.AssignedRobot?.Name,
                PickupNodeId = order.PickupNodeId,
                PickupNodeName = order.PickupNode?.Name ?? "Unknown",
                DropoffNodeId = order.DropoffNodeId,
                DropoffNodeName = order.DropoffNode?.Name ?? "Unknown",
                Images = order.Images?.Select(img => new FileResponseDTO
                {
                    Id = img.Id,
                    FileName = img.FileName,
                    FilePath = img.FilePath,
                    ContentType = img.ContentType,
                    FileSize = img.FileSize,
                    UploadedAt = img.UploadedAt
                }).ToList()
            };
        }

        private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            // Define valid status transitions
            return currentStatus switch
            {
                OrderStatus.Pending => newStatus == OrderStatus.Processing || newStatus == OrderStatus.Cancelled,
                OrderStatus.Processing => newStatus == OrderStatus.EnRoute || newStatus == OrderStatus.Cancelled,
                OrderStatus.EnRoute => newStatus == OrderStatus.Delivered || newStatus == OrderStatus.Cancelled,
                OrderStatus.Delivered => false, // Cannot change from delivered
                OrderStatus.Cancelled => false, // Cannot change from cancelled
                _ => false
            };
        }

        public async Task<ExecuteOrderResponseDTO> ExecuteOrderAsync(int orderId, int userId)
        {
            // Get the order
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found");
            }

            if ( !(await _orderRepository.DoesItBelong(orderId, userId)))
            {
                throw new InvalidOperationException($"Cannot execute order. Only sender's orders can be executed.");
            }

            // Validate order status
            if (order.Status != OrderStatus.Pending)
            {
                throw new InvalidOperationException($"Cannot execute order with status {order.Status}. Only Pending orders can be executed.");
            }

            // Get pickup and dropoff nodes
            var pickupNode = await _nodeRepository.GetByIdAsync(order.PickupNodeId);
            var dropoffNode = await _nodeRepository.GetByIdAsync(order.DropoffNodeId);

            if (pickupNode == null || dropoffNode == null)
            {
                throw new InvalidOperationException("Pickup or dropoff node not found");
            }

            // Get all drones on charging stations
            var chargingDrones = await _robotRepository.GetByTypeAndStatusAsync(RobotType.Drone, RobotStatus.Charging);
            var chargingDronesList = chargingDrones.ToList();

            if (!chargingDronesList.Any())
            {
                throw new InvalidOperationException("No drones available on charging stations");
            }

            // Find optimal drone and route
            Robot? optimalDrone = null;
            List<RouteSegmentDTO> optimalRoute = null;
            double minTotalDistance = double.MaxValue;
            double optimalBatteryUsage = 0;

            foreach (var drone in chargingDronesList)
            {
                if (drone.CurrentNode == null)
                {
                    continue; // Skip drones without current location
                }

                var (canComplete, route, totalDistance, batteryUsage) = await CalculateDroneRoute(
                    drone,
                    drone.CurrentNode,
                    pickupNode,
                    dropoffNode,
                    order.Weight
                );

                if (canComplete && totalDistance < minTotalDistance)
                {
                    optimalDrone = drone;
                    optimalRoute = route;
                    minTotalDistance = totalDistance;
                    optimalBatteryUsage = batteryUsage;
                }
            }

            if (optimalDrone == null || optimalRoute == null)
            {
                throw new InvalidOperationException("No suitable drone found that can complete this delivery");
            }

            // Assign the optimal drone to the order
            await AssignRobotToOrderAsync(orderId, optimalDrone.Id);

            // Send command to drone if IP address is configured
            string droneMessage = "Order successfully assigned to optimal drone";
            if (!string.IsNullOrEmpty(optimalDrone.IpAddress) && optimalDrone.Port.HasValue)
            {
                try
                {
                    // Build waypoints from route for Arduino
                    var waypoints = BuildWaypointsFromRoute(optimalRoute, pickupNode, dropoffNode);

                    // Create command packet for Arduino
                    var droneCommand = new Application.DTOs.RobotDTOs.DroneCommandDTO
                    {
                        OrderId = orderId,
                        OrderName = order.Name,
                        PackageWeight = order.Weight,
                        PickupNodeId = pickupNode.Id,
                        PickupNodeName = pickupNode.Name,
                        PickupLatitude = pickupNode.Latitude,
                        PickupLongitude = pickupNode.Longitude,
                        DropoffNodeId = dropoffNode.Id,
                        DropoffNodeName = dropoffNode.Name,
                        DropoffLatitude = dropoffNode.Latitude,
                        DropoffLongitude = dropoffNode.Longitude,
                        Route = waypoints,
                        TotalDistanceMeters = minTotalDistance,
                        EstimatedBatteryUsagePercent = optimalBatteryUsage,
                        CommandTimestamp = DateTime.UtcNow
                    };

                    // Send command to Arduino
                    var droneResponse = await _droneConnectionService.SendDeliveryCommandAsync(
                        optimalDrone.IpAddress,
                        optimalDrone.Port.Value,
                        droneCommand
                    );

                    if (droneResponse.Success)
                    {
                        droneMessage = $"Order assigned and command sent to drone. Drone response: {droneResponse.Message}";
                    }
                    else
                    {
                        droneMessage = $"Order assigned but drone communication failed: {droneResponse.Message}";
                    }
                }
                catch (Exception ex)
                {
                    droneMessage = $"Order assigned but failed to communicate with drone: {ex.Message}";
                }
            }

            return new ExecuteOrderResponseDTO
            {
                OrderId = orderId,
                AssignedRobotId = optimalDrone.Id,
                AssignedRobotName = optimalDrone.Name,
                Message = droneMessage,
                Route = optimalRoute,
                TotalDistanceMeters = minTotalDistance,
                EstimatedBatteryUsagePercent = optimalBatteryUsage
            };
        }

        private List<Application.DTOs.RobotDTOs.RouteWaypointDTO> BuildWaypointsFromRoute(
            List<RouteSegmentDTO> route,
            Node pickupNode,
            Node dropoffNode)
        {
            var waypoints = new List<Application.DTOs.RobotDTOs.RouteWaypointDTO>();
            var nodeCache = new Dictionary<string, (double lat, double lon)>
            {
                [pickupNode.Name] = (pickupNode.Latitude, pickupNode.Longitude),
                [dropoffNode.Name] = (dropoffNode.Latitude, dropoffNode.Longitude)
            };

            // Build waypoints from route segments
            foreach (var segment in route)
            {
                // Get coordinates for destination
                if (!nodeCache.ContainsKey(segment.ToNodeName))
                {
                    // For charging stations, fetch from database (simplified - in production cache all nodes)
                    var node = _nodeRepository.GetByTypeAsync(NodeType.ChargingStation).Result
                        .FirstOrDefault(n => n.Name == segment.ToNodeName);
                    if (node != null)
                    {
                        nodeCache[node.Name] = (node.Latitude, node.Longitude);
                    }
                }

                if (nodeCache.TryGetValue(segment.ToNodeName, out var coords))
                {
                    waypoints.Add(new Application.DTOs.RobotDTOs.RouteWaypointDTO
                    {
                        SequenceNumber = segment.SegmentNumber,
                        Latitude = coords.lat,
                        Longitude = coords.lon,
                        Action = segment.Action.ToLower(),
                        DistanceMeters = segment.DistanceMeters
                    });
                }
            }

            return waypoints;
        }

        private async Task<(bool canComplete, List<RouteSegmentDTO> route, double totalDistance, double batteryUsage)> CalculateDroneRoute(
            Robot drone,
            Node currentNode,
            Node pickupNode,
            Node dropoffNode,
            double packageWeight)
        {
            var route = new List<RouteSegmentDTO>();
            double totalDistance = 0;
            int segmentNumber = 1;

            // Calculate meters per 1% battery WITHOUT package weight (for empty flight to pickup)
            double metersPerBatteryPercentEmpty = drone.MaxFlightRangeMeters / 100.0;

            // Step 1: Calculate route from current position to pickup (without package)
            var (canReachPickup, routeToPickup, distanceToPickup, batteryToPickup) = await CalculateRouteSegmentWithCharging(
                currentNode,
                pickupNode,
                drone.BatteryLevel,
                metersPerBatteryPercentEmpty,
                segmentNumber,
                "Travel"
            );

            if (!canReachPickup)
            {
                return (false, new List<RouteSegmentDTO>(), 0, 0);
            }

            route.AddRange(routeToPickup);
            totalDistance += distanceToPickup;
            segmentNumber += routeToPickup.Count;

            // Pickup package action
            route.Add(new RouteSegmentDTO
            {
                SegmentNumber = segmentNumber++,
                FromNodeName = pickupNode.Name,
                ToNodeName = pickupNode.Name,
                DistanceMeters = 0,
                Action = "PickupPackage"
            });

            // Calculate remaining battery after reaching pickup
            double remainingBatteryAfterPickup = drone.BatteryLevel - batteryToPickup;
            // If we charged during route, battery is 100% minus last segment usage
            if (routeToPickup.Any(r => r.Action == "Charge"))
            {
                var lastChargeIndex = routeToPickup.FindLastIndex(r => r.Action == "Charge");
                double distanceAfterLastCharge = routeToPickup.Skip(lastChargeIndex + 1).Sum(r => r.DistanceMeters);
                remainingBatteryAfterPickup = 100 - (distanceAfterLastCharge / metersPerBatteryPercentEmpty);
            }

            // Step 2: Calculate route from pickup to dropoff WITH package weight
            // Heavier packages reduce flight range: 1% reduction per kg, minimum 50% efficiency
            double weightFactor = 1.0 - (packageWeight * 0.01);
            weightFactor = Math.Max(0.5, weightFactor);
            double metersPerBatteryPercentWithLoad = (drone.MaxFlightRangeMeters / 100.0) * weightFactor;

            var (canReachDropoff, routeToDropoff, distanceToDropoff, batteryToDropoff) = await CalculateRouteSegmentWithCharging(
                pickupNode,
                dropoffNode,
                remainingBatteryAfterPickup,
                metersPerBatteryPercentWithLoad,
                segmentNumber,
                "Travel"
            );

            if (!canReachDropoff)
            {
                return (false, new List<RouteSegmentDTO>(), 0, 0);
            }

            route.AddRange(routeToDropoff);
            totalDistance += distanceToDropoff;
            segmentNumber += routeToDropoff.Count;

            // Delivery action
            route.Add(new RouteSegmentDTO
            {
                SegmentNumber = segmentNumber++,
                FromNodeName = dropoffNode.Name,
                ToNodeName = dropoffNode.Name,
                DistanceMeters = 0,
                Action = "DeliverPackage"
            });

            double totalBatteryUsage = batteryToPickup + batteryToDropoff;
            return (true, route, totalDistance, totalBatteryUsage);
        }

        private async Task<(bool canComplete, List<RouteSegmentDTO> route, double distance, double batteryUsed)> CalculateRouteSegmentWithCharging(
            Node fromNode,
            Node toNode,
            double currentBattery,
            double metersPerBatteryPercent,
            int startSegmentNumber,
            string travelAction)
        {
            var route = new List<RouteSegmentDTO>();
            double totalDistance = 0;
            double totalBatteryUsed = 0;
            int segmentNumber = startSegmentNumber;

            double distanceToDestination = CalculateDistance(fromNode, toNode);
            double requiredBattery = distanceToDestination / metersPerBatteryPercent;

            // Check if can reach destination directly with safety margin
            if (requiredBattery <= currentBattery)
            {
                // SAFETY CHECK: Verify drone can reach a charging station from destination
                var nearestChargeFromDestination = await _nodeRepository.FindNearestNodeAsync(
                    toNode.Latitude,
                    toNode.Longitude,
                    NodeType.ChargingStation
                );

                if (nearestChargeFromDestination != null)
                {
                    double distanceToNearestCharge = CalculateDistance(toNode, nearestChargeFromDestination);
                    double batteryNeededForSafety = distanceToNearestCharge / metersPerBatteryPercent;
                    double remainingBatteryAtDestination = currentBattery - requiredBattery;

                    // If remaining battery is insufficient to reach charging station, need intermediate charge
                    if (remainingBatteryAtDestination < batteryNeededForSafety)
                    {
                        // Cannot go direct - need to charge en route
                        // Continue to charging station logic below
                    }
                    else
                    {
                        // Safe to fly direct
                        route.Add(new RouteSegmentDTO
                        {
                            SegmentNumber = segmentNumber++,
                            FromNodeName = fromNode.Name,
                            ToNodeName = toNode.Name,
                            DistanceMeters = distanceToDestination,
                            Action = travelAction
                        });

                        return (true, route, distanceToDestination, requiredBattery);
                    }
                }
                else
                {
                    // No charging stations available - risky but allow if battery sufficient
                    // This is edge case for systems without charging infrastructure
                    route.Add(new RouteSegmentDTO
                    {
                        SegmentNumber = segmentNumber++,
                        FromNodeName = fromNode.Name,
                        ToNodeName = toNode.Name,
                        DistanceMeters = distanceToDestination,
                        Action = travelAction
                    });

                    return (true, route, distanceToDestination, requiredBattery);
                }
            }

            // Need to charge en route - find optimal charging station
            // Get all charging stations
            var allChargingStations = await _nodeRepository.GetByTypeAsync(NodeType.ChargingStation);
            var chargingStationsList = allChargingStations.ToList();

            if (!chargingStationsList.Any())
            {
                return (false, new List<RouteSegmentDTO>(), 0, 0);
            }

            // Find optimal charging station:
            // 1. Must be reachable with current battery
            // 2. After reaching destination from this station, must have enough battery to reach another charging station (safety)
            // 3. Minimize total distance (from current position to station + station to destination)
            Node? optimalStation = null;
            double minTotalRouteDistance = double.MaxValue;

            foreach (var station in chargingStationsList)
            {
                double distanceToStation = CalculateDistance(fromNode, station);
                double batteryToStation = distanceToStation / metersPerBatteryPercent;

                // Check if we can reach this station with current battery
                if (batteryToStation <= currentBattery)
                {
                    double distanceFromStationToDestination = CalculateDistance(station, toNode);
                    double batteryFromStationToDestination = distanceFromStationToDestination / metersPerBatteryPercent;

                    // After charging to 100% at this station, check if we can:
                    // 1. Reach destination
                    // 2. Have enough battery left to reach a charging station from destination
                    if (batteryFromStationToDestination <= 100)
                    {
                        // Find nearest charging station from destination
                        var chargeFromDestination = await _nodeRepository.FindNearestNodeAsync(
                            toNode.Latitude,
                            toNode.Longitude,
                            NodeType.ChargingStation
                        );

                        if (chargeFromDestination != null)
                        {
                            double distanceDestToCharge = CalculateDistance(toNode, chargeFromDestination);
                            double batteryDestToCharge = distanceDestToCharge / metersPerBatteryPercent;
                            double remainingBatteryAtDest = 100 - batteryFromStationToDestination;

                            // Verify drone will have enough battery to reach charging station from destination
                            if (remainingBatteryAtDest >= batteryDestToCharge)
                            {
                                double totalRouteDistance = distanceToStation + distanceFromStationToDestination;

                                // Select station with minimum total route distance
                                if (totalRouteDistance < minTotalRouteDistance)
                                {
                                    minTotalRouteDistance = totalRouteDistance;
                                    optimalStation = station;
                                }
                            }
                        }
                        else
                        {
                            // No charging stations from destination - accept this station if distance is acceptable
                            double totalRouteDistance = distanceToStation + distanceFromStationToDestination;
                            if (totalRouteDistance < minTotalRouteDistance)
                            {
                                minTotalRouteDistance = totalRouteDistance;
                                optimalStation = station;
                            }
                        }
                    }
                }
            }

            if (optimalStation == null)
            {
                return (false, new List<RouteSegmentDTO>(), 0, 0); // No suitable charging stations found
            }

            double distanceToOptimalStation = CalculateDistance(fromNode, optimalStation);
            double batteryToOptimalStation = distanceToOptimalStation / metersPerBatteryPercent;

            // Fly to optimal charging station
            route.Add(new RouteSegmentDTO
            {
                SegmentNumber = segmentNumber++,
                FromNodeName = fromNode.Name,
                ToNodeName = optimalStation.Name,
                DistanceMeters = distanceToOptimalStation,
                Action = travelAction
            });
            totalDistance += distanceToOptimalStation;
            totalBatteryUsed += batteryToOptimalStation;

            // Charge
            route.Add(new RouteSegmentDTO
            {
                SegmentNumber = segmentNumber++,
                FromNodeName = optimalStation.Name,
                ToNodeName = optimalStation.Name,
                DistanceMeters = 0,
                Action = "Charge"
            });

            // Recursively calculate from charging station to destination with full battery
            var (canComplete, remainingRoute, remainingDistance, remainingBattery) = await CalculateRouteSegmentWithCharging(
                optimalStation,
                toNode,
                100, // Full battery after charging
                metersPerBatteryPercent,
                segmentNumber,
                travelAction
            );

            if (!canComplete)
            {
                return (false, new List<RouteSegmentDTO>(), 0, 0);
            }

            route.AddRange(remainingRoute);
            totalDistance += remainingDistance;
            totalBatteryUsed += remainingBattery;

            return (true, route, totalDistance, totalBatteryUsed);
        }

        private static double CalculateDistance(Node node1, Node node2)
        {
            // Haversine formula for calculating distance between two points on Earth
            const double earthRadiusMeters = 6371000; // Earth's radius in meters

            double lat1Rad = DegreesToRadians(node1.Latitude);
            double lat2Rad = DegreesToRadians(node2.Latitude);
            double deltaLatRad = DegreesToRadians(node2.Latitude - node1.Latitude);
            double deltaLonRad = DegreesToRadians(node2.Longitude - node1.Longitude);

            double a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMeters * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
