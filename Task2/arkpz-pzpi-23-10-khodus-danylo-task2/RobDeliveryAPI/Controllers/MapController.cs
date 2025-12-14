using Application.Abstractions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MapController : ControllerBase
    {
        private readonly IMapService _mapService;

        public MapController(IMapService mapService)
        {
            _mapService = mapService;
        }

        /// <summary>
        /// Get all map data (robots and nodes positions)
        /// </summary>
        [HttpGet("data")]
        public async Task<IActionResult> GetMapData()
        {
            try
            {
                var mapData = await _mapService.GetMapDataAsync();
                return Ok(mapData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving map data", details = ex.Message });
            }
        }
    }
}
