using ASUDorms.Application.DTOs.Meals;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MealsController : ControllerBase
    {
        private readonly IMealService _mealService;

        public MealsController(IMealService mealService)
        {
            _mealService = mealService;
        }

        /// <summary>
        /// Scan a single meal (Breakfast/Dinner OR Lunch)
        /// </summary>
        [HttpPost("scan")]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> ScanMeal([FromBody] MealScanRequestDto request)
        {
            var result = await _mealService.ScanMealAsync(request);
            if (result.Success)
                return Ok(result);
            return BadRequest(result);
        }

        /// <summary>
        /// Scan both meals together (Breakfast/Dinner + Lunch)
        /// Only available during lunch time (1:00 PM - 9:00 PM)
        /// Must be enabled by Registration user
        /// </summary>
        [HttpPost("scan-combined")]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> ScanCombinedMeal([FromBody] MealScanRequestDto request)
        {
            var result = await _mealService.ScanCombinedMealAsync(request);
            if (result.Success)
                return Ok(result);
            return BadRequest(result);
        }

        /// <summary>
        /// Check if current time is valid for a specific meal type
        /// </summary>
        [HttpGet("time-valid/{mealTypeId}")]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> IsTimeValid(int mealTypeId)
        {
            var isValid = await _mealService.IsTimeValidForMealTypeAsync(mealTypeId);
            return Ok(new { isValid });
        }

        /// <summary>
        /// Get meal settings for current dorm location
        /// Used by Restaurant to know if combined scanning is allowed
        /// </summary>
        [HttpGet("settings")]
        [Authorize(Roles = "Restaurant,Registration")]
        public async Task<IActionResult> GetMealSettings()
        {
            var settings = await _mealService.GetMealSettingsAsync();
            return Ok(settings);
        }

        /// <summary>
        /// Update meal settings (Registration only)
        /// Controls whether combined meal scanning is allowed
        /// </summary>
        [HttpPut("settings")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> UpdateMealSettings([FromBody] UpdateMealSettingsDto dto)
        {
            var success = await _mealService.UpdateMealSettingsAsync(dto);
            if (success)
                return Ok(new { message = "تم تحديث الإعدادات بنجاح" });
            return BadRequest(new { message = "فشل في تحديث الإعدادات" });
        }

        /// <summary>
        /// Get all dorm locations meal settings (Registration only)
        /// </summary>
        [HttpGet("all-locations-settings")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetAllDormLocationsSettings()
        {
            var settings = await _mealService.GetAllDormLocationsSettingsAsync();
            return Ok(settings);
        }

        /// <summary>
        /// Update specific dorm location meal setting (Registration only)
        /// </summary>
        [HttpPut("location-setting")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> UpdateDormLocationMealSetting([FromBody] UpdateDormLocationMealSettingDto dto)
        {
            var success = await _mealService.UpdateDormLocationMealSettingAsync(dto);
            if (success)
                return Ok(new { message = "تم تحديث إعدادات الموقع بنجاح" });
            return BadRequest(new { message = "فشل في تحديث إعدادات الموقع" });
        }

        /// <summary>
        /// Bulk update all dorm locations (Registration only)
        /// </summary>
        [HttpPut("bulk-update-all")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> BulkUpdateAllDormLocations([FromBody] BulkUpdateMealSettingsDto dto)
        {
            var success = await _mealService.BulkUpdateAllDormLocationsAsync(dto);
            if (success)
                return Ok(new { message = "تم تحديث جميع المواقع بنجاح" });
            return BadRequest(new { message = "فشل في تحديث المواقع" });
        }
    }
}