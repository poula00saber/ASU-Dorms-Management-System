using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // ====================================================================
        // REGISTRATION USER ENDPOINTS
        // ====================================================================

        /// <summary>
        /// Get dashboard statistics for registration user's dorm location
        /// </summary>
        [HttpGet("registration/dashboard")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetRegistrationDashboard()
        {
            try
            {
                var dashboardStats = await _reportService.GetRegistrationDashboardStatsAsync();
                return Ok(dashboardStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get daily absence report - shows students who didn't eat today
        /// Can be grouped by building
        /// </summary>
        [HttpGet("daily-absence")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetDailyAbsenceReport([FromQuery] DateTime? date)
        {
            try
            {
                var reportDate = date ?? DateTime.Today;
                var report = await _reportService.GetDailyAbsenceReportAsync(reportDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get monthly absence report - shows all students who missed meals in date range
        /// Includes number of missed meals and dates
        /// </summary>
        [HttpGet("monthly-absence")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetMonthlyAbsenceReport(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                var report = await _reportService.GetMonthlyAbsenceReportAsync(fromDate, toDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get meal absence report for registration users (LEGACY)
        /// Shows students from each building with their missed meals
        /// </summary>
        [HttpGet("meal-absence")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetMealAbsenceReport(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string buildingNumber = null,
            [FromQuery] string government = null,
            [FromQuery] string district = null,
            [FromQuery] string faculty = null)
        {
            var report = await _reportService.GetMealAbsenceReportAsync(
                fromDate, toDate, buildingNumber, government, district, faculty);
            return Ok(report);
        }

        /// <summary>
        /// Get statistics for all buildings
        /// </summary>
        [HttpGet("buildings-statistics")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetBuildingsStatistics(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            var stats = await _reportService.GetAllBuildingsStatisticsAsync(fromDate, toDate);
            return Ok(stats);
        }

        // ====================================================================
        // RESTAURANT USER ENDPOINTS
        // ====================================================================

        /// <summary>
        /// Get today's meal report for restaurant users
        /// Shows: Total meals, Received meals, Remaining meals
        /// </summary>
        [HttpGet("restaurant/today")]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> GetRestaurantTodayReport(
            [FromQuery] string buildingNumber = null)
        {
            var report = await _reportService.GetRestaurantTodayReportAsync(buildingNumber);
            return Ok(report);
        }

        /// <summary>
        /// Get meal report for specific date for restaurant users
        /// </summary>
        [HttpGet("restaurant/daily")]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> GetRestaurantDailyReport(
            [FromQuery] DateTime date,
            [FromQuery] string buildingNumber = null)
        {
            var report = await _reportService.GetRestaurantDailyReportAsync(date, buildingNumber);
            return Ok(report);
        }
    }
}