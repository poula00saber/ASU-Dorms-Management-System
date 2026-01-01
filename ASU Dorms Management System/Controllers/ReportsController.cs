using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        // ====================================================================
        // REGISTRATION USER ENDPOINTS
        // ====================================================================

        [HttpGet("registration/dashboard")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetRegistrationDashboard()
        {
            _logger.LogDebug("Getting registration dashboard");

            try
            {
                var dashboardStats = await _reportService.GetRegistrationDashboardStatsAsync();
                return Ok(dashboardStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registration dashboard");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("daily-absence")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetDailyAbsenceReport([FromQuery] DateTime? date)
        {
            var reportDate = date ?? DateTime.Today;

            _logger.LogDebug("Getting daily absence report: Date={Date}", reportDate.ToString("yyyy-MM-dd"));

            try
            {
                var report = await _reportService.GetDailyAbsenceReportAsync(reportDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily absence report: Date={Date}", reportDate.ToString("yyyy-MM-dd"));
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("monthly-absence")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetMonthlyAbsenceReport(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            _logger.LogDebug("Getting monthly absence report: FromDate={FromDate}, ToDate={ToDate}",
                fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));

            try
            {
                var report = await _reportService.GetMonthlyAbsenceReportAsync(fromDate, toDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly absence report: FromDate={FromDate}, ToDate={ToDate}",
                    fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));
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
            _logger.LogDebug("Getting meal absence report: FromDate={FromDate}, ToDate={ToDate}, Building={BuildingNumber}",
                fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"), buildingNumber ?? "All");

            try
            {
                var report = await _reportService.GetMealAbsenceReportAsync(
                    fromDate, toDate, buildingNumber, government, district, faculty);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal absence report: FromDate={FromDate}, ToDate={ToDate}",
                    fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("buildings-statistics")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetBuildingsStatistics(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            _logger.LogDebug("Getting buildings statistics: FromDate={FromDate}, ToDate={ToDate}",
                fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));

            try
            {
                var stats = await _reportService.GetAllBuildingsStatisticsAsync(fromDate, toDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting buildings statistics: FromDate={FromDate}, ToDate={ToDate}",
                    fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ====================================================================
        // RESTAURANT USER ENDPOINTS
        // ====================================================================

        [HttpGet("restaurant/today")]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> GetRestaurantTodayReport(
            [FromQuery] string buildingNumber = null)
        {
            _logger.LogDebug("Getting restaurant today report: Building={BuildingNumber}", buildingNumber ?? "All");

            try
            {
                var report = await _reportService.GetRestaurantTodayReportAsync(buildingNumber);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting restaurant today report: Building={BuildingNumber}", buildingNumber ?? "All");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("restaurant/daily")]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> GetRestaurantDailyReport(
            [FromQuery] DateTime date,
            [FromQuery] string buildingNumber = null)
        {
            _logger.LogDebug("Getting restaurant daily report: Date={Date}, Building={BuildingNumber}",
                date.ToString("yyyy-MM-dd"), buildingNumber ?? "All");

            try
            {
                var report = await _reportService.GetRestaurantDailyReportAsync(date, buildingNumber);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting restaurant daily report: Date={Date}, Building={BuildingNumber}",
                    date.ToString("yyyy-MM-dd"), buildingNumber ?? "All");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}