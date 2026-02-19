using ASUDorms.Application.DTOs.Common;
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

            // ADDED: Get selected dorm location
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during holiday creation");
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

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

            // Find student by NationalId - ADDED dorm location filter
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == dto.StudentNationalId
                    && s.DormLocationId == dormLocationId
                    && !s.IsDeleted);

            if (student == null)
            {
                _logger.LogWarning("Student not found: NationalIdHash={NationalIdHash}, DormLocationId={DormLocationId}",
                    nationalIdHash, dormLocationId);
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
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during holiday deletion");
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            var holiday = await _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .FirstOrDefaultAsync(h => h.Id == holidayId
                    && h.Student.DormLocationId == dormLocationId
                    && !h.IsDeleted);

            if (holiday == null)
            {
                _logger.LogWarning("Holiday not found or already deleted: HolidayId={HolidayId}, DormLocationId={DormLocationId}",
                    holidayId, dormLocationId);
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
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during holiday retrieval");
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            // Find the student by StudentId - ADDED dorm location filter
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.StudentId == studentId
                    && s.DormLocationId == dormLocationId
                    && !s.IsDeleted);

            if (student == null)
            {
                _logger.LogWarning("Student not found: StudentId={StudentId}, DormLocationId={DormLocationId}",
                    studentId, dormLocationId);
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
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during holiday retrieval");
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            // First verify student exists in current dorm
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == nationalId
                    && s.DormLocationId == dormLocationId
                    && !s.IsDeleted);

            if (student == null)
            {
                _logger.LogWarning("Student not found in dorm: NationalIdHash={NationalIdHash}, DormLocationId={DormLocationId}",
                    HashString(nationalId), dormLocationId);
                throw new KeyNotFoundException($"Student with National ID '{nationalId}' not found in this dorm");
            }

            var holidays = await _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.StudentNationalId == nationalId && !h.IsDeleted)
                .OrderByDescending(h => h.StartDate)
                .ToListAsync();

            return holidays.Select(h => MapToDto(h, h.Student)).ToList();
        }

        // ADDED: New method to get all holidays for current dorm
        public async Task<List<HolidayDto>> GetAllHolidaysAsync()
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogDebug("User not associated with dorm location, returning empty holiday list");
                return new List<HolidayDto>();
            }

            _logger.LogDebug("Getting all holidays: DormLocationId={DormLocationId}", dormLocationId);

            var holidays = await _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.Student.DormLocationId == dormLocationId && !h.IsDeleted)
                .OrderByDescending(h => h.StartDate)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} holidays for dorm location {DormLocationId}",
                holidays.Count, dormLocationId);

            return holidays.Select(h => MapToDto(h, h.Student)).ToList();
        }

        // ADDED: New method to get active holidays (current date)
        public async Task<List<HolidayDto>> GetActiveHolidaysAsync()
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();
            var today = DateTime.Today;

            if (dormLocationId == 0)
            {
                _logger.LogDebug("User not associated with dorm location, returning empty holiday list");
                return new List<HolidayDto>();
            }

            _logger.LogDebug("Getting active holidays: DormLocationId={DormLocationId}, Date={Date}",
                dormLocationId, today.ToString("yyyy-MM-dd"));

            var holidays = await _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.Student.DormLocationId == dormLocationId
                    && !h.IsDeleted
                    && h.StartDate <= today
                    && h.EndDate >= today)
                .OrderByDescending(h => h.StartDate)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} active holidays for dorm location {DormLocationId}",
                holidays.Count, dormLocationId);

            return holidays.Select(h => MapToDto(h, h.Student)).ToList();
        }

        // ADDED: New method to search holidays
        public async Task<List<HolidayDto>> SearchHolidaysAsync(string searchTerm)
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogDebug("User not associated with dorm location, returning empty holiday list");
                return new List<HolidayDto>();
            }

            _logger.LogDebug("Searching holidays: SearchTerm={SearchTerm}, DormLocationId={DormLocationId}",
                searchTerm, dormLocationId);

            var holidays = await _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.Student.DormLocationId == dormLocationId
                    && !h.IsDeleted
                    && (h.StudentId.Contains(searchTerm)
                        || h.StudentNationalId.Contains(searchTerm)
                        || (h.Student.FirstName + " " + h.Student.LastName).Contains(searchTerm)))
                .OrderByDescending(h => h.StartDate)
                .ToListAsync();

            _logger.LogDebug("Found {Count} holidays matching search term '{SearchTerm}'",
                holidays.Count, searchTerm);

            return holidays.Select(h => MapToDto(h, h.Student)).ToList();
        }

        // ADDED: Paginated get all holidays for current dorm
        public async Task<PagedResult<HolidayDto>> GetHolidaysPagedAsync(int pageNumber, int pageSize, string? search = null, string? filterStudentId = null)
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogDebug("User not associated with dorm location, returning empty paged result");
                return new PagedResult<HolidayDto>(new List<HolidayDto>(), 0, pageNumber, pageSize);
            }

            // Validate page size
            if (pageSize > 100) pageSize = 100;
            if (pageSize < 1) pageSize = 10;

            _logger.LogDebug("Getting paginated holidays: DormLocationId={DormLocationId}, Page={Page}, PageSize={PageSize}",
                dormLocationId, pageNumber, pageSize);

            var query = _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.Student.DormLocationId == dormLocationId && !h.IsDeleted);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(h => 
                    h.StudentId.Contains(search)
                    || h.StudentNationalId.Contains(search)
                    || (h.Student.FirstName + " " + h.Student.LastName).Contains(search));
            }

            // Apply student ID filter
            if (!string.IsNullOrWhiteSpace(filterStudentId))
            {
                query = query.Where(h => h.StudentId == filterStudentId);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var holidays = await query
                .OrderByDescending(h => h.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var holidayDtos = holidays.Select(h => MapToDto(h, h.Student)).ToList();

            return new PagedResult<HolidayDto>(holidayDtos, totalCount, pageNumber, pageSize);
        }

        // ADDED: Paginated get holidays for specific student
        public async Task<PagedResult<HolidayDto>> GetStudentHolidaysPagedAsync(string studentId, int pageNumber, int pageSize)
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogDebug("User not associated with dorm location, returning empty paged result");
                return new PagedResult<HolidayDto>(new List<HolidayDto>(), 0, pageNumber, pageSize);
            }

            // Validate page size
            if (pageSize > 100) pageSize = 100;
            if (pageSize < 1) pageSize = 10;

            _logger.LogDebug("Getting paginated student holidays: StudentId={StudentId}, DormLocationId={DormLocationId}, Page={Page}, PageSize={PageSize}",
                studentId, dormLocationId, pageNumber, pageSize);

            var query = _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.Student.DormLocationId == dormLocationId
                    && h.StudentId == studentId
                    && !h.IsDeleted);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var holidays = await query
                .OrderByDescending(h => h.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var holidayDtos = holidays.Select(h => MapToDto(h, h.Student)).ToList();

            return new PagedResult<HolidayDto>(holidayDtos, totalCount, pageNumber, pageSize);
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