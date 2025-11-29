using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class ReportSummaryDto
    {
        public int TotalStudents { get; set; }
        public int TotalAbsences { get; set; }
        public int TotalMissedBreakfasts { get; set; }
        public int TotalMissedLunches { get; set; }
        public int TotalMissedDinners { get; set; }
        public int TotalMissedMeals { get; set; }
        public decimal TotalPenalty { get; set; }
    }
}
