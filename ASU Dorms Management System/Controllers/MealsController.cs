using ASUDorms.Application.DTOs.Meals;
using ASUDorms.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ASU_Dorms_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MealsController : ControllerBase
    {
        private readonly IMealService _mealService;
        private readonly ILogger<MealsController> _logger;

        public MealsController(IMealService mealService, ILogger<MealsController> logger)
        {
            _mealService = mealService;
            _logger = logger;
        }

        /// <summary>
        /// Scan a single meal (Breakfast/Dinner OR Lunch)
        /// </summary>
        [HttpPost("scan")]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> ScanMeal([FromBody] MealScanRequestDto request)
        {
            var nationalIdHash = HashString(request.NationalId);

            _logger.LogInformation("Meal scan request: NationalIdHash={NationalIdHash}, MealType={MealType}",
                nationalIdHash, request.MealTypeId);

            var result = await _mealService.ScanMealAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("Meal scan success: NationalIdHash={NationalIdHash}, MealType={MealType}",
                    nationalIdHash, request.MealTypeId);
                return Ok(result);
            }

            _logger.LogInformation("Meal scan failed: NationalIdHash={NationalIdHash}, MealType={MealType}, Reason={Reason}",
                nationalIdHash, request.MealTypeId, result.Message);
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
            var nationalIdHash = HashString(request.NationalId);

            _logger.LogInformation("Combined meal scan request: NationalIdHash={NationalIdHash}",
                nationalIdHash);

            var result = await _mealService.ScanCombinedMealAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("Combined meal scan success: NationalIdHash={NationalIdHash}",
                    nationalIdHash);
                return Ok(result);
            }

            _logger.LogInformation("Combined meal scan failed: NationalIdHash={NationalIdHash}, Reason={Reason}",
                nationalIdHash, result.Message);
            return BadRequest(result);
        }

        /// <summary>
        /// Check if current time is valid for a specific meal type
        /// </summary>
        [HttpGet("time-valid/{mealTypeId}")]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> IsTimeValid(int mealTypeId)
        {
            _logger.LogDebug("Checking time validity: MealType={MealType}", mealTypeId);

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
            _logger.LogDebug("Getting meal settings");

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
            _logger.LogInformation("Updating meal settings: AllowCombinedScan={AllowCombinedScan}",
                dto.AllowCombinedMealScan);

            var success = await _mealService.UpdateMealSettingsAsync(dto);

            if (success)
            {
                _logger.LogInformation("Meal settings updated successfully");
                return Ok(new { message = "تم تحديث الإعدادات بنجاح" });
            }

            _logger.LogError("Failed to update meal settings");
            return BadRequest(new { message = "فشل في تحديث الإعدادات" });
        }

        /// <summary>
        /// Get all dorm locations meal settings (Registration only)
        /// </summary>
        [HttpGet("all-locations-settings")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> GetAllDormLocationsSettings()
        {
            _logger.LogDebug("Getting all dorm locations settings");

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
            _logger.LogInformation("Updating dorm location setting: DormLocationId={DormLocationId}, AllowCombinedScan={AllowCombinedScan}",
                dto.DormLocationId, dto.AllowCombinedMealScan);

            var success = await _mealService.UpdateDormLocationMealSettingAsync(dto);

            if (success)
            {
                _logger.LogInformation("Dorm location setting updated successfully: DormLocationId={DormLocationId}",
                    dto.DormLocationId);
                return Ok(new { message = "تم تحديث إعدادات الموقع بنجاح" });
            }

            _logger.LogError("Failed to update dorm location setting: DormLocationId={DormLocationId}",
                dto.DormLocationId);
            return BadRequest(new { message = "فشل في تحديث إعدادات الموقع" });
        }

        /// <summary>
        /// Bulk update all dorm locations (Registration only)
        /// </summary>
        [HttpPut("bulk-update-all")]
        [Authorize(Roles = "Registration")]
        public async Task<IActionResult> BulkUpdateAllDormLocations([FromBody] BulkUpdateMealSettingsDto dto)
        {
            _logger.LogInformation("Bulk updating all dorm locations: AllowCombinedScan={AllowCombinedScan}",
                dto.AllowCombinedMealScan);

            var success = await _mealService.BulkUpdateAllDormLocationsAsync(dto);

            if (success)
            {
                _logger.LogInformation("Bulk update completed successfully");
                return Ok(new { message = "تم تحديث جميع المواقع بنجاح" });
            }

            _logger.LogError("Failed to bulk update dorm locations");
            return BadRequest(new { message = "فشل في تحديث المواقع" });
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