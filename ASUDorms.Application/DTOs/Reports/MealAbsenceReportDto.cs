using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class MealAbsenceReportDto
    {
        public DateTime Date { get; set; }
        public List<StudentAbsenceDto> Absences { get; set; }
        public decimal TotalPenalty { get; set; }
        public int TotalAbsences { get; set; }
    }

}
