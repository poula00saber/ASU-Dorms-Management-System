using ASUDorms.Application.DTOs.Holidays;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Registration,User")]
    public class HolidaysController : ControllerBase
    {
        private readonly IHolidayService _holidayService;
        private readonly ILogger<HolidaysController> _logger;

        public HolidaysController(IHolidayService holidayService, ILogger<HolidaysController> logger)
        {
            _holidayService = holidayService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHolidayDto dto)
        {
            var nationalIdHash = HashString(dto.StudentNationalId);

            _logger.LogInformation("Creating holiday: NationalIdHash={NationalIdHash}, StartDate={StartDate}, EndDate={EndDate}",
                nationalIdHash, dto.StartDate.ToString("yyyy-MM-dd"), dto.EndDate.ToString("yyyy-MM-dd"));

            try
            {
                var holiday = await _holidayService.CreateHolidayAsync(dto);

                _logger.LogInformation("Holiday created: Id={HolidayId}, StudentId={StudentId}",
                    holiday.Id, holiday.StudentId);

                return CreatedAtAction(nameof(GetByStudentId),
                    new { studentId = holiday.StudentId },
                    holiday);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found: NationalIdHash={NationalIdHash}", nationalIdHash);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid holiday request: NationalIdHash={NationalIdHash}, Error={ErrorMessage}",
                    nationalIdHash, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Date overlap: NationalIdHash={NationalIdHash}, Error={ErrorMessage}",
                    nationalIdHash, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating holiday: NationalIdHash={NationalIdHash}", nationalIdHash);
                return StatusCode(500, new { message = "An error occurred while creating the holiday", error = ex.Message });
            }
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudentId(string studentId)
        {
            _logger.LogDebug("Getting holidays for student: StudentId={StudentId}", studentId);

            try
            {
                var holidays = await _holidayService.GetHolidaysByStudentIdAsync(studentId);

                _logger.LogDebug("Retrieved {Count} holidays for student: StudentId={StudentId}",
                    holidays.Count, studentId);

                return Ok(holidays);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found: StudentId={StudentId}", studentId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching holidays: StudentId={StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while fetching holidays", error = ex.Message });
            }
        }

        [HttpGet("national-id/{nationalId}")]
        public async Task<IActionResult> GetByNationalId(string nationalId)
        {
            var nationalIdHash = HashString(nationalId);

            _logger.LogDebug("Getting holidays: NationalIdHash={NationalIdHash}", nationalIdHash);

            try
            {
                var holidays = await _holidayService.GetHolidaysByNationalIdAsync(nationalId);

                _logger.LogDebug("Retrieved {Count} holidays: NationalIdHash={NationalIdHash}",
                    holidays.Count, nationalIdHash);

                return Ok(holidays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching holidays: NationalIdHash={NationalIdHash}", nationalIdHash);
                return StatusCode(500, new { message = "An error occurred while fetching holidays", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting holiday: HolidayId={HolidayId}", id);

            try
            {
                await _holidayService.DeleteHolidayAsync(id);

                _logger.LogInformation("Holiday deleted: HolidayId={HolidayId}", id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Holiday not found: HolidayId={HolidayId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting holiday: HolidayId={HolidayId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the holiday", error = ex.Message });
            }
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