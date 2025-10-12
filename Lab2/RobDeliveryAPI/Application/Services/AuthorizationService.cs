using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Entities;
using Entities.Models;
using Google.Apis.Auth;

namespace Application.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IGoogleTokenValidator _googleTokenValidator;

        public AuthorizationService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IGoogleTokenValidator googleTokenValidator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _googleTokenValidator = googleTokenValidator;
        }

        public async Task<RegisterStatus> RegisterAsync(UserRegisterDTO registerData)
        {
            if (!string.IsNullOrEmpty(registerData.googleJwtToken))
            {
                return await RegisterUserWithGoogleAsync(registerData.googleJwtToken);
            }

            if (!string.IsNullOrEmpty(registerData.Password))
            {
                return await RegisterUserWithPasswordAsync(registerData.Name, registerData.Email, registerData.Password, registerData.PhoneNumber);
            }

            return RegisterStatus.UnknownOathProvider;
        }

        public async Task<LoginStatus> LoginUserAsync(UserLoginDTO loginData)
        {
            if (!string.IsNullOrEmpty(loginData.googleJwtToken))
            {
                return await LoginUserWithGoogleAsync(loginData.googleJwtToken);
            }

            if (!string.IsNullOrEmpty(loginData.Password))
            {
                return await LoginUserWithPasswordAsync(loginData.Email, loginData.Password);
            }

            return LoginStatus.UnknownOathProvider;
        }

        private async Task<RegisterStatus> RegisterUserWithPasswordAsync(string name, string email, string password, string phoneNumber)
        {
            if (await _userRepository.ExistsByEmailAsync(email))
            {
                return RegisterStatus.EmailBusy;
            }

            string passwordHash = _passwordHasher.Hash(password);
            var user = new User
            {
                UserName = name,
                Email = email,
                PasswordHash = passwordHash,
                PhoneNumber = phoneNumber
            };

            await _userRepository.AddAsync(user);
            return RegisterStatus.Success;
        }

        private async Task<RegisterStatus> RegisterUserWithGoogleAsync(string googleJwtToken)
        {
            try
            {
                var payload = await _googleTokenValidator.ValidateAsync(googleJwtToken);
                if (await _userRepository.GetByGoogleIdAsync(payload.Subject) != null)
                {
                    return RegisterStatus.EmailBusy;
                }

                var user = new User
                {
                    UserName = payload.Name,
                    Email = payload.Email,
                    GoogleId = payload.Subject
                };

                await _userRepository.AddAsync(user);
                return RegisterStatus.Success;
            }
            catch (InvalidJwtException)
            {
                return RegisterStatus.InvalidToken;
            }
        }

        private async Task<LoginStatus> LoginUserWithPasswordAsync(string email, string password)
        {
            if (!await _userRepository.ExistsByEmailAsync(email))
            {
                return LoginStatus.IncorrectEmail;
            }

            string passwordHash = _passwordHasher.Hash(password);
            bool isPasswordValid = await _userRepository.IsPasswordValidByEmailAsync(email, passwordHash);

            return isPasswordValid ? LoginStatus.Success : LoginStatus.IncorrectPassword;
        }

        private async Task<LoginStatus> LoginUserWithGoogleAsync(string googleJwtToken)
        {
            try
            {
                var payload = await _googleTokenValidator.ValidateAsync(googleJwtToken);
                if (await _userRepository.GetByGoogleIdAsync(payload.Subject) == null)
                {
                    return LoginStatus.UnregisteredGoogle;
                }
                return LoginStatus.Success;
            }
            catch (InvalidJwtException)
            {
                return LoginStatus.InvalidToken;
            }
        }
    }
}