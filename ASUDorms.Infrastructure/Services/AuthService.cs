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


        public bool CanAccessDormLocation(int dormLocationId)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    return false;
                }

                var accessibleClaim = httpContext.User.FindFirst("AccessibleDormLocations");
                if (accessibleClaim != null)
                {
                    var accessibleIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(accessibleClaim.Value);
                    return accessibleIds?.Contains(dormLocationId) ?? false;
                }

                // Fallback: check if it matches user's primary location
                var locationClaim = httpContext.User.FindFirst("DormLocationId");
                if (locationClaim != null && int.TryParse(locationClaim.Value, out int primaryLocationId))
                {
                    return primaryLocationId == dormLocationId;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking dorm location access");
                return false;
            }
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var usernameHash = HashString(request.Username);
            _logger.LogDebug("Looking up user: UsernameHash={UsernameHash}", usernameHash);

            var users = await _unitOfWork.Users.FindAsync(u =>
                u.Username == request.Username && u.IsActive);

            var user = users.FirstOrDefault();

            if (user == null)
            {
                _logger.LogWarning("User not found or inactive: UsernameHash={UsernameHash}", usernameHash);
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password: UserId={UserId}, UsernameHash={UsernameHash}",
                    user.Id, usernameHash);
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            var token = GenerateJwtToken(user);
            _logger.LogDebug("JWT token generated for UserId={UserId}", user.Id);

            var location = await _unitOfWork.DormLocations.GetByIdAsync(user.DormLocationId);

            // Get all accessible locations
            var accessibleIds = user.GetAccessibleLocations();
            var accessibleLocations = new Dictionary<int, string>();

            // Fetch actual dorm location names from database
            foreach (var id in accessibleIds)
            {
                var loc = await _unitOfWork.DormLocations.GetByIdAsync(id);
                if (loc != null)
                {
                    accessibleLocations[id] = loc.Name; // Use actual Name from database
                }
            }

            _logger.LogInformation("User {UserId} can access {Count} locations: {Locations}",
                user.Id, accessibleIds.Count, string.Join(", ", accessibleIds));

            return new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role.ToString(),
                DormLocationId = user.DormLocationId,
                DormLocationName = location?.Name,
                AccessibleDormLocationIds = accessibleIds,
                AccessibleDormLocations = accessibleLocations
            };
        }
        public async Task<AppUser> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                _logger.LogDebug("No authenticated user found");
                return null;
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User not found in database: UserId={UserId}", userId);
            }

            return user;
        }

        public int GetCurrentDormLocationId()
        {
            // This should return the USER'S ASSIGNED dorm (from JWT token)
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
                _logger.LogError(ex, "Error getting current dorm location from claims");
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
                    return 0;
                }

                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var user = await _unitOfWork.Users
                        .Query()
                        .Include(u => u.DormLocation)
                        .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                    if (user != null)
                    {
                        return user.DormLocationId; // User's assigned dorm
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current dorm location from database");
                return 0;
            }
        }


        public int GetDormIdFromHeaderOrToken()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return 0;

                // METHOD 1: Direct header check (bypass all logic)
                if (httpContext.Request.Headers.TryGetValue("X-Selected-Dorm-Id", out var headerValue))
                {
                    _logger.LogDebug("🎯 DIRECT HEADER CHECK: X-Selected-Dorm-Id = {Value}", headerValue);
                    if (int.TryParse(headerValue, out int dormId))
                    {
                        // Quick validation against token
                        var accessibleClaim = httpContext.User.FindFirst("AccessibleDormLocations");
                        if (accessibleClaim != null)
                        {
                            try
                            {
                                var accessibleLocations = System.Text.Json.JsonSerializer.Deserialize<List<int>>(accessibleClaim.Value);
                                if (accessibleLocations != null && accessibleLocations.Contains(dormId))
                                {
                                    _logger.LogDebug("✅✅✅ DIRECT: Returning dorm {DormId} from header", dormId);
                                    return dormId;
                                }
                            }
                            catch { }
                        }
                    }
                }

                // METHOD 2: Fallback to token
                var locationClaim = httpContext.User.FindFirst("DormLocationId");
                if (locationClaim != null && int.TryParse(locationClaim.Value, out int tokenDormId))
                {
                    _logger.LogDebug("🎯 DIRECT: Returning dorm {TokenDormId} from token", tokenDormId);
                    return tokenDormId;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDormIdFromHeaderOrToken");
                return 0;
            }
        }

        public async Task<int> GetSelectedDormLocationIdAsync()
        {
            try
            {
                _logger.LogDebug("🔍 GetSelectedDormLocationIdAsync CALLED");

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogDebug("🚫 User not authenticated");
                    return 0;
                }

                // Log ALL headers for debugging
                _logger.LogDebug("📋 ALL REQUEST HEADERS:");
                foreach (var header in httpContext.Request.Headers)
                {
                    _logger.LogDebug("  {Key} = {Value}", header.Key, header.Value);
                }

                // Get header value
                int selectedDormId = 0;
                if (httpContext.Request.Headers.TryGetValue("X-Selected-Dorm-Id", out var headerValue))
                {
                    _logger.LogDebug("✅ Found X-Selected-Dorm-Id header: {HeaderValue}", headerValue);
                    if (int.TryParse(headerValue, out selectedDormId))
                    {
                        _logger.LogDebug("✅ Parsed header to int: {SelectedDormId}", selectedDormId);
                    }
                }
                else
                {
                    _logger.LogDebug("❌ X-Selected-Dorm-Id header NOT FOUND");
                }

                // Get accessible locations from token
                var accessibleClaim = httpContext.User.FindFirst("AccessibleDormLocations");
                if (accessibleClaim != null)
                {
                    _logger.LogDebug("🔑 AccessibleDormLocations claim: {ClaimValue}", accessibleClaim.Value);

                    try
                    {
                        var accessibleLocations = System.Text.Json.JsonSerializer.Deserialize<List<int>>(accessibleClaim.Value);
                        _logger.LogDebug("📊 Accessible locations parsed: {Locations}", string.Join(", ", accessibleLocations ?? new List<int>()));

                        // If we have a valid header AND it's accessible, return it
                        if (selectedDormId > 0 && accessibleLocations != null && accessibleLocations.Contains(selectedDormId))
                        {
                            _logger.LogDebug("✅✅✅ RETURNING DORM FROM HEADER: {SelectedDormId}", selectedDormId);
                            return selectedDormId;
                        }
                        else if (selectedDormId > 0)
                        {
                            _logger.LogWarning("⚠️ Header dorm {SelectedDormId} NOT in accessible locations: {AccessibleLocations}",
                                selectedDormId, string.Join(", ", accessibleLocations ?? new List<int>()));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse accessible locations");
                    }
                }
                else
                {
                    _logger.LogDebug("❌ No AccessibleDormLocations claim found in token");
                }

                // Fallback: get from token (primary dorm)
                var locationClaim = httpContext.User.FindFirst("DormLocationId");
                if (locationClaim != null && int.TryParse(locationClaim.Value, out int tokenDormId))
                {
                    _logger.LogDebug("🔄 FALLBACK - Returning dorm from token: {TokenDormId}", tokenDormId);
                    return tokenDormId;
                }

                _logger.LogDebug("❌ No dorm ID found anywhere");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in GetSelectedDormLocationIdAsync");
                return 0;
            }
        }
        // NEW: Non-async version
        // In AuthService.cs - FIXED GetSelectedDormLocationId method
        public int GetSelectedDormLocationId()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogDebug("User not authenticated");
                    return 0;
                }

                // FIRST: Get selected dorm from header
                int selectedDormId = GetSelectedDormIdFromRequest();
                _logger.LogDebug("📥 Selected dorm from request header: {SelectedDormId}", selectedDormId);

                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogDebug("🔍 Getting selected dorm for user ID: {UserId}", userId);

                    // Check accessible locations from token first (no DB query needed)
                    var accessibleClaim = httpContext.User.FindFirst("AccessibleDormLocations");
                    List<int> accessibleFromToken = null;

                    if (accessibleClaim != null)
                    {
                        try
                        {
                            accessibleFromToken = System.Text.Json.JsonSerializer.Deserialize<List<int>>(accessibleClaim.Value);
                            _logger.LogDebug("🔑 Accessible locations from token: {AccessibleLocations}",
                                string.Join(", ", accessibleFromToken ?? new List<int>()));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse accessible locations from token");
                        }
                    }

                    // If we have header and user can access it (from token), return immediately
                    if (selectedDormId > 0 && accessibleFromToken != null && accessibleFromToken.Contains(selectedDormId))
                    {
                        _logger.LogDebug("✅ Using selected dorm from header (validated via token): {SelectedDormId}", selectedDormId);
                        return selectedDormId;
                    }

                    // Fallback: Get user from database (slower)
                    var user = _unitOfWork.Users
                        .Query()
                        .FirstOrDefault(u => u.Id == userId && u.IsActive);

                    if (user != null)
                    {
                        _logger.LogDebug("👤 User found: {Username}, Primary Dorm: {PrimaryDormId}",
                            user.Username, user.DormLocationId);

                        var accessibleLocations = user.GetAccessibleLocations();
                        _logger.LogDebug("🔑 User accessible locations: {AccessibleLocations}",
                            string.Join(", ", accessibleLocations));

                        // If no header or invalid dorm, use user's assigned dorm
                        if (selectedDormId == 0)
                        {
                            _logger.LogDebug("⚠️ No selected dorm in header, using primary dorm: {PrimaryDormId}",
                                user.DormLocationId);
                            return user.DormLocationId;
                        }

                        if (!accessibleLocations.Contains(selectedDormId))
                        {
                            _logger.LogWarning("🚫 User {UserId} cannot access dorm {SelectedDormId}. Accessible: {AccessibleLocations}",
                                userId, selectedDormId, string.Join(", ", accessibleLocations));
                            _logger.LogDebug("Using primary dorm instead: {PrimaryDormId}", user.DormLocationId);
                            return user.DormLocationId;
                        }

                        _logger.LogDebug("✅ Using selected dorm: {SelectedDormId}", selectedDormId);
                        return selectedDormId;
                    }
                    else
                    {
                        _logger.LogWarning("User not found in database: UserId={UserId}", userId);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selected dorm location");
                return 0;
            }
        }

        private int GetSelectedDormIdFromRequest()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogDebug("❌ HttpContext is null");
                    return 0;
                }

                // Check ALL headers for debugging
                _logger.LogDebug("🔍 Checking all headers for dorm ID:");
                foreach (var header in httpContext.Request.Headers)
                {
                    if (header.Key.Contains("dorm", StringComparison.OrdinalIgnoreCase) ||
                        header.Key.Contains("selected", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("  Found header: {Key} = {Value}", header.Key, header.Value);
                    }
                }

                // Check header (from frontend)
                if (httpContext.Request.Headers.TryGetValue("X-Selected-Dorm-Id", out var headerValue))
                {
                    _logger.LogDebug("🎯 Found X-Selected-Dorm-Id header: {HeaderValue}", headerValue);

                    if (int.TryParse(headerValue, out int dormId))
                    {
                        _logger.LogDebug("✅ Successfully parsed dorm ID: {DormId}", dormId);
                        return dormId;
                    }
                    else
                    {
                        _logger.LogWarning("❌ Failed to parse dorm ID from header: {HeaderValue}", headerValue);
                    }
                }
                else
                {
                    _logger.LogDebug("📭 X-Selected-Dorm-Id header NOT FOUND in request");

                    // Check alternative headers
                    if (httpContext.Request.Headers.TryGetValue("Selected-Dorm-Id", out var altHeaderValue))
                    {
                        _logger.LogDebug("🔄 Found alternative header Selected-Dorm-Id: {Value}", altHeaderValue);
                        if (int.TryParse(altHeaderValue, out int dormId))
                        {
                            return dormId;
                        }
                    }
                }

                // Check query string (alternative)
                if (httpContext.Request.Query.TryGetValue("dormId", out var queryValue))
                {
                    _logger.LogDebug("🔗 Query string dormId found: {QueryValue}", queryValue);
                    if (int.TryParse(queryValue, out int dormId))
                    {
                        return dormId;
                    }
                }

                _logger.LogDebug("❌ No dorm ID found in request");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error getting selected dorm ID from request");
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
                _logger.LogError(ex, "Error getting current user ID");
                return 0;
            }
        }


        private string GenerateJwtToken(AppUser user)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("DormLocationId", user.DormLocationId.ToString()),
        // NEW: Add accessible locations to token
        new Claim("AccessibleDormLocations", user.AccessibleDormLocationIds ?? $"[{user.DormLocationId}]")
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

        private string HashString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "null";

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes)[..8];
        }
    }
}