using Application.Abstractions.Interfaces;
using Application.DTOs.PartnerDTOs;
using Entities.Interfaces;
using Entities.Models;

namespace Application.Services
{
    public class PartnerService : IPartnerService
    {
        private readonly IPartnerRepository _partnerRepository;
        private readonly INodeRepository _nodeRepository;

        public PartnerService(IPartnerRepository partnerRepository, INodeRepository nodeRepository)
        {
            _partnerRepository = partnerRepository;
            _nodeRepository = nodeRepository;
        }

        public async Task<PartnerResponseDTO> CreatePartnerAsync(CreatePartnerDTO partnerDto)
        {
            if (string.IsNullOrWhiteSpace(partnerDto.Name))
                throw new ArgumentException("Partner name is required");
            if (string.IsNullOrWhiteSpace(partnerDto.ContactPerson))
                throw new ArgumentException("Contact person is required");
            if (string.IsNullOrWhiteSpace(partnerDto.PhoneNumber))
                throw new ArgumentException("Phone number is required");

            // Validate that node exists
            var node = await _nodeRepository.GetByIdAsync(partnerDto.NodeId);
            if (node == null)
                throw new ArgumentException($"Node with ID {partnerDto.NodeId} does not exist");

            var partner = new Partner
            {
                Name = partnerDto.Name,
                ContactPerson = partnerDto.ContactPerson,
                PhoneNumber = partnerDto.PhoneNumber,
                NodeId = partnerDto.NodeId
            };

            await _partnerRepository.AddAsync(partner);
            var created = await _partnerRepository.GetByIdAsync(partner.Id);
            return MapToResponseDTO(created!);
        }

        public async Task<PartnerResponseDTO?> GetPartnerByIdAsync(int partnerId)
        {
            var partner = await _partnerRepository.GetByIdAsync(partnerId);
            return partner == null ? null : MapToResponseDTO(partner);
        }

        public async Task<IEnumerable<PartnerResponseDTO>> GetAllPartnersAsync()
        {
            var partners = await _partnerRepository.GetAllAsync();
            return partners.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<PartnerResponseDTO>> GetPartnersByNodeIdAsync(int nodeId)
        {
            var partners = await _partnerRepository.GetByNodeIdAsync(nodeId);
            return partners.Select(MapToResponseDTO);
        }

        public async Task<bool> UpdatePartnerAsync(UpdatePartnerDTO partnerDto)
        {
            var partner = await _partnerRepository.GetByIdAsync(partnerDto.Id);
            if (partner == null) return false;

            if (string.IsNullOrWhiteSpace(partnerDto.Name))
                throw new ArgumentException("Partner name is required");
            if (string.IsNullOrWhiteSpace(partnerDto.ContactPerson))
                throw new ArgumentException("Contact person is required");
            if (string.IsNullOrWhiteSpace(partnerDto.PhoneNumber))
                throw new ArgumentException("Phone number is required");

            // Validate that node exists
            var node = await _nodeRepository.GetByIdAsync(partnerDto.NodeId);
            if (node == null)
                throw new ArgumentException($"Node with ID {partnerDto.NodeId} does not exist");

            partner.Name = partnerDto.Name;
            partner.ContactPerson = partnerDto.ContactPerson;
            partner.PhoneNumber = partnerDto.PhoneNumber;
            partner.NodeId = partnerDto.NodeId;

            await _partnerRepository.UpdateAsync(partner);
            return true;
        }

        public async Task<bool> DeletePartnerAsync(int partnerId)
        {
            if (!await _partnerRepository.ExistsAsync(partnerId)) return false;
            await _partnerRepository.DeleteAsync(partnerId);
            return true;
        }

        private static PartnerResponseDTO MapToResponseDTO(Partner partner)
        {
            return new PartnerResponseDTO
            {
                Id = partner.Id,
                Name = partner.Name,
                ContactPerson = partner.ContactPerson,
                PhoneNumber = partner.PhoneNumber,
                NodeId = partner.NodeId,
                LocationName = partner.Location?.Name
            };
        }
    }
}
