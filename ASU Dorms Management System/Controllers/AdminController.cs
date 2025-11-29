using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Enums;
using ASUDorms.Domain.Interfaces;
using ASUDorms.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Registration")]
    public class AdminController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var passwordHash = AuthService.HashPassword(dto.Password);

            var user = new AppUser
            {
                Username = dto.Username,
                PasswordHash = passwordHash,
                Role = dto.Role,
                DormLocationId = dto.DormLocationId,
                IsActive = true
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "User created successfully", userId = user.Id });
        }

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations()
        {
            var locations = await _unitOfWork.DormLocations.GetAllAsync();
            return Ok(locations);
        }
    }

    public class CreateUserDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }
        public int DormLocationId { get; set; }
    }
}
