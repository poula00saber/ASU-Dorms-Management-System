using ASUDorms.Application.DTOs.Payments;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Registration")]
    public class PaymentExemptionsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentExemptionsController> _logger;

        public PaymentExemptionsController(IPaymentService paymentService, ILogger<PaymentExemptionsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentExemptionDto dto)
        {
            var nationalIdHash = HashString(dto.StudentNationalId);

            _logger.LogInformation("Creating payment exemption: NationalIdHash={NationalIdHash}, StartDate={StartDate}, EndDate={EndDate}",
                nationalIdHash, dto.StartDate.ToString("yyyy-MM-dd"), dto.EndDate.ToString("yyyy-MM-dd"));

            try
            {
                var exemption = await _paymentService.CreatePaymentExemptionAsync(dto);

                _logger.LogInformation("Payment exemption created successfully: ExemptionId={ExemptionId}", exemption.Id);

                return CreatedAtAction(nameof(GetById), new { id = exemption.Id }, exemption);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found for exemption: NationalIdHash={NationalIdHash}", nationalIdHash);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid exemption request: NationalIdHash={NationalIdHash}, Error={ErrorMessage}",
                    nationalIdHash, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Exemption date overlap: NationalIdHash={NationalIdHash}, Error={ErrorMessage}",
                    nationalIdHash, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment exemption: NationalIdHash={NationalIdHash}", nationalIdHash);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء إضافة الإعفاء",
                    error = ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentExemptionDto dto)
        {
            _logger.LogInformation("Updating payment exemption: ExemptionId={ExemptionId}", id);

            try
            {
                var exemption = await _paymentService.UpdatePaymentExemptionAsync(id, dto);

                _logger.LogInformation("Payment exemption updated successfully: ExemptionId={ExemptionId}", id);

                return Ok(exemption);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Exemption not found for update: ExemptionId={ExemptionId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid exemption update: ExemptionId={ExemptionId}, Error={ErrorMessage}",
                    id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Exemption date overlap on update: ExemptionId={ExemptionId}, Error={ErrorMessage}",
                    id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment exemption: ExemptionId={ExemptionId}", id);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء تحديث الإعفاء",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogDebug("Getting payment exemption: ExemptionId={ExemptionId}", id);

            try
            {
                var exemption = await _paymentService.GetPaymentExemptionByIdAsync(id);
                return Ok(exemption);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Exemption not found: ExemptionId={ExemptionId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment exemption: ExemptionId={ExemptionId}", id);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب الإعفاء",
                    error = ex.Message
                });
            }
        }

        [HttpGet("student/{studentNationalId}")]
        public async Task<IActionResult> GetByStudent(string studentNationalId)
        {
            var nationalIdHash = HashString(studentNationalId);

            _logger.LogDebug("Getting payment exemptions for student: NationalIdHash={NationalIdHash}", nationalIdHash);

            try
            {
                var exemptions = await _paymentService.GetPaymentExemptionsByStudentAsync(studentNationalId);
                return Ok(exemptions);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found: NationalIdHash={NationalIdHash}", nationalIdHash);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment exemptions: NationalIdHash={NationalIdHash}", nationalIdHash);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب الإعفاءات",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaymentExemptionFilterDto filter)
        {
            _logger.LogDebug("Getting all payment exemptions with filter");

            try
            {
                var exemptions = await _paymentService.GetPaymentExemptionsAsync(filter);
                return Ok(exemptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all payment exemptions");
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب الإعفاءات",
                    error = ex.Message
                });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
        {
            _logger.LogInformation("Toggling exemption status: ExemptionId={ExemptionId}, IsActive={IsActive}", id, isActive);

            try
            {
                await _paymentService.TogglePaymentExemptionStatusAsync(id, isActive);

                _logger.LogInformation("Exemption status updated: ExemptionId={ExemptionId}, IsActive={IsActive}", id, isActive);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Exemption not found for status toggle: ExemptionId={ExemptionId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling exemption status: ExemptionId={ExemptionId}", id);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء تغيير حالة الإعفاء",
                    error = ex.Message
                });
            }
        }

        [HttpGet("validate/{studentNationalId}")]
        public async Task<IActionResult> Validate(string studentNationalId, [FromQuery] DateTime date)
        {
            var nationalIdHash = HashString(studentNationalId);

            _logger.LogDebug("Validating exemption: NationalIdHash={NationalIdHash}, Date={Date}",
                nationalIdHash, date.ToString("yyyy-MM-dd"));

            try
            {
                var isValid = await _paymentService.IsPaymentExemptionValidAsync(studentNationalId, date);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exemption: NationalIdHash={NationalIdHash}", nationalIdHash);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء التحقق من صحة الإعفاء",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Admin deleting payment exemption: ExemptionId={ExemptionId}", id);

            try
            {
                await _paymentService.DeletePaymentExemptionAsync(id);

                _logger.LogInformation("Payment exemption deleted by admin: ExemptionId={ExemptionId}", id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Exemption not found for admin deletion: ExemptionId={ExemptionId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment exemption: ExemptionId={ExemptionId}", id);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء حذف الإعفاء",
                    error = ex.Message
                });
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