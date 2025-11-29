using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class MealTypeStatsDto
    {
        public string MealType { get; set; } // "Breakfast", "Lunch", "Dinner"
        public int TotalStudents { get; set; }
        public int ReceivedMeals { get; set; }
        public int RemainingMeals { get; set; }
        public decimal AttendancePercentage { get; set; }
    }
}
