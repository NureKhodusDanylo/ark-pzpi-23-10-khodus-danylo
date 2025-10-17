using Application.Abstractions.Interfaces;
using Application.DTOs.OrderDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Get authenticated user ID from JWT token
        /// </summary>
        private int GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst("Id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        /// <summary>
        /// Create a new order (uses authenticated user from JWT token)
        /// </summary>
        /// <param name="orderDto">Order details</param>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO orderDto)
        {
            try
            {
                int senderId = GetAuthenticatedUserId();
                var result = await _orderService.CreateOrderAsync(senderId, orderDto);
                return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the order", details = ex.Message });
            }
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the order", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all orders
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all orders for authenticated user (sent and received)
        /// </summary>
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                int userId = GetAuthenticatedUserId();
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return Ok(orders);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving user orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all orders for a specific user (sent and received) - Admin only
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserOrders(int userId)
        {
            try
            {
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving user orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get orders sent by a specific user
        /// </summary>
        [HttpGet("sent/{senderId}")]
        public async Task<IActionResult> GetSentOrders(int senderId)
        {
            try
            {
                var orders = await _orderService.GetSentOrdersAsync(senderId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving sent orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get orders received by a specific user
        /// </summary>
        [HttpGet("received/{recipientId}")]
        public async Task<IActionResult> GetReceivedOrders(int recipientId)
        {
            try
            {
                var orders = await _orderService.GetReceivedOrdersAsync(recipientId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving received orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get orders by status
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetOrdersByStatus(OrderStatus status)
        {
            try
            {
                var orders = await _orderService.GetOrdersByStatusAsync(status);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving orders by status", details = ex.Message });
            }
        }

        /// <summary>
        /// Update order status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDTO updateDto)
        {
            try
            {
                var result = await _orderService.UpdateOrderStatusAsync(id, updateDto.NewStatus);
                if (!result)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(new { message = "Order status updated successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating order status", details = ex.Message });
            }
        }

        /// <summary>
        /// Assign a robot to an order
        /// </summary>
        [HttpPost("{id}/assign-robot")]
        public async Task<IActionResult> AssignRobotToOrder(int id, [FromBody] AssignRobotDTO assignDto)
        {
            try
            {
                var result = await _orderService.AssignRobotToOrderAsync(id, assignDto.RobotId);
                if (!result)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(new { message = "Robot assigned successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while assigning robot", details = ex.Message });
            }
        }

        /// <summary>
        /// Cancel an order (only order sender can cancel)
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                int userId = GetAuthenticatedUserId();
                var result = await _orderService.CancelOrderAsync(id, userId);
                if (!result)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(new { message = "Order cancelled successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while cancelling the order", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete an order
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var result = await _orderService.DeleteOrderAsync(id);
                if (!result)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(new { message = "Order deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the order", details = ex.Message });
            }
        }
    }
}
