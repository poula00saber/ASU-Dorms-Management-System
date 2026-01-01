using ASUDorms.Application.DTOs.Holidays;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class HolidayService : IHolidayService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly ILogger<HolidayService> _logger;

        public HolidayService(
            IUnitOfWork unitOfWork,
            IAuthService authService,
            ILogger<HolidayService> logger)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _logger = logger;
        }

        public async Task<HolidayDto> CreateHolidayAsync(CreateHolidayDto dto)
        {
            var nationalIdHash = HashString(dto.StudentNationalId);

            // Validate dates
            if (dto.StartDate > dto.EndDate)
            {
                _logger.LogWarning("Invalid date range: StartDate={StartDate} > EndDate={EndDate}",
                    dto.StartDate.ToString("yyyy-MM-dd"), dto.EndDate.ToString("yyyy-MM-dd"));
                throw new ArgumentException("Start date cannot be after end date");
            }

            if (dto.StartDate < DateTime.Today)
            {
                _logger.LogWarning("Start date in past: StartDate={StartDate}",
                    dto.StartDate.ToString("yyyy-MM-dd"));
                throw new ArgumentException("Start date cannot be in the past");
            }

            // Find student by NationalId
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == dto.StudentNationalId && !s.IsDeleted);

            if (student == null)
            {
                _logger.LogWarning("Student not found: NationalIdHash={NationalIdHash}", nationalIdHash);
                throw new KeyNotFoundException($"Student with National ID '{dto.StudentNationalId}' not found or is deleted");
            }

            // Check for date overlaps
            var existingActiveHolidays = await _unitOfWork.Holidays
                .Query()
                .Where(h => h.StudentNationalId == dto.StudentNationalId && !h.IsDeleted)
                .ToListAsync();

            var isOverlapping = existingActiveHolidays.Any(h =>
                (dto.StartDate <= h.EndDate && dto.EndDate >= h.StartDate));

            if (isOverlapping)
            {
                _logger.LogWarning("Date overlap detected: StudentId={StudentId}, ExistingHolidays={Count}",
                    student.StudentId, existingActiveHolidays.Count);
                throw new InvalidOperationException("Holiday dates overlap with an existing active holiday for this student");
            }

            // Get current user name from auth service
            var currentUser = await _authService.GetCurrentUserAsync();
            var modifiedBy = currentUser?.Username ?? "System";

            var holiday = new Holiday
            {
                StudentNationalId = dto.StudentNationalId,
                StudentId = student.StudentId,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                LastModifiedBy = modifiedBy
            };

            await _unitOfWork.Holidays.AddAsync(holiday);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(holiday, student);
        }

        public async Task DeleteHolidayAsync(int holidayId)
        {
            var holiday = await _unitOfWork.Holidays
                .Query()
                .FirstOrDefaultAsync(h => h.Id == holidayId && !h.IsDeleted);

            if (holiday == null)
            {
                _logger.LogWarning("Holiday not found or already deleted: HolidayId={HolidayId}", holidayId);
                throw new KeyNotFoundException($"Active holiday with ID {holidayId} not found");
            }

            // Get current user name for audit
            var currentUser = await _authService.GetCurrentUserAsync();
            holiday.LastModifiedBy = currentUser?.Username ?? "System";

            // Soft delete
            holiday.IsDeleted = true;

            _unitOfWork.Holidays.Update(holiday);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<HolidayDto>> GetHolidaysByStudentIdAsync(string studentId)
        {
            // Find the student by StudentId
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.StudentId == studentId && !s.IsDeleted);

            if (student == null)
            {
                _logger.LogWarning("Student not found: StudentId={StudentId}", studentId);
                throw new KeyNotFoundException($"Student with Student ID '{studentId}' not found");
            }

            // Get holidays
            var holidays = await _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.StudentNationalId == student.NationalId && !h.IsDeleted)
                .OrderByDescending(h => h.StartDate)
                .ToListAsync();

            return holidays.Select(h => MapToDto(h, h.Student)).ToList();
        }

        public async Task<List<HolidayDto>> GetHolidaysByNationalIdAsync(string nationalId)
        {
            var holidays = await _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.StudentNationalId == nationalId && !h.IsDeleted)
                .OrderByDescending(h => h.StartDate)
                .ToListAsync();

            return holidays.Select(h => MapToDto(h, h.Student)).ToList();
        }

        private HolidayDto MapToDto(Holiday holiday, Student student = null)
        {
            return new HolidayDto
            {
                Id = holiday.Id,
                StudentId = holiday.StudentId,
                StudentNationalId = holiday.StudentNationalId,
                StartDate = holiday.StartDate,
                EndDate = holiday.EndDate,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : null,
                ModifiedBy = holiday.LastModifiedBy,
                LastModifiedDate = holiday.UpdatedAt ?? holiday.CreatedAt
            };
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