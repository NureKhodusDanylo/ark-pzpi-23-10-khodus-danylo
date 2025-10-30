using Entities.Interfaces;

namespace Entities.Models
{
    public class Robot : IDbEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public RobotType Type { get; set; }
        public RobotStatus Status { get; set; }
        public double BatteryLevel { get; set; }

        // Flight characteristics (meters per 1% battery considering weight)
        public double MaxFlightRangeMeters { get; set; } = 10000; // Default 10km per full charge

        // Authentication fields for IoT devices
        public string? SerialNumber { get; set; } // Unique identifier
        public string? AccessKeyHash { get; set; } // Hashed access key for authentication

        // Location tracking
        public int? CurrentNodeId { get; set; }
        public virtual Node? CurrentNode { get; set; }

        public double? CurrentLatitude { get; set; } // GPS coordinates when not at a node
        public double? CurrentLongitude { get; set; }

        public int? TargetNodeId { get; set; } // Destination node (null if idle)
        public virtual Node? TargetNode { get; set; }

        public virtual ICollection<Order> ActiveOrders { get; set; }
    }
}