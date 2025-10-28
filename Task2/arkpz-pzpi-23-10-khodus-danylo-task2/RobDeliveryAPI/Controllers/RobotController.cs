using Application.Abstractions.Interfaces;
using Application.DTOs.RobotDTOs;
using Entities.Interfaces;
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
        private readonly IRobotRepository _robotRepository;

        public RobotController(IRobotService robotService, IRobotRepository robotRepository)
        {
            _robotService = robotService;
            _robotRepository = robotRepository;
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

                // Get robot from database
                var robot = await _robotRepository.GetByIdAsync(robotId);
                if (robot == null)
                {
                    return NotFound(new { error = "Robot not found" });
                }

                // Validate and parse status
                if (!Enum.TryParse<RobotStatus>(statusUpdate.Status, true, out var newStatus))
                {
                    return BadRequest(new { error = "Invalid status. Must be: Idle, Delivering, Charging, or Maintenance" });
                }

                // Update robot status and location
                robot.Status = newStatus;
                robot.BatteryLevel = statusUpdate.BatteryLevel;
                robot.CurrentNodeId = statusUpdate.CurrentNodeId;
                robot.CurrentLatitude = statusUpdate.CurrentLatitude;
                robot.CurrentLongitude = statusUpdate.CurrentLongitude;
                robot.TargetNodeId = statusUpdate.TargetNodeId;

                await _robotRepository.UpdateAsync(robot);

                return Ok(new
                {
                    message = "Robot status updated successfully",
                    robotId = robot.Id,
                    status = robot.Status.ToString(),
                    batteryLevel = robot.BatteryLevel,
                    currentNodeId = robot.CurrentNodeId,
                    targetNodeId = robot.TargetNodeId,
                    coordinates = robot.CurrentLatitude != null && robot.CurrentLongitude != null
                        ? new { latitude = robot.CurrentLatitude, longitude = robot.CurrentLongitude }
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
