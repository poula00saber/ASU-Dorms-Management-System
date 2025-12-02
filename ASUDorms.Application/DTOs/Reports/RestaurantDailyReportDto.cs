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
        public MealTypeStatsDto BreakfastStats { get; set; }
        public MealTypeStatsDto LunchStats { get; set; }
        public MealTypeStatsDto DinnerStats { get; set; }
        public DailySummaryDto Summary { get; set; }
    }
}
