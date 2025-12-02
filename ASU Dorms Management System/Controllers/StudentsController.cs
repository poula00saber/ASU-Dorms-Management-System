
using ASUDorms.Application.DTOs.Students;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ASUDorms.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Registration")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentDto dto)
        {
            try
            {
                // Debug: Log authentication status
                _logger.LogInformation("========== CREATE STUDENT DEBUG ==========");
                _logger.LogInformation($"User Authenticated: {User.Identity?.IsAuthenticated}");
                _logger.LogInformation($"User Name: {User.Identity?.Name}");

                // Log all claims
                _logger.LogInformation("Claims in request:");
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation($"  {claim.Type} = {claim.Value}");
                }
                _logger.LogInformation("==========================================");

                var student = await _studentService.CreateStudentAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = student.StudentId }, student);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"❌ Unauthorized: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"❌ Invalid Operation: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating student");
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                return Ok(student);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                _logger.LogInformation("Getting all students...");
                _logger.LogInformation($"User authenticated: {User.Identity?.IsAuthenticated}");

                var students = await _studentService.GetAllStudentsAsync();
                _logger.LogInformation($"Returned {students.Count} students");
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CreateStudentDto dto)
        {
            try
            {
                var student = await _studentService.UpdateStudentAsync(id, dto);
                return Ok(student);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _studentService.DeleteStudentAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/photo")]
        public async Task<IActionResult> UploadPhoto(string id, IFormFile file)
        {
            try
            {
                var photoUrl = await _studentService.UploadPhotoAsync(file, id);
                return Ok(new { photoUrl });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
