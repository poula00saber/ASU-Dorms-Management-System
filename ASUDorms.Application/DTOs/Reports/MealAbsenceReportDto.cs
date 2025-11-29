using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class MealAbsenceReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string BuildingNumber { get; set; }
        public List<BuildingAbsenceDto> Buildings { get; set; }
        public ReportSummaryDto Summary { get; set; }
    }

}
