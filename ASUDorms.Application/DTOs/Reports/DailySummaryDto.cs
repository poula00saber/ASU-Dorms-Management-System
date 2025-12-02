using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class DailySummaryDto
    {
        public int TotalStudentsInBuilding { get; set; }
        public int TotalMealsExpected { get; set; } // Total students × 3 meals
        public int TotalMealsReceived { get; set; }
        public int TotalMealsRemaining { get; set; }
        public decimal OverallAttendancePercentage { get; set; }
    }
}
