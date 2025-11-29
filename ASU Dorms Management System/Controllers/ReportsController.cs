using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Registration")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("meal-absence")]
        public async Task<IActionResult> GetMealAbsenceReport(
            [FromQuery] DateTime date,
            [FromQuery] string buildingNumber = null,
            [FromQuery] string government = null,
            [FromQuery] string district = null,
            [FromQuery] string faculty = null)
        {
            var report = await _reportService.GetMealAbsenceReportAsync(
                date, buildingNumber, government, district, faculty);

            return Ok(report);
        }
    }
}
