//Контроллери:

using Application.Abstractions.Interfaces;
using Application.DTOs.AdminDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>
        /// Get system statistics for dashboard
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            try
            {
                var stats = await _adminService.GetSystemStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve system stats", details = ex.Message });
            }
        }

        /// <summary>
        /// Export delivery history as JSON
        /// </summary>
        [HttpGet("export/delivery-history")]
        public async Task<IActionResult> ExportDeliveryHistory()
        {
            try
            {
                var historyJson = await _adminService.ExportDeliveryHistoryAsync();
                var fileName = $"DeliveryHistory_{DateTime.Now:yyyyMMdd_HHmmss}.json";

                return File(
                    System.Text.Encoding.UTF8.GetBytes(historyJson),
                    "application/json",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to export delivery history", details = ex.Message });
            }
        }

        /// <summary>
        /// Create database backup
        /// </summary>
        [HttpPost("backup")]
        public async Task<IActionResult> CreateBackup([FromBody] BackupRequestDTO request)
        {
            try
            {
                string backupPath = request.BackupPath ?? "Backups";
                var success = await _adminService.CreateDatabaseBackupAsync(backupPath);

                if (success)
                {
                    return Ok(new { message = "Backup created successfully", path = backupPath });
                }
                else
                {
                    return StatusCode(500, new { error = "Failed to create backup" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create backup", details = ex.Message });
            }
        }

        /// <summary>
        /// Get robot efficiency analytics
        /// </summary>
        [HttpGet("analytics/robot-efficiency")]
        public async Task<IActionResult> GetRobotEfficiency()
        {
            try
            {
                var efficiency = await _adminService.GetRobotEfficiencyAsync();
                return Ok(efficiency);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve robot efficiency", details = ex.Message });
            }
        }

        /// <summary>
        /// Generate a new admin registration key
        /// </summary>
        [HttpPost("keys/generate")]
        public async Task<IActionResult> GenerateAdminKey([FromBody] CreateAdminKeyDTO request)
        {
            try
            {
                var userIdClaim = User.FindFirst("Id")?.Value;
                if (userIdClaim == null || !int.TryParse(userIdClaim, out int adminId))
                {
                    return Unauthorized(new { error = "Invalid token" });
                }

                var adminKey = await _adminService.GenerateAdminKeyAsync(
                    adminId,
                    request.ExpiresAt,
                    request.Description
                );

                return Ok(new
                {
                    message = "Admin key generated successfully",
                    key = adminKey
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate admin key", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all admin keys
        /// </summary>
        [HttpGet("keys")]
        public async Task<IActionResult> GetAllAdminKeys()
        {
            try
            {
                var keys = await _adminService.GetAllAdminKeysAsync();
                return Ok(keys);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve admin keys", details = ex.Message });
            }
        }

        /// <summary>
        /// Get unused admin keys
        /// </summary>
        [HttpGet("keys/unused")]
        public async Task<IActionResult> GetUnusedAdminKeys()
        {
            try
            {
                var keys = await _adminService.GetUnusedAdminKeysAsync();
                return Ok(keys);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve unused admin keys", details = ex.Message });
            }
        }

        /// <summary>
        /// Revoke an admin key
        /// </summary>
        [HttpPost("keys/{keyId}/revoke")]
        public async Task<IActionResult> RevokeAdminKey(int keyId)
        {
            try
            {
                var success = await _adminService.RevokeAdminKeyAsync(keyId);

                if (success)
                {
                    return Ok(new { message = "Admin key revoked successfully" });
                }
                else
                {
                    return NotFound(new { error = "Admin key not found or already used" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to revoke admin key", details = ex.Message });
            }
        }
    }

    public class BackupRequestDTO
    {
        public string? BackupPath { get; set; }
    }
}
using Application.Abstractions.Interfaces;
using Application.DTOs.RobotDTOs;
using Application.DTOs.UserDTOs;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IAuthService = Application.Abstractions.Interfaces.IAuthorizationService;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IAuthService _authorizationService;
        private readonly IRobotService _robotService;

        public AuthController(
            ITokenService tokenService,
            IAuthService authorizationService,
            IRobotService robotService)
        {
            _tokenService = tokenService;
            _authorizationService = authorizationService;
            _robotService = robotService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDTO registerData)
        {
            var result = await _authorizationService.RegisterAsync(registerData);

            if (result != RegisterStatus.Success)
            {
                return BadRequest(new { status = result.ToString(), message = "Registration failed" });
            }
            return Ok(new { status = result.ToString(), message = "User registered successfully" });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDTO loginData)
        {
            var result = await _authorizationService.LoginUserAsync(loginData);
            if (result != LoginStatus.Success)
            {
                return BadRequest(new { status = result.ToString(), message = "Login failed" });
            }

            var token = await _tokenService.GenerateToken(loginData);
            return Ok(new { status = result.ToString(), token, message = "Login successful" });
        }

        [AllowAnonymous]
        [HttpPost("robot/register")]
        public async Task<IActionResult> RegisterRobot(RobotRegisterDTO registerData)
        {
            // Use RobotService to register robot (follows Clean Architecture)
            var (success, robotId, errorMessage) = await _robotService.RegisterRobotAsync(registerData);

            if (!success)
            {
                return BadRequest(new { error = errorMessage });
            }

            // Generate token for the robot
            var token = await _tokenService.GenerateRobotToken(robotId!.Value);

            // Get robot details for response
            var robot = await _robotService.GetRobotByIdAsync(robotId.Value);

            return Ok(new
            {
                message = "Robot registered successfully",
                robotId = robotId.Value,
                serialNumber = registerData.SerialNumber,
                token
            });
        }

        [AllowAnonymous]
        [HttpPost("robot/login")]
        public async Task<IActionResult> LoginRobot(RobotLoginDTO loginData)
        {
            // Use RobotService to authenticate robot (follows Clean Architecture)
            var (success, robotId, errorMessage) = await _robotService.AuthenticateRobotAsync(loginData);

            if (!success)
            {
                return Unauthorized(new { error = errorMessage });
            }

            // Generate token for the authenticated robot
            var token = await _tokenService.GenerateRobotToken(robotId!.Value);

            // Get robot details for response
            var robot = await _robotService.GetRobotByIdAsync(robotId.Value);

            return Ok(new
            {
                message = "Robot login successful",
                robotId = robotId.Value,
                serialNumber = robot?.SerialNumber,
                robotName = robot?.Name,
                token
            });
        }
    }
}
using Application.Abstractions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobDeliveryAPI.Extensions;
using System.Security.Claims;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _environment;

        public FileController(
            IFileService fileService,
            IWebHostEnvironment environment)
        {
            _fileService = fileService;
            _environment = environment;
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
        /// Upload images for an order
        /// </summary>
        [HttpPost("order/{orderId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadOrderImages(int orderId, IFormFileCollection files)
        {
            try
            {
                int userId = GetAuthenticatedUserId();

                var uploadedFiles = await _fileService.UploadOrderImagesAsync(orderId, files.ToFileUploadDTOs(), _environment.ContentRootPath);

                return Ok(new { message = $"{uploadedFiles.Count} file(s) uploaded successfully", files = uploadedFiles });
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
                return StatusCode(500, new { error = "An error occurred while uploading files", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        [HttpDelete("{fileId}")]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            try
            {
                int userId = GetAuthenticatedUserId();
                bool isAdmin = User.IsInRole("Admin");

                // Check authorization
                bool isAuthorized = await _fileService.IsUserAuthorizedForFileAsync(fileId, userId, isAdmin);
                if (!isAuthorized)
                {
                    return StatusCode(403, new { error = "You do not have permission to delete this file" });
                }

                // Delete file
                var result = await _fileService.DeleteFileAsync(fileId, _environment.ContentRootPath);
                if (!result)
                {
                    return NotFound(new { error = "File not found" });
                }

                return Ok(new { message = "File deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the file", details = ex.Message });
            }
        }

        /// <summary>
        /// Get files for an order
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderFiles(int orderId)
        {
            try
            {
                var files = await _fileService.GetOrderFilesAsync(orderId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving files", details = ex.Message });
            }
        }

        /// <summary>
        /// Download a file by ID
        /// </summary>
        [HttpGet("{fileId}/download")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                var file = await _fileService.GetFileByIdAsync(fileId);
                if (file == null)
                {
                    return NotFound(new { error = "File not found" });
                }

                var fileBytes = await _fileService.GetFileContentAsync(fileId, _environment.ContentRootPath);
                if (fileBytes == null)
                {
                    return NotFound(new { error = "File not found on disk" });
                }

                return File(fileBytes, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while downloading the file", details = ex.Message });
            }
        }
    }
}
using Application.Abstractions.Interfaces;
using Application.DTOs.NodeDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NodeController : ControllerBase
    {
        private readonly INodeService _nodeService;

        public NodeController(INodeService nodeService)
        {
            _nodeService = nodeService;
        }

        /// <summary>
        /// Create a new delivery node
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateNode([FromBody] CreateNodeDTO nodeDto)
        {
            try
            {
                var result = await _nodeService.CreateNodeAsync(nodeDto);
                return CreatedAtAction(nameof(GetNodeById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the node", details = ex.Message });
            }
        }

        /// <summary>
        /// Get node by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNodeById(int id)
        {
            try
            {
                var node = await _nodeService.GetNodeByIdAsync(id);
                if (node == null)
                {
                    return NotFound(new { error = "Node not found" });
                }
                return Ok(node);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the node", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all nodes
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllNodes()
        {
            try
            {
                var nodes = await _nodeService.GetAllNodesAsync();
                return Ok(nodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving nodes", details = ex.Message });
            }
        }

        /// <summary>
        /// Get nodes by type
        /// </summary>
        [HttpGet("type/{type}")]
        public async Task<IActionResult> GetNodesByType(NodeType type)
        {
            try
            {
                var nodes = await _nodeService.GetNodesByTypeAsync(type);
                return Ok(nodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving nodes by type", details = ex.Message });
            }
        }

        /// <summary>
        /// Find nearest node to given coordinates
        /// </summary>
        [HttpGet("nearest")]
        public async Task<IActionResult> FindNearestNode([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] NodeType? type = null)
        {
            try
            {
                var node = await _nodeService.FindNearestNodeAsync(latitude, longitude, type);
                if (node == null)
                {
                    return NotFound(new { error = "No nodes found" });
                }
                return Ok(node);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while finding nearest node", details = ex.Message });
            }
        }

        /// <summary>
        /// Update a node
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNode(int id, [FromBody] UpdateNodeDTO nodeDto)
        {
            try
            {
                if (id != nodeDto.Id)
                {
                    return BadRequest(new { error = "ID mismatch" });
                }

                var result = await _nodeService.UpdateNodeAsync(nodeDto);
                if (!result)
                {
                    return NotFound(new { error = "Node not found" });
                }

                return Ok(new { message = "Node updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the node", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a node
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNode(int id)
        {
            try
            {
                var result = await _nodeService.DeleteNodeAsync(id);
                if (!result)
                {
                    return NotFound(new { error = "Node not found" });
                }

                return Ok(new { message = "Node deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the node", details = ex.Message });
            }
        }
    }
}
using Application.Abstractions.Interfaces;
using Application.DTOs.OrderDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobDeliveryAPI.Extensions;
using System.Security.Claims;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _environment;

        public OrderController(
            IOrderService orderService,
            IFileService fileService,
            IWebHostEnvironment environment)
        {
            _orderService = orderService;
            _fileService = fileService;
            _environment = environment;
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
        /// Create a new order with optional images (uses authenticated user from JWT token)
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateOrder(
            [FromForm] string name,
            [FromForm] string description,
            [FromForm] double weight,
            [FromForm] decimal productPrice,
            [FromForm] bool isProductPaid,
            [FromForm] int recipientId,
            [FromForm] int deliveryPayer,
            IFormFileCollection? files = null)
        {
            try
            {
                int senderId = GetAuthenticatedUserId();

                // Create order DTO
                var orderDto = new CreateOrderDTO
                {
                    Name = name,
                    Description = description,
                    Weight = weight,
                    ProductPrice = productPrice,
                    IsProductPaid = isProductPaid,
                    RecipientId = recipientId,
                    DeliveryPayer = (Entities.Models.DeliveryPayer)deliveryPayer
                };

                // Create order
                var result = await _orderService.CreateOrderAsync(senderId, orderDto);

                // Upload files if provided
                if (files != null && files.Count > 0)
                {
                    await _fileService.UploadOrderImagesAsync(result.Id, files.ToFileUploadDTOs(), _environment.ContentRootPath);

                    // Reload order with images
                    result = await _orderService.GetOrderByIdAsync(result.Id);
                }

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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [HttpPatch]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDTO updateDto)
        {
            try
            {
                var result = await _orderService.UpdateOrderStatusAsync(updateDto.OrderId, updateDto.NewStatus);
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
        [Authorize(Roles = "Admin")]
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

        /// <summary>
        /// Execute an order - automatically find and assign optimal drone with route calculation
        /// </summary>
        [HttpPost("{id}/execute")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExecuteOrder(int id)
        {
            try
            {
                var result = await _orderService.ExecuteOrderAsync(id);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while executing the order", details = ex.Message });
            }
        }
    }
}
using Application.Abstractions.Interfaces;
using Application.DTOs.PaymentDTOs;
using Application.DTOs.OrderDTOs;
using Entities.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RobDeliveryAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentProcessorService _paymentProcessorService;
    private readonly IOrderRepository _orderRepository;

    public PaymentsController(
        IPaymentProcessorService paymentProcessorService,
        IOrderRepository orderRepository)
    {
        _paymentProcessorService = paymentProcessorService;
        _orderRepository = orderRepository;
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
    /// Process a payment using the specified payment method (PayPal, GooglePay, or Stripe)
    /// </summary>
    [HttpPost("process")]
    public async Task<ActionResult<PaymentResultDTO>> ProcessPayment([FromBody] PaymentRequestDTO request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new PaymentResultDTO
                {
                    Success = false,
                    TransactionId = string.Empty,
                    PaymentMethod = request.PaymentMethod,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    ProcessedAt = DateTime.UtcNow,
                    ErrorMessage = "Payment amount must be greater than zero"
                });
            }

            var result = await _paymentProcessorService.ProcessPaymentAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new PaymentResultDTO
            {
                Success = false,
                TransactionId = string.Empty,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new PaymentResultDTO
            {
                Success = false,
                TransactionId = string.Empty,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Pay for order product and/or delivery
    /// </summary>
    [HttpPost("pay-order")]
    public async Task<ActionResult<PaymentResultDTO>> PayOrder([FromBody] PayOrderDTO paymentDto)
    {
        try
        {
            int userId = GetAuthenticatedUserId();

            // Validate that at least one payment option is selected
            if (!paymentDto.PayProduct && !paymentDto.PayDelivery)
            {
                return BadRequest(new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = "At least one payment option (PayProduct or PayDelivery) must be selected",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Get order
            var order = await _orderRepository.GetByIdAsync(paymentDto.OrderId);
            if (order == null)
            {
                return NotFound(new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = $"Order with ID {paymentDto.OrderId} not found",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Validate user authorization
            bool isAuthorized = false;
            if (paymentDto.PayProduct && order.SenderId == userId)
            {
                isAuthorized = true; // Sender can pay for product
            }
            if (paymentDto.PayDelivery)
            {
                // Check who should pay for delivery
                if ((order.DeliveryPayer == DeliveryPayer.Sender && order.SenderId == userId) ||
                    (order.DeliveryPayer == DeliveryPayer.Recipient && order.RecipientId == userId))
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                return StatusCode(403, new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = "You are not authorized to make this payment",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Calculate total amount to pay
            decimal totalAmount = 0;
            bool willPayProduct = false;
            bool willPayDelivery = false;

            if (paymentDto.PayProduct && !order.IsProductPaid)
            {
                totalAmount += order.ProductPrice;
                willPayProduct = true;
            }
            if (paymentDto.PayDelivery && !order.IsDeliveryPaid)
            {
                totalAmount += order.DeliveryPrice;
                willPayDelivery = true;
            }

            // Check if there's anything to pay
            if (totalAmount == 0)
            {
                return BadRequest(new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = "The selected items are already paid",
                    OrderId = order.Id,
                    ProductPaid = order.IsProductPaid,
                    DeliveryPaid = order.IsDeliveryPaid,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Process payment through payment processor
            var paymentRequest = new PaymentRequestDTO
            {
                Amount = totalAmount,
                Currency = "UAH",
                OrderId = order.Id,
                PaymentMethod = paymentDto.PaymentMethod
            };

            var paymentResult = await _paymentProcessorService.ProcessPaymentAsync(paymentRequest);

            if (!paymentResult.Success)
            {
                return BadRequest(new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = $"Payment failed: {paymentResult.ErrorMessage}",
                    OrderId = order.Id,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Update order payment status
            if (willPayProduct)
            {
                order.IsProductPaid = true;
            }
            if (willPayDelivery)
            {
                order.IsDeliveryPaid = true;
            }

            await _orderRepository.UpdateAsync(order);

            // Return success result
            return Ok(new PaymentResultDTO
            {
                Success = true,
                TransactionId = paymentResult.TransactionId,
                PaymentMethod = paymentResult.PaymentMethod,
                Amount = totalAmount,
                Currency = "UAH",
                ProcessedAt = paymentResult.ProcessedAt,
                OrderId = order.Id,
                ProductPaid = order.IsProductPaid,
                DeliveryPaid = order.IsDeliveryPaid
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new PaymentResultDTO
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new PaymentResultDTO
            {
                Success = false,
                ErrorMessage = $"An error occurred while processing payment: {ex.Message}",
                ProcessedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Refund a previously processed payment
    /// </summary>
    [HttpPost("refund")]
    public async Task<ActionResult<object>> RefundPayment([FromBody] RefundRequestDTO request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Refund amount must be greater than zero" });
            }

            var result = await _paymentProcessorService.RefundPaymentAsync(
                request.PaymentMethod,
                request.TransactionId,
                request.Amount
            );

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = "Refund processed successfully",
                    transactionId = request.TransactionId,
                    amount = request.Amount,
                    processedAt = DateTime.UtcNow
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Refund processing failed"
                });
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
        }
    }
}

public class RefundRequestDTO
{
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
using Application.Abstractions.Interfaces;
using Application.DTOs.RobotDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RobotController : ControllerBase
    {
        private readonly IRobotService _robotService;

        public RobotController(IRobotService robotService)
        {
            _robotService = robotService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRobot([FromBody] CreateRobotDTO robotDto)
        {
            try
            {
                var result = await _robotService.CreateRobotAsync(robotDto);
                return CreatedAtAction(nameof(GetRobotById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRobotById(int id)
        {
            var robot = await _robotService.GetRobotByIdAsync(id);
            return robot == null ? NotFound(new { error = "Robot not found" }) : Ok(robot);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRobots()
        {
            var robots = await _robotService.GetAllRobotsAsync();
            return Ok(robots);
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByStatus(RobotStatus status)
        {
            var robots = await _robotService.GetByStatusAsync(status);
            return Ok(robots);
        }

        [HttpGet("type/{type}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByType(RobotType type)
        {
            var robots = await _robotService.GetByTypeAsync(type);
            return Ok(robots);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableRobots()
        {
            var robots = await _robotService.GetAvailableRobotsAsync();
            return Ok(robots);
        }

        [HttpPatch]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRobot([FromBody] UpdateRobotDTO robotDto)
        {
            try
            {
                if (robotDto.Id != robotDto.Id) return BadRequest(new { error = "ID mismatch" });
                var result = await _robotService.UpdateRobotAsync(robotDto);
                return result ? Ok(new { message = "Robot updated" }) : NotFound(new { error = "Robot not found" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRobot(int id)
        {
            var result = await _robotService.DeleteRobotAsync(id);
            return result ? Ok(new { message = "Robot deleted" }) : NotFound(new { error = "Robot not found" });
        }

        // IoT Device Endpoint - Robot Status Update
        [HttpPost("status")]
        [Authorize(Roles = "Iot,Admin")]
        public async Task<IActionResult> UpdateRobotStatus([FromBody] RobotStatusUpdateDTO statusUpdate)
        {
            try
            {
                // Get robot ID from token
                var robotIdClaim = User.FindFirst("RobotId")?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                int robotId;

                // If Admin, they must provide robot ID in the request or use their own ID claim
                if (userRole == "Admin")
                {
                    var idClaim = User.FindFirst("Id")?.Value;
                    if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out robotId))
                    {
                        return Unauthorized(new { error = "Invalid token" });
                    }
                }
                // If IoT device, use RobotId from token
                else if (!string.IsNullOrEmpty(robotIdClaim) && int.TryParse(robotIdClaim, out robotId))
                {
                    // Robot can only update its own status
                }
                else
                {
                    return Unauthorized(new { error = "Invalid robot token" });
                }

                // Update robot status through service
                var robot = await _robotService.UpdateRobotStatusAsync(robotId, statusUpdate);
                if (robot == null)
                {
                    return NotFound(new { error = "Robot not found" });
                }

                return Ok(new
                {
                    message = "Robot status updated successfully",
                    robotId = robot.Id,
                    status = robot.StatusName,
                    batteryLevel = robot.BatteryLevel,
                    currentNodeId = robot.CurrentNodeId,
                    targetNodeId = statusUpdate.TargetNodeId,
                    coordinates = statusUpdate.CurrentLatitude != null && statusUpdate.CurrentLongitude != null
                        ? new { latitude = statusUpdate.CurrentLatitude, longitude = statusUpdate.CurrentLongitude }
                        : null
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }

        // Get robot's own info (for IoT devices)
        [HttpGet("me")]
        [Authorize(Roles = "Iot")]
        public async Task<IActionResult> GetMyRobotInfo()
        {
            var robotIdClaim = User.FindFirst("RobotId")?.Value;
            if (string.IsNullOrEmpty(robotIdClaim) || !int.TryParse(robotIdClaim, out int robotId))
            {
                return Unauthorized(new { error = "Invalid robot token" });
            }

            var robot = await _robotService.GetRobotByIdAsync(robotId);
            if (robot == null)
            {
                return NotFound(new { error = "Robot not found" });
            }

            return Ok(robot);
        }
    }
}

using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobDeliveryAPI.Extensions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;

        public UserController(
            IUserService userService,
            IWebHostEnvironment environment)
        {
            _userService = userService;
            _environment = environment;
        }

        /// <summary>
        /// Get full profile of authenticated user
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var profile = await _userService.GetProfileAsync(userId);
                if (profile == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving profile", details = ex.Message });
            }
        }

        /// <summary>
        /// Get profile photo of authenticated user
        /// </summary>
        [HttpGet("profile/photo")]
        public async Task<IActionResult> GetProfilePhoto()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var photo = await _userService.GetProfilePhotoAsync(userId);
                if (photo == null)
                {
                    return NotFound(new { error = "Profile photo not found" });
                }

                var fileBytes = await _userService.GetProfilePhotoContentAsync(userId, _environment.ContentRootPath);
                if (fileBytes == null)
                {
                    return NotFound(new { error = "Profile photo file not found on disk" });
                }

                return File(fileBytes, photo.ContentType, photo.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving profile photo", details = ex.Message });
            }
        }

        /// <summary>
        /// Get profile photo of specific user by ID
        /// </summary>
        [HttpGet("{userId}/photo")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserPhoto(int userId)
        {
            try
            {
                var photo = await _userService.GetProfilePhotoAsync(userId);
                if (photo == null)
                {
                    return NotFound(new { error = "Profile photo not found" });
                }

                var fileBytes = await _userService.GetProfilePhotoContentAsync(userId, _environment.ContentRootPath);
                if (fileBytes == null)
                {
                    return NotFound(new { error = "Profile photo file not found on disk" });
                }

                return File(fileBytes, photo.ContentType, photo.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving profile photo", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var userDto = await _userService.GetUserByIdAsync(id);
            if (userDto == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(userDto);
        }

        [HttpGet("my-node")]
        public async Task<IActionResult> GetMyNode()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var nodeDTO = await _userService.GetMyNodeAsync(userId);
                if (nodeDTO == null)
                {
                    return NotFound(new { error = "Personal node not found" });
                }

                return Ok(nodeDTO);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPut("my-node")]
        public async Task<IActionResult> UpdateMyNode([FromBody] UpdateMyNodeDTO updateDto)
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var nodeDTO = await _userService.UpdateMyNodeAsync(userId, updateDto);
                return Ok(new { message = "Personal node updated successfully", node = nodeDTO });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var userDTOs = await _userService.GetAllUsersAsync();
            return Ok(userDTOs);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Search query cannot be empty" });
            }

            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var userDTOs = await _userService.SearchUsersAsync(query, currentUserId);
                return Ok(userDTOs);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update user profile (username, phone, password, and profile photo)
        /// </summary>
        [HttpPut("profile")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile(
            [FromForm] string? userName,
            [FromForm] string? phoneNumber,
            [FromForm] string? password,
            IFormFile? profilePhoto)
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var updateDto = new UpdateUserProfileDTO
                {
                    UserName = userName,
                    PhoneNumber = phoneNumber,
                    Password = password
                };

                var updatedProfile = await _userService.UpdateProfileWithPhotoAsync(userId, updateDto, profilePhoto?.ToFileUploadDTO(), _environment.ContentRootPath);

                return Ok(new { message = "Profile updated successfully", profile = updatedProfile });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating profile", details = ex.Message });
            }
        }
    }
}

