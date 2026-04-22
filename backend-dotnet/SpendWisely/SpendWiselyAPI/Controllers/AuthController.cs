using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SpendWiselyAPI.Application.DTOs.User;
using SpendWiselyAPI.Application.Interfaces;

namespace SpendWiselyAPI.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserRequest request)
        {
            var user = await _userService.RegisterUserAsync(
                request.Name,
                request.Email,
                request.Password
            );

            return Ok(new { user.Id, user.Name, user.Email, user.Role });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserRequest request)
        {
            var response = await _userService.LoginUserAsync(
                request.Email,
                request.Password
            );

            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(SpendWiselyAPI.Application.DTOs.User.RefreshRequest request)
        {
            var response = await _userService.RefreshTokenAsync(
                request.UserId,
                request.RefreshToken
            );

            return Ok(response);
        }
    }
}
