using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Application.DTOs.NodeDTOs;
using Entities.Interfaces;
using Entities.Enums;
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
        private readonly IUserRepository _userRepository;
        private readonly INodeRepository _nodeRepository;

        public UserController(IUserRepository userRepository, INodeRepository nodeRepository)
        {
            _userRepository = userRepository;
            _nodeRepository = nodeRepository;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var profileDTO = new UserProfileDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.ToString(),
                SentOrdersCount = user.SentOrders?.Count ?? 0,
                ReceivedOrdersCount = user.ReceivedOrders?.Count ?? 0
            };

            return Ok(profileDTO);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var profileDTO = new UserProfileDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.ToString(),
                SentOrdersCount = user.SentOrders?.Count ?? 0,
                ReceivedOrdersCount = user.ReceivedOrders?.Count ?? 0
            };

            return Ok(profileDTO);
        }

        [HttpGet("my-node")]
        public async Task<IActionResult> GetMyNode()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            if (user.PersonalNodeId == null)
            {
                return NotFound(new { error = "Personal node not found" });
            }

            var node = await _nodeRepository.GetByIdAsync(user.PersonalNodeId.Value);
            if (node == null)
            {
                return NotFound(new { error = "Personal node not found" });
            }

            var nodeDTO = new NodeResponseDTO
            {
                Id = node.Id,
                Name = node.Name,
                Latitude = node.Latitude,
                Longitude = node.Longitude,
                Type = node.Type,
                TypeName = node.Type.ToString()
            };

            return Ok(nodeDTO);
        }

        [HttpPut("my-node")]
        public async Task<IActionResult> UpdateMyNode([FromBody] UpdateMyNodeDTO updateDto)
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            if (user.PersonalNodeId == null)
            {
                return NotFound(new { error = "Personal node not found" });
            }

            var node = await _nodeRepository.GetByIdAsync(user.PersonalNodeId.Value);
            if (node == null)
            {
                return NotFound(new { error = "Personal node not found" });
            }

            // Update node coordinates and address
            node.Latitude = updateDto.Latitude;
            node.Longitude = updateDto.Longitude;
            if (!string.IsNullOrEmpty(updateDto.Address))
            {
                node.Name = updateDto.Address;
            }

            await _nodeRepository.UpdateAsync(node);
            await _nodeRepository.SaveChangesAsync();

            var nodeDTO = new NodeResponseDTO
            {
                Id = node.Id,
                Name = node.Name,
                Latitude = node.Latitude,
                Longitude = node.Longitude,
                Type = node.Type,
                TypeName = node.Type.ToString()
            };

            return Ok(new { message = "Personal node updated successfully", node = nodeDTO });
        }
    }
}