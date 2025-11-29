using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class BuildingStatisticsDto
    {
        public string BuildingNumber { get; set; }
        public int TotalStudents { get; set; }
        public int CurrentCapacity { get; set; }
        public int TotalMealsServed { get; set; }
        public decimal AttendanceRate { get; set; }
    }

    public class AllBuildingsStatisticsDto
    {
        public List<BuildingStatisticsDto> Buildings { get; set; }
        public int TotalStudentsAllBuildings { get; set; }
        public int TotalMealsServedAllBuildings { get; set; }
        public decimal AverageAttendanceRate { get; set; }
    }
}
