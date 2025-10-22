using Application.Abstractions.Interfaces;
using Application.DTOs.RobotDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetRobotById(int id)
        {
            var robot = await _robotService.GetRobotByIdAsync(id);
            return robot == null ? NotFound(new { error = "Robot not found" }) : Ok(robot);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRobots()
        {
            var robots = await _robotService.GetAllRobotsAsync();
            return Ok(robots);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetByStatus(RobotStatus status)
        {
            var robots = await _robotService.GetByStatusAsync(status);
            return Ok(robots);
        }

        [HttpGet("type/{type}")]
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

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRobot(int id, [FromBody] UpdateRobotDTO robotDto)
        {
            try
            {
                if (id != robotDto.Id) return BadRequest(new { error = "ID mismatch" });
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
    }
}
