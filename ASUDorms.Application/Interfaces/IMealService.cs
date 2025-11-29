using ASUDorms.Application.DTOs.Meals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.Interfaces
{
    public interface IMealService
    {
        Task<MealScanResultDto> ScanMealAsync(MealScanRequestDto request);
        Task<bool> IsTimeValidForMealTypeAsync(int mealTypeId);
    }
}
