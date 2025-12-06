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
        public int StudentsNotOnHoliday { get; set; }
        public int StudentsOnHoliday { get; set; }
        public int TotalMealsExpected { get; set; }     // (StudentsNotOnHoliday × 2) - 2 meal times        public int TotalMealsReceived { get; set; }
        public int TotalMealsReceived { get; set; }

        public int TotalMealsRemaining { get; set; }
        public decimal OverallAttendancePercentage { get; set; }
        public int EligibleStudents { get; set; } // Students eligible for meals (after payment check)
        public int StudentsNotEligible { get; set; } // Students not eligible due to payment issues
    }
}
