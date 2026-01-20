using BAU_Plagiarism_System.Core.DTOs;
using BAU_Plagiarism_System.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BAU_Plagiarism_System.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly UserService _userService;

        public AuthController(AuthService authService, UserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (result == null)
                return Unauthorized(new { message = "Invalid username or password" });

            return Ok(result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> Register([FromBody] CreateUserDto dto)
        {
            try
            {
                var user = await _userService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetProfile), new { }, user);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " Details: " + ex.InnerException.Message;
                }
                return BadRequest(new { message = errorMessage });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _userService.GetUserByIdAsync(userId);
            
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _userService.UpdateUserAsync(userId, dto);
            
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _userService.ChangePasswordAsync(userId, dto);
            
            if (!result)
                return BadRequest(new { message = "Current password is incorrect" });

            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _userService.ProcessForgotPasswordAsync(dto.Email);
            
            // For security, always return OK even if email not found
            // but for this project we'll return based on actual result
            if (!result)
                return BadRequest(new { message = "Email không tồn tại trong hệ thống" });

            return Ok(new { message = "Mã xác nhận đã được gửi về email của bạn" });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordWithCodeDto dto)
        {
            var result = await _userService.VerifyAndResetPasswordAsync(dto);
            
            if (!result)
                return BadRequest(new { message = "Mã xác nhận không chính xác hoặc đã hết hạn" });

            return Ok(new { message = "Mật khẩu đã được đặt lại thành công" });
        }
    }
}
