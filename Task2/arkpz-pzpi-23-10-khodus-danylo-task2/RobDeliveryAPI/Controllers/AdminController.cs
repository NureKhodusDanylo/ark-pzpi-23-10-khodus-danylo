using Application.Abstractions.Interfaces;
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
    }

    public class BackupRequestDTO
    {
        public string? BackupPath { get; set; }
    }
}
