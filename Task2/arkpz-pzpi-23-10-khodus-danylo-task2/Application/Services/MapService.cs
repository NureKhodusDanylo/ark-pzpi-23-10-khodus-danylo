using Application.Abstractions.Interfaces;
using Application.DTOs.MapDTOs;
using Entities.Interfaces;
using Entities.Models;

namespace Application.Services
{
    public class MapService : IMapService
    {
        private readonly IRobotRepository _robotRepository;
        private readonly INodeRepository _nodeRepository;

        public MapService(IRobotRepository robotRepository, INodeRepository nodeRepository)
        {
            _robotRepository = robotRepository;
            _nodeRepository = nodeRepository;
        }

        public async Task<MapDataResponseDTO> GetMapDataAsync()
        {
            var robots = await _robotRepository.GetAllAsync();
            var nodes = await _nodeRepository.GetAllAsync();

            var robotsList = robots.ToList();
            var nodesList = nodes.ToList();

            // Count robots at each node
            var robotsAtNodes = robotsList
                .Where(r => r.CurrentNodeId.HasValue)
                .GroupBy(r => r.CurrentNodeId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return new MapDataResponseDTO
            {
                Robots = robotsList.Select(MapRobotToPosition).ToList(),
                Nodes = nodesList.Select(n => MapNodeToPosition(n, robotsAtNodes.GetValueOrDefault(n.Id, 0))).ToList()
            };
        }

        private RobotMapPositionDTO MapRobotToPosition(Robot robot)
        {
            double? latitude = robot.CurrentLatitude;
            double? longitude = robot.CurrentLongitude;

            // If robot is at a node and doesn't have custom coordinates, use node coordinates
            if ((!latitude.HasValue || !longitude.HasValue) && robot.CurrentNode != null)
            {
                latitude = robot.CurrentNode.Latitude;
                longitude = robot.CurrentNode.Longitude;
            }

            return new RobotMapPositionDTO
            {
                Id = robot.Id,
                Name = robot.Name,
                Model = robot.Model,
                Type = robot.Type,
                TypeName = robot.Type.ToString(),
                Status = robot.Status,
                StatusName = robot.Status.ToString(),
                BatteryLevel = robot.BatteryLevel,
                Latitude = latitude,
                Longitude = longitude,
                CurrentNodeId = robot.CurrentNodeId,
                CurrentNodeName = robot.CurrentNode?.Name,
                TargetNodeId = robot.TargetNodeId,
                TargetNodeName = robot.TargetNode?.Name,
                ActiveOrdersCount = robot.ActiveOrders?.Count ?? 0
            };
        }

        private NodeMapPositionDTO MapNodeToPosition(Node node, int robotsAtNode)
        {
            return new NodeMapPositionDTO
            {
                Id = node.Id,
                Name = node.Name,
                Latitude = node.Latitude,
                Longitude = node.Longitude,
                Type = node.Type,
                TypeName = node.Type.ToString(),
                RobotsAtNode = robotsAtNode
            };
        }
    }
}
