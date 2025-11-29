using ASUDorms.Application.DTOs.Meals;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Restaurant")]
    public class MealsController : ControllerBase
    {
        private readonly IMealService _mealService;

        public MealsController(IMealService mealService)
        {
            _mealService = mealService;
        }

        [HttpPost("scan")]
        public async Task<IActionResult> ScanMeal([FromBody] MealScanRequestDto request)
        {
            var result = await _mealService.ScanMealAsync(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpGet("time-valid/{mealTypeId}")]
        public async Task<IActionResult> IsTimeValid(int mealTypeId)
        {
            var isValid = await _mealService.IsTimeValidForMealTypeAsync(mealTypeId);
            return Ok(new { isValid });
        }
    }
}
