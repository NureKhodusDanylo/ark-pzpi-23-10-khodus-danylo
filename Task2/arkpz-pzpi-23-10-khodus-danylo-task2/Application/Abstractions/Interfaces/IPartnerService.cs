using Application.DTOs.PartnerDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IPartnerService
    {
        Task<PartnerResponseDTO> CreatePartnerAsync(CreatePartnerDTO partnerDto);
        Task<PartnerResponseDTO?> GetPartnerByIdAsync(int partnerId);
        Task<IEnumerable<PartnerResponseDTO>> GetAllPartnersAsync();
        Task<IEnumerable<PartnerResponseDTO>> GetPartnersByNodeIdAsync(int nodeId);
        Task<bool> UpdatePartnerAsync(UpdatePartnerDTO partnerDto);
        Task<bool> DeletePartnerAsync(int partnerId);
    }
}
