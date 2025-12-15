using System;
using System.Collections.Generic;

namespace ASUDorms.Application.DTOs.Meals
{
    public class MealSettingsDto
    {
        public bool AllowCombinedMealScan { get; set; }
    }

    public class UpdateMealSettingsDto
    {
        public bool AllowCombinedMealScan { get; set; }
    }

    // NEW DTOs for managing all dorm locations
    public class DormLocationMealSettingsDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool AllowCombinedMealScan { get; set; }
        public bool IsActive { get; set; }
    }

    public class AllDormLocationSettingsDto
    {
        public List<DormLocationMealSettingsDto> DormLocations { get; set; }
    }

    public class UpdateDormLocationMealSettingDto
    {
        public int DormLocationId { get; set; }
        public bool AllowCombinedMealScan { get; set; }
    }

    public class BulkUpdateMealSettingsDto
    {
        public bool AllowCombinedMealScan { get; set; }
    }
}