using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class BuildingAbsenceDto
    {
        public string BuildingNumber { get; set; }
        public int TotalStudents { get; set; }
        public List<StudentAbsenceDetailDto> Students { get; set; }
        public int TotalMissedMeals { get; set; }
        public decimal TotalPenalty { get; set; }
    }
}
