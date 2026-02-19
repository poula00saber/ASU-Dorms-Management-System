using ASUDorms.Application.DTOs.Students;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ASUDorms.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Registration,User")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        [HttpGet("print-data")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetStudentsForPrinting([FromQuery] int? dormLocationId = null)
        {
            _logger.LogInformation("Getting students for printing: DormLocationId={DormLocationId}",
                dormLocationId?.ToString() ?? "Current");

            try
            {
                List<StudentDto> students;

                if (dormLocationId.HasValue && dormLocationId.Value > 0)
                {
                    students = await _studentService.GetStudentsByDormLocationAsync(dormLocationId.Value);
                }
                else
                {
                    students = await _studentService.GetAllStudentsAsync();
                }

                _logger.LogInformation("Returned {Count} students for printing", students.Count);
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students for printing");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentDto dto)
        {
            var nationalIdHash = HashString(dto.NationalId);

            _logger.LogInformation("Creating student: StudentId={StudentId}, NationalIdHash={NationalIdHash}",
                dto.StudentId, nationalIdHash);

            try
            {
                var student = await _studentService.CreateStudentAsync(dto);

                _logger.LogInformation("Student created successfully: StudentId={StudentId}", student.StudentId);

                return CreatedAtAction(nameof(GetById), new { id = student.StudentId }, student);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized student creation: StudentId={StudentId}, Error={ErrorMessage}",
                    dto.StudentId, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid student creation: StudentId={StudentId}, Error={ErrorMessage}",
                    dto.StudentId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student: StudentId={StudentId}", dto.StudentId);
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            _logger.LogDebug("Getting student: StudentId={StudentId}", id);

            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                return Ok(student);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found: StudentId={StudentId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized student access: StudentId={StudentId}, Error={ErrorMessage}",
                    id, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null,
            [FromQuery] string? building = null,
            [FromQuery] string? faculty = null)
        {
            try
            {
                // If page is specified, return paginated results
                if (page.HasValue)
                {
                    var pageNum = page.Value < 1 ? 1 : page.Value;
                    var size = pageSize.HasValue && pageSize.Value > 0 ? pageSize.Value : 10;
                    if (size > 100) size = 100;

                    var pagedResult = await _studentService.GetStudentsPagedAsync(pageNum, size, search, building, faculty);

                    _logger.LogDebug("Returned page {Page} with {Count}/{Total} students",
                        pageNum, pagedResult.Items.Count, pagedResult.TotalCount);

                    return Ok(pagedResult);
                }

                // Otherwise return all (backward compatible)
                var students = await _studentService.GetAllStudentsAsync();
                _logger.LogDebug("Returned {Count} students (unpaged)", students.Count);
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
            var nationalIdHash = HashString(dto.NationalId);

            _logger.LogInformation("Updating student: StudentId={StudentId}, NationalIdHash={NationalIdHash}",
                id, nationalIdHash);

            try
            {
                var student = await _studentService.UpdateStudentAsync(id, dto);

                _logger.LogInformation("Student updated successfully: StudentId={StudentId}", id);

                return Ok(student);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found for update: StudentId={StudentId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid student update: StudentId={StudentId}, Error={ErrorMessage}",
                    id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized student update: StudentId={StudentId}, Error={ErrorMessage}",
                    id, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation("Deleting student: StudentId={StudentId}", id);

            try
            {
                await _studentService.DeleteStudentAsync(id);

                _logger.LogInformation("Student deleted successfully: StudentId={StudentId}", id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found for deletion: StudentId={StudentId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized student deletion: StudentId={StudentId}, Error={ErrorMessage}",
                    id, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/photo")]
        public async Task<IActionResult> UploadPhoto(string id, IFormFile file)
        {
            _logger.LogInformation("Uploading photo for student: StudentId={StudentId}, FileName={FileName}, FileSize={FileSize}",
                id, file?.FileName, file?.Length);

            try
            {
                var photoUrl = await _studentService.UploadPhotoAsync(file, id);

                _logger.LogInformation("Photo uploaded successfully: StudentId={StudentId}, PhotoUrl={PhotoUrl}",
                    id, photoUrl);

                return Ok(new { photoUrl });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid photo upload: StudentId={StudentId}, Error={ErrorMessage}", id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found for photo upload: StudentId={StudentId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized photo upload: StudentId={StudentId}, Error={ErrorMessage}",
                    id, ex.Message);
                return Unauthorized(new { message = ex.Message });
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