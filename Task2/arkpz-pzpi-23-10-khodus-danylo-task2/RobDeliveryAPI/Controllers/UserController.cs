using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Entities.Interfaces;
using FileEntity = Entities.Models.File;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFileRepository _fileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWebHostEnvironment _environment;

        public UserController(
            IUserService userService,
            IFileRepository fileRepository,
            IUserRepository userRepository,
            IWebHostEnvironment environment)
        {
            _userService = userService;
            _fileRepository = fileRepository;
            _userRepository = userRepository;
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
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.ProfilePhotoId.HasValue)
                {
                    return NotFound(new { error = "Profile photo not found" });
                }

                var photo = await _fileRepository.GetByIdAsync(user.ProfilePhotoId.Value);
                if (photo == null)
                {
                    return NotFound(new { error = "Profile photo not found" });
                }

                var filePath = Path.Combine(_environment.ContentRootPath, photo.FilePath.TrimStart('/'));
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { error = "Profile photo file not found on disk" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
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
        public async Task<IActionResult> GetUserPhoto(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.ProfilePhotoId.HasValue)
                {
                    return NotFound(new { error = "Profile photo not found" });
                }

                var photo = await _fileRepository.GetByIdAsync(user.ProfilePhotoId.Value);
                if (photo == null)
                {
                    return NotFound(new { error = "Profile photo not found" });
                }

                var filePath = Path.Combine(_environment.ContentRootPath, photo.FilePath.TrimStart('/'));
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { error = "Profile photo file not found on disk" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
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
                // Update basic profile info
                var updateDto = new UpdateUserProfileDTO
                {
                    UserName = userName,
                    PhoneNumber = phoneNumber,
                    Password = password
                };

                var updatedProfile = await _userService.UpdateProfileAsync(userId, updateDto);

                // Handle profile photo upload if provided
                if (profilePhoto != null)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null)
                    {
                        return NotFound(new { error = "User not found" });
                    }

                    // Validate file type and size
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    const long maxFileSize = 5 * 1024 * 1024; // 5MB

                    var extension = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest(new { error = $"Invalid file extension. Allowed: {string.Join(", ", allowedExtensions)}" });
                    }

                    if (profilePhoto.Length > maxFileSize)
                    {
                        return BadRequest(new { error = "File exceeds maximum size of 5MB" });
                    }

                    // Delete old profile photo if exists
                    if (user.ProfilePhotoId.HasValue)
                    {
                        var oldPhoto = await _fileRepository.GetByIdAsync(user.ProfilePhotoId.Value);
                        if (oldPhoto != null)
                        {
                            // Delete file from disk
                            var oldFilePath = Path.Combine(_environment.ContentRootPath, oldPhoto.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            await _fileRepository.DeleteAsync(oldPhoto.Id);
                            await _fileRepository.SaveChangesAsync();
                        }
                    }

                    // Create upload directory if it doesn't exist
                    var uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads", "Profiles");
                    Directory.CreateDirectory(uploadsPath);

                    // Generate unique filename
                    var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsPath, uniqueFileName);

                    // Save file to disk
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePhoto.CopyToAsync(stream);
                    }

                    // Create file entity
                    var fileEntity = new FileEntity
                    {
                        FileName = profilePhoto.FileName,
                        FilePath = $"/Uploads/Profiles/{uniqueFileName}",
                        ContentType = profilePhoto.ContentType,
                        FileSize = profilePhoto.Length,
                        UserId = userId,
                        UploadedAt = DateTime.UtcNow
                    };

                    await _fileRepository.AddAsync(fileEntity);
                    await _fileRepository.SaveChangesAsync();

                    // Update user's profile photo reference
                    user.ProfilePhotoId = fileEntity.Id;
                    await _userRepository.UpdateAsync(user);
                    await _userRepository.SaveChangesAsync();

                    // Update the profile DTO with new photo URL
                    updatedProfile.ProfilePhotoUrl = fileEntity.FilePath;
                }

                return Ok(new { message = "Profile updated successfully", profile = updatedProfile });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating profile", details = ex.Message });
            }
        }
    }
}