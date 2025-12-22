using ASUDorms.Application.DTOs.Holidays;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public HolidayService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<HolidayDto> CreateHolidayAsync(CreateHolidayDto dto)
        {
            // Validate dates
            if (dto.StartDate > dto.EndDate)
            {
                throw new ArgumentException("Start date cannot be after end date");
            }

            if (dto.StartDate < DateTime.Today)
            {
                throw new ArgumentException("Start date cannot be in the past");
            }

            // Find student by NationalId
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == dto.StudentNationalId && !s.IsDeleted);

            if (student == null)
            {
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

                // Set single field for who modified
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

        // Get holidays by StudentId
        public async Task<List<HolidayDto>> GetHolidaysByStudentIdAsync(string studentId)
        {
            // Find the student by StudentId
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.StudentId == studentId && !s.IsDeleted);

            if (student == null)
            {
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

        // Get holidays by NationalId
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
    }
}