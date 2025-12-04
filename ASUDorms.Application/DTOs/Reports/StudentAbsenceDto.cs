using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{

    [Obsolete("Use MealAbsenceReportDto instead")]
    public class StudentAbsenceDto
    {
        public string StudentNationalId { get; set; }
        public string StudentName { get; set; }
        public string BuildingNumber { get; set; }
        public string RoomNumber { get; set; }
        public string Faculty { get; set; }
        public string Grade { get; set; }
        public bool WasOnHoliday { get; set; }
        public decimal Penalty { get; set; }
        public List<string> MissedMeals { get; set; }
    }
}
