// RegistrationDashboardDto.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class RegistrationDashboardDto
    {
        public string Date { get; set; }
        public int DormLocationId { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int OnLeaveStudents { get; set; }
        public DashboardMealStatsDto ExpectedMeals { get; set; }
        public DashboardMealStatsDto ReceivedMeals { get; set; }
        public DashboardMealStatsDto RemainingMeals { get; set; }
        public decimal AttendancePercentage { get; set; }
        public List<DashboardBuildingStatsDto> BuildingStats { get; set; }
        public List<RecentRegistrationDto> RecentRegistrations { get; set; }
        public List<RecentLeaveRequestDto> RecentLeaveRequests { get; set; }
    }

    public class DashboardMealStatsDto
    {
        public int BreakfastDinner { get; set; }
        public int Lunch { get; set; }
        public int Total { get; set; }
    }

    public class DashboardBuildingStatsDto
    {
        public string BuildingNumber { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int OnLeaveStudents { get; set; }
        public int ExpectedMeals { get; set; }
        public int ReceivedMeals { get; set; }
        public int RemainingMeals { get; set; }
        public decimal AttendancePercentage { get; set; }
    }

    public class RecentRegistrationDto
    {
        public string StudentId { get; set; }
        public string Name { get; set; }
        public string Faculty { get; set; }
        public string BuildingNumber { get; set; }
        public string Time { get; set; }
    }

    public class RecentLeaveRequestDto
    {
        public string StudentId { get; set; }
        public string Name { get; set; }
        public string BuildingNumber { get; set; }
        public string LeaveDate { get; set; }
        public string ReturnDate { get; set; }
    }
}