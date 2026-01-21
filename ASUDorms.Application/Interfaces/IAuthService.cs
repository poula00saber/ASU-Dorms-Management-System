
// Application/Interfaces/IAuthService.cs
using ASUDorms.Application.DTOs.Auth;
using ASUDorms.Domain.Entities;
using System.Threading.Tasks;

namespace ASUDorms.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<AppUser> GetCurrentUserAsync();
        int GetCurrentDormLocationId(); // User's assigned dorm
        Task<int> GetCurrentDormLocationIdAsync();
        int GetSelectedDormLocationId(); // SELECTED dorm (with header check)
        Task<int> GetSelectedDormLocationIdAsync();
        bool CanAccessDormLocation(int dormLocationId);
        int GetDormIdFromHeaderOrToken();

    }
}