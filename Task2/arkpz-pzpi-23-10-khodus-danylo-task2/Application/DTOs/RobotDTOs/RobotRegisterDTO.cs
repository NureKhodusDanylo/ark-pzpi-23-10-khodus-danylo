using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.RobotDTOs
{
    public class RobotRegisterDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        public string Type { get; set; } // "GroundCourier" or "Drone"

        [Required]
        public string SerialNumber { get; set; } // Unique identifier for the robot

        [Required]
        public string AccessKey { get; set; } // Secret key for robot authentication
    }
}
