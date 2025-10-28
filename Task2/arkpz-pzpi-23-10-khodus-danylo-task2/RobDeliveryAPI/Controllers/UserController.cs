using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var profileDTO = await _userService.GetProfileAsync(userId);
            if (profileDTO == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(profileDTO);
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
    }
}