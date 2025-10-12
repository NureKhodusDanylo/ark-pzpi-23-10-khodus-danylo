using Application.Abstractions.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Application.Services
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly string _googleClientId;

        public GoogleTokenValidator(IConfiguration configuration)
        {
            _googleClientId = configuration["Authentication:Google:ClientId"];
        }

        public async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string jwtToken)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleClientId }
            };
            return await GoogleJsonWebSignature.ValidateAsync(jwtToken, settings);
        }
    }
}
