using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class StudentAbsenceDetailDto
    {
        public string StudentNationalId { get; set; }
        public string StudentName { get; set; }
        public string BuildingNumber { get; set; }
        public string RoomNumber { get; set; }
        public string Faculty { get; set; }
        public string Grade { get; set; }

        // Meal counts
        public int MissedBreakfastCount { get; set; }
        public int MissedLunchCount { get; set; }
        public int MissedDinnerCount { get; set; }
        public int TotalMissedMeals { get; set; }

        // Financial
        public decimal TotalPenalty { get; set; }

        // Holiday info
        public int DaysOnHoliday { get; set; }
        public bool IsCurrentlyOnHoliday { get; set; }
    }
}
