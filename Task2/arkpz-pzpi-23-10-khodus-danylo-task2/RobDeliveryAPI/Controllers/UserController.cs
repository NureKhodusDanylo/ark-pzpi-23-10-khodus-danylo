using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Application.Services;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IAuthService = Application.Abstractions.Interfaces.IAuthorizationService;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(ITokenService _tokenService, IAuthService _authorizationService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDTO registerData)
        {
            var result = await _authorizationService.RegisterAsync(registerData);

            if (result != RegisterStatus.Success)
            {
                return BadRequest(result.ToString());
            }
            return Ok(result.ToString());
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDTO loginData)
        {
            var result = await _authorizationService.LoginUserAsync(loginData);
            if (result != LoginStatus.Success)
            {
                return BadRequest(result.ToString());
            }

            var token = await _tokenService.GenerateToken(loginData);
            return Ok(new { token });
        }
    }
}