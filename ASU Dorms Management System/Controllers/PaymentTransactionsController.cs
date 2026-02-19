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
    [Authorize(Roles = "Registration,User")]
    public class PaymentTransactionsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentTransactionsController> _logger;

        public PaymentTransactionsController(IPaymentService paymentService, ILogger<PaymentTransactionsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentTransactionDto dto)
        {
            var nationalIdHash = HashString(dto.StudentNationalId);

            _logger.LogInformation("Creating payment transaction: NationalIdHash={NationalIdHash}, Amount={Amount}, Type={PaymentType}",
                nationalIdHash, dto.Amount, dto.PaymentType);

            try
            {
                var payment = await _paymentService.CreatePaymentTransactionAsync(dto);

                _logger.LogInformation("Payment transaction created successfully: PaymentId={PaymentId}", payment.Id);

                return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found for payment: NationalIdHash={NationalIdHash}", nationalIdHash);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid payment request: NationalIdHash={NationalIdHash}, Error={ErrorMessage}",
                    nationalIdHash, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment transaction: NationalIdHash={NationalIdHash}", nationalIdHash);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء إضافة عملية الدفع",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogDebug("Getting payment transaction: PaymentId={PaymentId}", id);

            try
            {
                var payment = await _paymentService.GetPaymentTransactionByIdAsync(id);
                return Ok(payment);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Payment transaction not found: PaymentId={PaymentId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment transaction: PaymentId={PaymentId}", id);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب عملية الدفع",
                    error = ex.Message
                });
            }
        }

        [HttpGet("student/{studentNationalId}")]
        public async Task<IActionResult> GetByStudent(string studentNationalId)
        {
            var nationalIdHash = HashString(studentNationalId);

            _logger.LogDebug("Getting payment transactions for student: NationalIdHash={NationalIdHash}", nationalIdHash);

            try
            {
                var payments = await _paymentService.GetPaymentTransactionsByStudentAsync(studentNationalId);
                return Ok(payments);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found: NationalIdHash={NationalIdHash}", nationalIdHash);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment transactions: NationalIdHash={NationalIdHash}", nationalIdHash);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب عمليات الدفع",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaymentFilterDto filter)
        {
            _logger.LogDebug("Getting all payment transactions with filter");

            try
            {
                var payments = await _paymentService.GetPaymentTransactionsAsync(filter);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all payment transactions");
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب عمليات الدفع",
                    error = ex.Message
                });
            }
        }

        [HttpGet("summary/{studentNationalId}")]
        public async Task<IActionResult> GetSummary(string studentNationalId)
        {
            var nationalIdHash = HashString(studentNationalId);

            _logger.LogDebug("Getting payment summary for student: NationalIdHash={NationalIdHash}", nationalIdHash);

            try
            {
                var summary = await _paymentService.GetPaymentSummaryByStudentAsync(studentNationalId);
                return Ok(summary);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Student not found for summary: NationalIdHash={NationalIdHash}", nationalIdHash);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment summary: NationalIdHash={NationalIdHash}", nationalIdHash);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب ملخص الدفع",
                    error = ex.Message
                });
            }
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null,
            [FromQuery] string? studentId = null)
        {
            _logger.LogDebug("Getting paginated payment transactions: Page={Page}, PageSize={PageSize}",
                page?.ToString() ?? "null", pageSize?.ToString() ?? "null");

            try
            {
                var pageNum = page ?? 1;
                if (pageNum < 1) pageNum = 1;

                var size = pageSize ?? 10;
                if (size < 1) size = 10;
                if (size > 100) size = 100;

                var pagedResult = await _paymentService.GetPaymentTransactionsPagedAsync(pageNum, size, search, studentId);

                _logger.LogDebug("Returned page {Page} with {Count}/{Total} payment transactions",
                    pageNum, pagedResult.Items.Count, pagedResult.TotalCount);

                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated payment transactions");
                return StatusCode(500, new { message = "An error occurred while fetching payment transactions", error = ex.Message });
            }
        }

        [HttpGet("student/{studentId}/paged")]
        public async Task<IActionResult> GetByStudentIdPaged(
            string studentId,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null)
        {
            _logger.LogDebug("Getting paginated payment transactions for student: StudentId={StudentId}, Page={Page}, PageSize={PageSize}",
                studentId, page?.ToString() ?? "null", pageSize?.ToString() ?? "null");

            try
            {
                var pageNum = page ?? 1;
                if (pageNum < 1) pageNum = 1;

                var size = pageSize ?? 10;
                if (size < 1) size = 10;
                if (size > 100) size = 100;

                var pagedResult = await _paymentService.GetStudentPaymentTransactionsPagedAsync(studentId, pageNum, size);

                _logger.LogDebug("Returned page {Page} with {Count}/{Total} payment transactions for student {StudentId}",
                    pageNum, pagedResult.Items.Count, pagedResult.TotalCount, studentId);

                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated payment transactions for student: StudentId={StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while fetching payment transactions", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting payment transaction: PaymentId={PaymentId}", id);

            try
            {
                await _paymentService.DeletePaymentTransactionAsync(id);

                _logger.LogInformation("Payment transaction deleted successfully: PaymentId={PaymentId}", id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Payment transaction not found for deletion: PaymentId={PaymentId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment transaction: PaymentId={PaymentId}", id);
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء حذف عملية الدفع",
                    error = ex.Message
                });
            }
        }

        // BULK MONTHLY FEES ENDPOINTS

        [HttpGet("bulk/available-dorm-types")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetAvailableDormTypesForBulkFees()
        {
            _logger.LogInformation("Getting available dorm types for bulk fees");

            try
            {
                var dormTypes = await _paymentService.GetAvailableDormTypesForBulkFeesAsync();

                _logger.LogInformation("Returned {Count} dorm types for bulk fees", dormTypes.Count);
                return Ok(dormTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dorm types for bulk fees");
                return StatusCode(500, new { message = "خطأ في الحصول على أنواع السكن", error = ex.Message });
            }
        }

        [HttpPost("bulk/add-monthly-fees")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> BulkAddMonthlyFees([FromBody] BulkMonthlyFeesDto dto)
        {
            _logger.LogInformation("Adding bulk monthly fees: Month={Month}, DormTypes={DormTypeCount}",
                dto.Month, dto.DormTypeAmounts.Count);

            try
            {
                var result = await _paymentService.BulkAddMonthlyFeesAsync(dto);

                if (result.Success)
                {
                    _logger.LogInformation("Bulk monthly fees added successfully: Total={Total}, Amount={Amount}",
                        result.TotalStudentsProcessed, result.TotalAmountAdded);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Bulk monthly fees partially failed: {Message}", result.Message);
                    return BadRequest(result);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid bulk fees request: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized bulk fees request: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding bulk monthly fees");
                return StatusCode(500, new { message = "خطأ في إضافة الرسوم", error = ex.Message });
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