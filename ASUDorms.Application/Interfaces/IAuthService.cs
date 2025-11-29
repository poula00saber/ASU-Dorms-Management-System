using ASUDorms.Application.DTOs.Auth;
using ASUDorms.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<AppUser> GetCurrentUserAsync();
        int GetCurrentDormLocationId();
    }
}
