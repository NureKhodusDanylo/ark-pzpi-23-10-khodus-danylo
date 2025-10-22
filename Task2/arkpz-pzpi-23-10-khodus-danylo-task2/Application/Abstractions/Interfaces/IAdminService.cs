using Application.DTOs.AdminDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IAdminService
    {
        /// <summary>
        /// Get system statistics for admin dashboard
        /// </summary>
        Task<SystemStatsDTO> GetSystemStatsAsync();

        /// <summary>
        /// Export all delivery history to JSON format
        /// </summary>
        Task<string> ExportDeliveryHistoryAsync();

        /// <summary>
        /// Create database backup
        /// </summary>
        Task<bool> CreateDatabaseBackupAsync(string backupPath);

        /// <summary>
        /// Get robot efficiency analytics
        /// </summary>
        Task<Dictionary<int, double>> GetRobotEfficiencyAsync();
    }
}
