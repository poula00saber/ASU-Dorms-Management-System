using ASUDorms.Application.DTOs.Auth;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // Hash username for privacy in logs
            var usernameHash = HashString(request.Username);

            _logger.LogInformation("Login attempt: UsernameHash={UsernameHash}", usernameHash);

            try
            {
                var response = await _authService.LoginAsync(request);

                _logger.LogInformation("Login successful: UsernameHash={UsernameHash}, Role={Role}",
                    usernameHash, response.Role);

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed - invalid credentials: UsernameHash={UsernameHash}",
                    usernameHash);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error: UsernameHash={UsernameHash}", usernameHash);
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        private string HashString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "null";

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes)[..8];
        }
    }
}