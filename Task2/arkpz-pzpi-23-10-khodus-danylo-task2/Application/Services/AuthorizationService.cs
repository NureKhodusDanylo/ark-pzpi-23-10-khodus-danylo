using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Entities;
using Entities.Enums;
using Entities.Interfaces;
using Entities.Models;
using Google.Apis.Auth;

namespace Application.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IGoogleTokenValidator _googleTokenValidator;
        private readonly INodeRepository _nodeRepository;

        public AuthorizationService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IGoogleTokenValidator googleTokenValidator,
            INodeRepository nodeRepository)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _googleTokenValidator = googleTokenValidator;
            _nodeRepository = nodeRepository;
        }

        public async Task<RegisterStatus> RegisterAsync(UserRegisterDTO registerData)
        {
            if (!string.IsNullOrEmpty(registerData.googleJwtToken))
            {
                return await RegisterUserWithGoogleAsync(registerData);
            }

            if (!string.IsNullOrEmpty(registerData.Password))
            {
                return await RegisterUserWithPasswordAsync(registerData);
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

        private async Task<RegisterStatus> RegisterUserWithPasswordAsync(UserRegisterDTO registerData)
        {
            if (await _userRepository.ExistsByEmailAsync(registerData.Email))
            {
                return RegisterStatus.EmailBusy;
            }

            // Create personal node for user
            var personalNode = new Node
            {
                Name = string.IsNullOrEmpty(registerData.Address)
                    ? $"{registerData.UserName}'s Location"
                    : registerData.Address,
                Latitude = registerData.Latitude,
                Longitude = registerData.Longitude,
                Type = NodeType.UserNode
            };

            await _nodeRepository.AddAsync(personalNode);
            await _nodeRepository.SaveChangesAsync();

            string passwordHash = _passwordHasher.Hash(registerData.Password);

            // First registered user becomes Admin
            var allUsers = await _userRepository.GetAllAsync();
            var isFirstUser = !allUsers.Any();

            var user = new User
            {
                UserName = registerData.UserName,
                Email = registerData.Email,
                PasswordHash = passwordHash,
                PhoneNumber = registerData.PhoneNumber,
                Role = isFirstUser ? UserRole.Admin : UserRole.User,
                PersonalNodeId = personalNode.Id
            };

            await _userRepository.AddAsync(user);
            return RegisterStatus.Success;
        }

        private async Task<RegisterStatus> RegisterUserWithGoogleAsync(UserRegisterDTO registerData)
        {
            try
            {
                var payload = await _googleTokenValidator.ValidateAsync(registerData.googleJwtToken);
                if (await _userRepository.GetByGoogleIdAsync(payload.Subject) != null)
                {
                    return RegisterStatus.EmailBusy;
                }

                // Create personal node for user
                var personalNode = new Node
                {
                    Name = string.IsNullOrEmpty(registerData.Address)
                        ? $"{payload.Name}'s Location"
                        : registerData.Address,
                    Latitude = registerData.Latitude,
                    Longitude = registerData.Longitude,
                    Type = NodeType.UserNode
                };

                await _nodeRepository.AddAsync(personalNode);
                await _nodeRepository.SaveChangesAsync();

                // First registered user becomes Admin
                var allUsers = await _userRepository.GetAllAsync();
                var isFirstUser = !allUsers.Any();

                var user = new User
                {
                    UserName = payload.Name,
                    Email = payload.Email,
                    GoogleId = payload.Subject,
                    Role = isFirstUser ? UserRole.Admin : UserRole.User,
                    PersonalNodeId = personalNode.Id
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