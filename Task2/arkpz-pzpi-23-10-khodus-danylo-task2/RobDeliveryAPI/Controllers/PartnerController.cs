using Application.Abstractions.Interfaces;
using Application.DTOs.PartnerDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PartnerController : ControllerBase
    {
        private readonly IPartnerService _partnerService;

        public PartnerController(IPartnerService partnerService)
        {
            _partnerService = partnerService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePartner([FromBody] CreatePartnerDTO partnerDto)
        {
            try
            {
                var result = await _partnerService.CreatePartnerAsync(partnerDto);
                return CreatedAtAction(nameof(GetPartnerById), new { id = result.Id }, result);
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
        public async Task<IActionResult> GetPartnerById(int id)
        {
            var partner = await _partnerService.GetPartnerByIdAsync(id);
            return partner == null ? NotFound(new { error = "Partner not found" }) : Ok(partner);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPartners()
        {
            var partners = await _partnerService.GetAllPartnersAsync();
            return Ok(partners);
        }

        [HttpGet("node/{nodeId}")]
        public async Task<IActionResult> GetPartnersByNode(int nodeId)
        {
            var partners = await _partnerService.GetPartnersByNodeIdAsync(nodeId);
            return Ok(partners);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePartner(int id, [FromBody] UpdatePartnerDTO partnerDto)
        {
            try
            {
                if (id != partnerDto.Id) return BadRequest(new { error = "ID mismatch" });
                var result = await _partnerService.UpdatePartnerAsync(partnerDto);
                return result ? Ok(new { message = "Partner updated" }) : NotFound(new { error = "Partner not found" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartner(int id)
        {
            var result = await _partnerService.DeletePartnerAsync(id);
            return result ? Ok(new { message = "Partner deleted" }) : NotFound(new { error = "Partner not found" });
        }
    }
}
