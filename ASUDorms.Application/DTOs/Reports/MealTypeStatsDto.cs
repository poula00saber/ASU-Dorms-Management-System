using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class MealTypeStatsDto
    {
        public string MealType { get; set; }
        public int TotalMeals { get; set; }      // Total students not on holiday
        public int ReceivedMeals { get; set; }   // Students who received meal
        public int RemainingMeals { get; set; }  // Students who didn't receive meal
        public decimal AttendancePercentage { get; set; }
    }
}
