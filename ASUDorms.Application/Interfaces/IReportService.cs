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
        // Registration User Reports
        Task<MealAbsenceReportDto> GetMealAbsenceReportAsync(
            DateTime fromDate,
            DateTime toDate,
            string buildingNumber = null,
            string government = null,
            string district = null,
            string faculty = null);

        Task<AllBuildingsStatisticsDto> GetAllBuildingsStatisticsAsync(
            DateTime fromDate,
            DateTime toDate);

        // Restaurant User Reports
        Task<RestaurantDailyReportDto> GetRestaurantDailyReportAsync(
            DateTime date,
            string buildingNumber = null);

        Task<RestaurantDailyReportDto> GetRestaurantTodayReportAsync(
            string buildingNumber = null);
        Task<RegistrationDashboardDto> GetRegistrationDashboardStatsAsync(); // New method
        Task<DailyAbsenceReportDto> GetDailyAbsenceReportAsync(DateTime date);
        Task<MonthlyAbsenceReportDto> GetMonthlyAbsenceReportAsync(DateTime fromDate, DateTime toDate);


    }
}
