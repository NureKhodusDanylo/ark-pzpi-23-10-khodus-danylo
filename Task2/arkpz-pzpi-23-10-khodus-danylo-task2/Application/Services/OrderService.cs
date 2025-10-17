using Application.Abstractions.Interfaces;
using Application.DTOs.OrderDTOs;
using Entities.Interfaces;
using Entities.Models;

namespace Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRobotRepository _robotRepository;

        public OrderService(IOrderRepository orderRepository, IUserRepository userRepository, IRobotRepository robotRepository)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _robotRepository = robotRepository;
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

            // Validate weight and price
            if (orderDto.Weight <= 0)
            {
                throw new ArgumentException("Weight must be greater than zero");
            }

            if (orderDto.Price < 0)
            {
                throw new ArgumentException("Price cannot be negative");
            }

            // Create new order
            var order = new Order
            {
                Name = orderDto.Name,
                Description = orderDto.Description,
                Weight = orderDto.Weight,
                Price = orderDto.Price,
                Paid = false,
                Status = OrderStatus.Pending,
                SenderId = senderId,
                RecipientId = orderDto.RecipientId,
                PickupNodeId = orderDto.PickupNodeId,
                DropoffNodeId = orderDto.DropoffNodeId,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddAsync(order);

            // Retrieve the created order with all related data
            var createdOrder = await _orderRepository.GetByIdAsync(order.Id);

            return MapToResponseDTO(createdOrder!);
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

            // Check if robot is available
            if (robot.Status != RobotStatus.Idle)
            {
                throw new InvalidOperationException($"Robot {robot.Name} is not available (current status: {robot.Status})");
            }

            // Check if robot has sufficient battery
            if (robot.BatteryLevel < 20)
            {
                throw new InvalidOperationException($"Robot {robot.Name} has insufficient battery ({robot.BatteryLevel}%)");
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
                Price = order.Price,
                Paid = order.Paid,
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
                DropoffNodeName = order.DropoffNode?.Name ?? "Unknown"
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
    }
}
