using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class RestaurantDailyReportDto
    {
        public DateTime Date { get; set; }
        public string BuildingNumber { get; set; }

        // Breakfast & Dinner (served together)
        public MealTypeStatsDto BreakfastDinnerStats { get; set; }

        // Lunch (served separately)
        public MealTypeStatsDto LunchStats { get; set; }
        public DailySummaryDto Summary { get; set; }
    }
}
