using ASUDorms.Application.DTOs.Meals;
using System.Threading.Tasks;

namespace ASUDorms.Application.Interfaces
{
    public interface IMealService
    {
        Task<MealScanResultDto> ScanMealAsync(MealScanRequestDto request);
        Task<MealScanResultDto> ScanCombinedMealAsync(MealScanRequestDto request);
        Task<bool> IsTimeValidForMealTypeAsync(int mealTypeId);
        Task<MealSettingsDto> GetMealSettingsAsync();
        Task<bool> UpdateMealSettingsAsync(UpdateMealSettingsDto dto);

        // NEW METHODS for managing all dorm locations
        Task<AllDormLocationSettingsDto> GetAllDormLocationsSettingsAsync();
        Task<bool> UpdateDormLocationMealSettingAsync(UpdateDormLocationMealSettingDto dto);
        Task<bool> BulkUpdateAllDormLocationsAsync(BulkUpdateMealSettingsDto dto);
    }
}