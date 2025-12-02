using ASUDorms.Application.DTOs.Auth;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ASUDorms.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var users = await _unitOfWork.Users.FindAsync(u =>
                u.Username == request.Username && u.IsActive);

            var user = users.FirstOrDefault();

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            var token = GenerateJwtToken(user);

            var location = await _unitOfWork.DormLocations.GetByIdAsync(user.DormLocationId);

            return new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role.ToString(),
                DormLocationId = user.DormLocationId,
                DormLocationName = location?.Name
            };
        }

        public async Task<AppUser> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return null;

            return await _unitOfWork.Users.GetByIdAsync(userId);
        }

        public int GetCurrentDormLocationId()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    return 0;
                }

                var locationClaim = httpContext.User.FindFirst("DormLocationId");
                if (locationClaim != null && int.TryParse(locationClaim.Value, out int locationId))
                {
                    return locationId;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentDormLocationId");
                return 0;
            }
        }

        public async Task<int> GetCurrentDormLocationIdAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogWarning("User is not authenticated");
                    return 0;
                }

                // Try to get user ID from claims
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Get user from database with dorm location
                    var user = await _unitOfWork.Users
                        .Query()
                        .Include(u => u.DormLocation)
                        .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                    if (user != null)
                    {
                        _logger.LogInformation($"Found user {user.Username} with DormLocationId: {user.DormLocationId}");
                        return user.DormLocationId;
                    }
                }

                _logger.LogWarning("Could not find user or dorm location from database");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentDormLocationIdAsync");
                return 0;
            }
        }

        private int GetCurrentUserId()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    return 0;
                }

                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentUserId");
                return 0;
            }
        }

        private string GenerateJwtToken(AppUser user)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("DormLocationId", user.DormLocationId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}