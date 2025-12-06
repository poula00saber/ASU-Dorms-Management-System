using ASUDorms.Application.DTOs.Holidays;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Registration")]
    public class HolidaysController : ControllerBase
    {
        private readonly IHolidayService _holidayService;

        public HolidaysController(IHolidayService holidayService)
        {
            _holidayService = holidayService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHolidayDto dto)
        {
            try
            {
                var holiday = await _holidayService.CreateHolidayAsync(dto);
                return CreatedAtAction(nameof(GetByStudentId),
                    new { studentId = holiday.StudentId },
                    holiday);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the holiday", error = ex.Message });
            }
        }

        // For frontend: Get holidays by StudentId
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudentId(string studentId)
        {
            try
            {
                var holidays = await _holidayService.GetHolidaysByStudentIdAsync(studentId);
                return Ok(holidays);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching holidays", error = ex.Message });
            }
        }

        // Alternative endpoint: Get holidays by NationalId
        [HttpGet("national-id/{nationalId}")]
        public async Task<IActionResult> GetByNationalId(string nationalId)
        {
            try
            {
                var holidays = await _holidayService.GetHolidaysByNationalIdAsync(nationalId);
                return Ok(holidays);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching holidays", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _holidayService.DeleteHolidayAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the holiday", error = ex.Message });
            }
        }
    }
}