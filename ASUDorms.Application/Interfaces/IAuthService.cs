
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
        int GetCurrentDormLocationId();
        Task<int> GetCurrentDormLocationIdAsync(); // ← Add this
    }
}