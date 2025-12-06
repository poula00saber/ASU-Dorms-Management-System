using ASUDorms.Application.DTOs.Holidays;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            // Find student by NationalId (primary key for Student)
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == dto.StudentNationalId && !s.IsDeleted);

            if (student == null)
            {
                throw new KeyNotFoundException($"Student with National ID '{dto.StudentNationalId}' not found or is deleted");
            }

            // Check for date overlaps with active holidays only
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

            var holiday = new Holiday
            {
                StudentNationalId = dto.StudentNationalId, // Foreign key
                StudentId = student.StudentId, // For easy lookup by StudentId
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
            };

            await _unitOfWork.Holidays.AddAsync(holiday);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(holiday, student);
        }

        // Get holidays by StudentId (for frontend display)
        public async Task<List<HolidayDto>> GetHolidaysByStudentIdAsync(string studentId)
        {
            // First find the student by StudentId to get their NationalId
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.StudentId == studentId && !s.IsDeleted);

            if (student == null)
            {
                throw new KeyNotFoundException($"Student with Student ID '{studentId}' not found");
            }

            // Then get holidays by NationalId (foreign key)
            var holidays = await _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.StudentNationalId == student.NationalId && !h.IsDeleted)
                .OrderByDescending(h => h.StartDate)
                .ToListAsync();

            return holidays.Select(h => MapToDto(h, h.Student)).ToList();
        }

        // Alternative: Get holidays directly by NationalId
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

        public async Task DeleteHolidayAsync(int holidayId)
        {
            var holiday = await _unitOfWork.Holidays
                .Query()
                .FirstOrDefaultAsync(h => h.Id == holidayId && !h.IsDeleted);

            if (holiday == null)
            {
                throw new KeyNotFoundException($"Active holiday with ID {holidayId} not found");
            }

            // Soft delete
            holiday.IsDeleted = true;

            _unitOfWork.Holidays.Update(holiday);
            await _unitOfWork.SaveChangesAsync();
        }

        private HolidayDto MapToDto(Holiday holiday, Student student = null)
        {
            return new HolidayDto
            {
                Id = holiday.Id,
                StudentId = holiday.StudentId, // Include StudentId for frontend
                StudentNationalId = holiday.StudentNationalId,
                StartDate = holiday.StartDate,
                EndDate = holiday.EndDate,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : null
            };
        }
    }
}