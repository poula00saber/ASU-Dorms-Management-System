using System;
using System.Collections.Generic;

namespace ASUDorms.Application.DTOs.Reports
{
    public class DailyAbsenceReportDto
    {
        public DateTime Date { get; set; }
        public int TotalStudents { get; set; }
        public int StudentsOnHoliday { get; set; }
        public int StudentsExpectedToEat { get; set; }
        public int StudentsWhoDidntEat { get; set; }
        public List<BuildingDailyAbsenceDto> BuildingGroups { get; set; }
        public DailyAbsenceSummaryDto Summary { get; set; }
    }

    public class BuildingDailyAbsenceDto
    {
        public string BuildingNumber { get; set; }
        public int TotalStudentsInBuilding { get; set; }
        public int StudentsWhoDidntEat { get; set; }
        public List<StudentDailyAbsenceDto> Students { get; set; }
    }

    public class StudentDailyAbsenceDto
    {
        public string NationalId { get; set; }
        public string StudentId { get; set; }
        public string Name { get; set; }
        public string BuildingNumber { get; set; }
        public string RoomNumber { get; set; }
        public string Faculty { get; set; }
        public bool MissedBreakfastDinner { get; set; }
        public bool MissedLunch { get; set; }
        public int TotalMissedMealsToday { get; set; }
    }

    public class DailyAbsenceSummaryDto
    {
        public int TotalMissedBreakfastDinner { get; set; }
        public int TotalMissedLunch { get; set; }
        public int TotalMissedMeals { get; set; }
        public decimal ExpectedPenalty { get; set; }
    }

    public class MonthlyAbsenceReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalDays { get; set; }
        public List<BuildingMonthlyAbsenceDto> BuildingGroups { get; set; }
        public MonthlyAbsenceSummaryDto Summary { get; set; }
    }

    public class BuildingMonthlyAbsenceDto
    {
        public string BuildingNumber { get; set; }
        public int TotalStudents { get; set; }
        public List<StudentMonthlyAbsenceDto> Students { get; set; }
    }

    public class StudentMonthlyAbsenceDto
    {
        public string NationalId { get; set; }
        public string StudentId { get; set; }
        public string Name { get; set; }
        public string BuildingNumber { get; set; }
        public string RoomNumber { get; set; }
        public string Faculty { get; set; }
        public int TotalMissedMeals { get; set; }
        public int MissedBreakfastDinnerCount { get; set; }
        public int MissedLunchCount { get; set; }
        public List<DateTime> MissedDates { get; set; }
        public int DaysOnHoliday { get; set; }
        public decimal TotalPenalty { get; set; }
    }

    public class MonthlyAbsenceSummaryDto
    {
        public int TotalStudentsWithAbsences { get; set; }
        public int TotalMissedMeals { get; set; }
        public int TotalMissedBreakfastDinner { get; set; }
        public int TotalMissedLunch { get; set; }
        public decimal TotalPenalty { get; set; }
    }
}