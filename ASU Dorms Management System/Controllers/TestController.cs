using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        [HttpGet("cors-test")]
        public IActionResult CorsTest()
        {
            _logger.LogInformation("🔍 CORS TEST - Headers received:");

            // Log all headers
            foreach (var header in Request.Headers)
            {
                _logger.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value.ToArray()));
            }

            // Check for custom header
            string dormHeaderValue = null;
            if (Request.Headers.TryGetValue("X-Selected-Dorm-Id", out var dormHeader))
            {
                dormHeaderValue = dormHeader;
                _logger.LogInformation("✅✅✅ X-Selected-Dorm-Id FOUND: {Value}", dormHeader);
            }
            else
            {
                _logger.LogInformation("❌❌❌ X-Selected-Dorm-Id NOT FOUND");
            }

            return Ok(new
            {
                message = "CORS test successful",
                headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                customHeader = dormHeaderValue,
                time = DateTime.UtcNow
            });
        }

        [HttpGet("auth-test")]
        public IActionResult AuthTest()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "Not authenticated" });
            }

            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

            _logger.LogInformation("🔐 AUTH TEST - User authenticated");
            _logger.LogInformation("  User ID: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            _logger.LogInformation("  DormLocationId: {DormId}", User.FindFirst("DormLocationId")?.Value);

            return Ok(new
            {
                message = "Authentication test successful",
                isAuthenticated = true,
                userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
                dormLocationId = User.FindFirst("DormLocationId")?.Value,
                accessibleDormLocations = User.FindFirst("AccessibleDormLocations")?.Value,
                claims = claims
            });
        }
    }
}