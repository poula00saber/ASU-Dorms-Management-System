using ASUDorms.Application.DTOs.Holidays;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
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
            // Verify student exists and belongs to current location
            var student = await _unitOfWork.Students.GetByIdAsync(dto.StudentNationalId);
            if (student == null)
            {
                throw new KeyNotFoundException("Student not found");
            }

            var holiday = new Holiday
            {
                StudentNationalId = dto.StudentNationalId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
            };

            await _unitOfWork.Holidays.AddAsync(holiday);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(holiday);
        }

        public async Task<List<HolidayDto>> GetHolidaysByStudentAsync(string studentId)
        {
            var holidays = await _unitOfWork.Holidays.FindAsync(h => h.StudentNationalId == studentId);
            return holidays.Select(MapToDto).ToList();
        }

        public async Task DeleteHolidayAsync(int holidayId)
        {
            var holiday = await _unitOfWork.Holidays.GetByIdAsync(holidayId);
            if (holiday == null)
            {
                throw new KeyNotFoundException("Holiday not found");
            }

            _unitOfWork.Holidays.Delete(holiday);
            await _unitOfWork.SaveChangesAsync();
        }

        private HolidayDto MapToDto(Holiday holiday)
        {
            return new HolidayDto
            {
                Id = holiday.Id,
                StudentNationalId = holiday.StudentNationalId,
                StartDate = holiday.StartDate,
                EndDate = holiday.EndDate,
            };
        }
    }
}
