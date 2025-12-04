using ASUDorms.Application.DTOs.Holidays;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
                return CreatedAtAction(nameof(GetByStudent), new { studentId = dto.StudentNationalId }, holiday);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(string studentId)
        {
            var holidays = await _holidayService.GetHolidaysByStudentAsync(studentId);
            return Ok(holidays);
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
        }
    }
}
