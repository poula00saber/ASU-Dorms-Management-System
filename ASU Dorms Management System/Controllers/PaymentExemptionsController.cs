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
    public class PaymentExemptionsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentExemptionsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentExemptionDto dto)
        {
            try
            {
                var exemption = await _paymentService.CreatePaymentExemptionAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = exemption.Id }, exemption);
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
            try
            {
                var exemption = await _paymentService.UpdatePaymentExemptionAsync(id, dto);
                return Ok(exemption);
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
            try
            {
                var exemption = await _paymentService.GetPaymentExemptionByIdAsync(id);
                return Ok(exemption);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
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
            try
            {
                var exemptions = await _paymentService.GetPaymentExemptionsByStudentAsync(studentNationalId);
                return Ok(exemptions);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
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
            try
            {
                var exemptions = await _paymentService.GetPaymentExemptionsAsync(filter);
                return Ok(exemptions);
            }
            catch (Exception ex)
            {
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
            try
            {
                await _paymentService.TogglePaymentExemptionStatusAsync(id, isActive);
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
                    message = "حدث خطأ أثناء تغيير حالة الإعفاء",
                    error = ex.Message
                });
            }
        }

        [HttpGet("validate/{studentNationalId}")]
        public async Task<IActionResult> Validate(string studentNationalId, [FromQuery] DateTime date)
        {
            try
            {
                var isValid = await _paymentService.IsPaymentExemptionValidAsync(studentNationalId, date);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
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
            try
            {
                await _paymentService.DeletePaymentExemptionAsync(id);
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
                    message = "حدث خطأ أثناء حذف الإعفاء",
                    error = ex.Message
                });
            }
        }
    }
}