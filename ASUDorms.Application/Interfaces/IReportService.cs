using ASUDorms.Application.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.Interfaces
{
    public interface IReportService
    {
        Task<MealAbsenceReportDto> GetMealAbsenceReportAsync(
            DateTime date,
            string buildingNumber = null,
            string government = null,
            string district = null,
            string faculty = null);
    }
}
