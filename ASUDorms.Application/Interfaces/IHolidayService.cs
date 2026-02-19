using ASUDorms.Application.DTOs.Common;
using ASUDorms.Application.DTOs.Holidays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.Interfaces
{
    public interface IHolidayService
    {
        Task<HolidayDto> CreateHolidayAsync(CreateHolidayDto dto);
        Task DeleteHolidayAsync(int holidayId);
        Task<List<HolidayDto>> GetHolidaysByStudentIdAsync(string studentId);
        Task<List<HolidayDto>> GetHolidaysByNationalIdAsync(string nationalId);
        Task<List<HolidayDto>> GetAllHolidaysAsync();
        Task<List<HolidayDto>> GetActiveHolidaysAsync();
        Task<List<HolidayDto>> SearchHolidaysAsync(string searchTerm);
        Task<PagedResult<HolidayDto>> GetHolidaysPagedAsync(int pageNumber, int pageSize, string? search = null, string? filterStudentId = null);
        Task<PagedResult<HolidayDto>> GetStudentHolidaysPagedAsync(string studentId, int pageNumber, int pageSize);
    }
}
