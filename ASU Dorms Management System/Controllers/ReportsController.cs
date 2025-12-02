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
        /// Get meal absence report for registration users
        /// Shows students from each building with their missed meals
        /// </summary>
        /// 


        // deeeeeeeeeeeeep seeeeeeeeeek

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
