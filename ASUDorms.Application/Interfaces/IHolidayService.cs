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
        Task<List<HolidayDto>> GetHolidaysByStudentIdAsync(string studentId);
        Task<List<HolidayDto>> GetHolidaysByNationalIdAsync(string nationalId);
        Task DeleteHolidayAsync(int holidayId);
    }
}
