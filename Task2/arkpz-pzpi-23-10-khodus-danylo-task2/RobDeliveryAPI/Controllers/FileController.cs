using Application.Abstractions.Interfaces;
using Application.DTOs.FileDTOs;
using Entities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FileEntity = Entities.Models.File;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWebHostEnvironment _environment;

        public FileController(
            IFileRepository fileRepository,
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            IWebHostEnvironment environment)
        {
            _fileRepository = fileRepository;
            _orderRepository = orderRepository;
            _userRepository = userRepository;
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

                // Verify order exists
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                // Verify user is the order sender
                if (order.SenderId != userId)
                {
                    return StatusCode(403, new { error = "Only the order sender can upload images" });
                }

                if (files == null || files.Count == 0)
                {
                    return BadRequest(new { error = "No files provided" });
                }

                // Validate file types and sizes
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                const long maxFileSize = 5 * 1024 * 1024; // 5MB

                var uploadedFiles = new List<FileResponseDTO>();

                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest(new { error = $"File {file.FileName} has invalid extension. Allowed: {string.Join(", ", allowedExtensions)}" });
                    }

                    if (file.Length > maxFileSize)
                    {
                        return BadRequest(new { error = $"File {file.FileName} exceeds maximum size of 5MB" });
                    }

                    // Create upload directory if it doesn't exist
                    var uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads", "Orders", orderId.ToString());
                    Directory.CreateDirectory(uploadsPath);

                    // Generate unique filename
                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsPath, uniqueFileName);

                    // Save file to disk
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Create file entity
                    var fileEntity = new FileEntity
                    {
                        FileName = file.FileName,
                        FilePath = $"/Uploads/Orders/{orderId}/{uniqueFileName}",
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        OrderId = orderId,
                        UploadedAt = DateTime.UtcNow
                    };

                    await _fileRepository.AddAsync(fileEntity);
                    await _fileRepository.SaveChangesAsync();

                    uploadedFiles.Add(new FileResponseDTO
                    {
                        Id = fileEntity.Id,
                        FileName = fileEntity.FileName,
                        FilePath = fileEntity.FilePath,
                        ContentType = fileEntity.ContentType,
                        FileSize = fileEntity.FileSize,
                        UploadedAt = fileEntity.UploadedAt
                    });
                }

                return Ok(new { message = $"{uploadedFiles.Count} file(s) uploaded successfully", files = uploadedFiles });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
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

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return NotFound(new { error = "File not found" });
                }

                // Verify user owns this file (either through order or profile photo)
                bool isAuthorized = false;
                if (file.OrderId.HasValue)
                {
                    var order = await _orderRepository.GetByIdAsync(file.OrderId.Value);
                    if (order != null && order.SenderId == userId)
                    {
                        isAuthorized = true;
                    }
                }
                else if (file.UserId.HasValue && file.UserId == userId)
                {
                    isAuthorized = true;
                }

                if (!isAuthorized && !User.IsInRole("Admin"))
                {
                    return StatusCode(403, new { error = "You do not have permission to delete this file" });
                }

                // Delete file from disk
                var diskPath = Path.Combine(_environment.ContentRootPath, file.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(diskPath))
                {
                    System.IO.File.Delete(diskPath);
                }

                // Delete from database
                await _fileRepository.DeleteAsync(fileId);
                await _fileRepository.SaveChangesAsync();

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
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                var files = await _fileRepository.GetByOrderIdAsync(orderId);
                var response = files.Select(f => new FileResponseDTO
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    FilePath = f.FilePath,
                    ContentType = f.ContentType,
                    FileSize = f.FileSize,
                    UploadedAt = f.UploadedAt
                }).ToList();

                return Ok(response);
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
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return NotFound(new { error = "File not found" });
                }

                var filePath = Path.Combine(_environment.ContentRootPath, file.FilePath.TrimStart('/'));
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { error = "File not found on disk" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while downloading the file", details = ex.Message });
            }
        }
    }
}
