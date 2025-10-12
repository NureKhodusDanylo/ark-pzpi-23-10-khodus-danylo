using Application.DTOs.UserDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IAuthorizationService
    {
        Task<RegisterStatus> RegisterAsync(UserRegisterDTO registerData);
        Task<LoginStatus> LoginUserAsync(UserLoginDTO loginData);
    }
}