using ASUDorms.Application.DTOs.Payments;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Registration")]
    public class PaymentTransactionsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentTransactionsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentTransactionDto dto)
        {
            try
            {
                var payment = await _paymentService.CreatePaymentTransactionAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
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
            try
            {
                var payment = await _paymentService.GetPaymentTransactionByIdAsync(id);
                return Ok(payment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
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
            try
            {
                var payments = await _paymentService.GetPaymentTransactionsByStudentAsync(studentNationalId);
                return Ok(payments);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
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
            try
            {
                var payments = await _paymentService.GetPaymentTransactionsAsync(filter);
                return Ok(payments);
            }
            catch (Exception ex)
            {
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
            try
            {
                var summary = await _paymentService.GetPaymentSummaryByStudentAsync(studentNationalId);
                return Ok(summary);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء جلب ملخص الدفع",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _paymentService.DeletePaymentTransactionAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "حدث خطأ أثناء حذف عملية الدفع",
                    error = ex.Message
                });
            }
        }
    }
}