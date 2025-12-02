using ASUDorms.Application.DTOs.Reports;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private const decimal MEAL_PENALTY_AMOUNT = 10.00m;

        public ReportService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        // ====================================================================
        // REGISTRATION USER REPORTS
        // ====================================================================

        // deeeeeeeeeep seeeeeeek

        public async Task<RegistrationDashboardDto> GetRegistrationDashboardStatsAsync()
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var currentDate = DateTime.UtcNow.Date;

            // Get all students in this dorm location
            var students = await _unitOfWork.Students
                .Query()
                .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted)
                .ToListAsync();

            // Get today's holidays
            var todayHolidays = await _unitOfWork.Holidays
                .Query()
                .Where(h => h.StartDate.Date <= currentDate && h.EndDate.Date >= currentDate)
                .ToListAsync();

            // Get today's meal transactions
            var todayMealTransactions = await _unitOfWork.MealTransactions
                .Query()
                .Where(m => m.Date.Date == currentDate && m.DormLocationId == dormLocationId)
                .Include(m => m.MealType)
                .ToListAsync();

            // Calculate statistics
            var totalStudents = students.Count;
            var activeStudents = students.Count(s=>s.IsDeleted == false);
            var onLeaveStudents = todayHolidays
                .Select(h => h.StudentId)
                .Distinct()
                .Count();

            // Calculate meal statistics
            var breakfastDinnerReceived = todayMealTransactions
                .Count(m => m.MealType.Name == "BreakfastDinner");
            var lunchReceived = todayMealTransactions
                .Count(m => m.MealType.Name == "Lunch");

            var studentsNotOnHoliday = totalStudents - onLeaveStudents;
            var expectedBreakfastDinner = studentsNotOnHoliday;
            var expectedLunch = studentsNotOnHoliday;

            var breakfastDinnerRemaining = expectedBreakfastDinner - breakfastDinnerReceived;
            var lunchRemaining = expectedLunch - lunchReceived;

            var overallAttendancePercentage = (expectedBreakfastDinner + expectedLunch) > 0
                ? ((breakfastDinnerReceived + lunchReceived) * 100.0m / (expectedBreakfastDinner + expectedLunch))
                : 0;

            // Group by building
            var buildingStats = students
                .GroupBy(s => s.BuildingNumber)
                .Select(g =>
                {
                    var buildingStudents = g.ToList();
                    var buildingStudentIds = buildingStudents.Select(s => s.StudentId).ToList();

                    var buildingOnLeave = todayHolidays
                        .Count(h => buildingStudentIds.Contains(h.StudentId));

                    var buildingActive = buildingStudents.Count(s => s.IsDeleted == false);
                    var buildingNotOnHoliday = buildingStudents.Count - buildingOnLeave;

                    var buildingBreakfastDinner = todayMealTransactions
                        .Count(m => buildingStudentIds.Contains(m.StudentId) && m.MealType.Name == "BreakfastDinner");

                    var buildingLunch = todayMealTransactions
                        .Count(m => buildingStudentIds.Contains(m.StudentId) && m.MealType.Name == "Lunch");

                    var buildingTotalReceived = buildingBreakfastDinner + buildingLunch;
                    var buildingTotalExpected = buildingNotOnHoliday * 2; // BreakfastDinner + Lunch
                    var buildingAttendance = buildingTotalExpected > 0
                        ? (buildingTotalReceived * 100.0m / buildingTotalExpected)
                        : 0;

                    return new DashboardBuildingStatsDto
                    {
                        BuildingNumber = g.Key ?? "غير محدد",
                        TotalStudents = buildingStudents.Count,
                        ActiveStudents = buildingActive,
                        OnLeaveStudents = buildingOnLeave,
                        ExpectedMeals = buildingTotalExpected,
                        ReceivedMeals = buildingTotalReceived,
                        RemainingMeals = buildingTotalExpected - buildingTotalReceived,
                        AttendancePercentage = buildingAttendance
                    };
                })
                .OrderBy(b => b.BuildingNumber)
                .ToList();

            // Get recent registrations (last 5)
            var recentRegistrations = await _unitOfWork.Students
                .Query()
                .Where(s => s.DormLocationId == dormLocationId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => new RecentRegistrationDto
                {
                    StudentId = s.StudentId,
                    Name = s.FirstName + " " + s.LastName,
                    Faculty = s.Faculty ?? "غير محدد",
                    BuildingNumber = s.BuildingNumber ?? "غير محدد",
                    Time = s.CreatedAt.ToString("hh:mm tt")
                })
                .ToListAsync();

            // Get recent leave requests (last 5)
            var recentLeaveRequests = await _unitOfWork.Holidays
                .Query()
                .Include(h => h.Student)
                .Where(h => h.Student.DormLocationId == dormLocationId &&
                           h.StartDate.Date >= currentDate.AddDays(-7))
                .OrderByDescending(h => h.CreatedAt)
                .Take(5)
                .Select(h => new RecentLeaveRequestDto
                {
                    StudentId = h.Student.StudentId,
                    Name = h.Student.FirstName + " " + h.Student.LastName,
                    BuildingNumber = h.Student.BuildingNumber ?? "غير محدد",
                    LeaveDate = h.StartDate.ToString("yyyy-MM-dd"),
                    ReturnDate = h.EndDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return new RegistrationDashboardDto
            {
                Date = currentDate.ToString("yyyy-MM-dd"),
                DormLocationId = dormLocationId,
                TotalStudents = totalStudents,
                ActiveStudents = activeStudents,
                OnLeaveStudents = onLeaveStudents,
                ExpectedMeals = new DashboardMealStatsDto
                {
                    BreakfastDinner = expectedBreakfastDinner,
                    Lunch = expectedLunch,
                    Total = expectedBreakfastDinner + expectedLunch
                },
                ReceivedMeals = new DashboardMealStatsDto
                {
                    BreakfastDinner = breakfastDinnerReceived,
                    Lunch = lunchReceived,
                    Total = breakfastDinnerReceived + lunchReceived
                },
                RemainingMeals = new DashboardMealStatsDto
                {
                    BreakfastDinner = breakfastDinnerRemaining,
                    Lunch = lunchRemaining,
                    Total = breakfastDinnerRemaining + lunchRemaining
                },
                AttendancePercentage = overallAttendancePercentage,
                BuildingStats = buildingStats,
                RecentRegistrations = recentRegistrations,
                RecentLeaveRequests = recentLeaveRequests
            };
        }





        public async Task<MealAbsenceReportDto> GetMealAbsenceReportAsync(
            DateTime fromDate,
            DateTime toDate,
            string buildingNumber = null,
            string government = null,
            string district = null,
            string faculty = null)
        {
            var dormLocationId =await _authService.GetCurrentDormLocationIdAsync();

            // Get all students with filters
            var studentsQuery = _unitOfWork.Students.Query()
                .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted);

            if (!string.IsNullOrEmpty(buildingNumber))
                studentsQuery = studentsQuery.Where(s => s.BuildingNumber == buildingNumber);

            if (!string.IsNullOrEmpty(government))
                studentsQuery = studentsQuery.Where(s => s.Government == government);

            if (!string.IsNullOrEmpty(district))
                studentsQuery = studentsQuery.Where(s => s.District == district);

            if (!string.IsNullOrEmpty(faculty))
                studentsQuery = studentsQuery.Where(s => s.Faculty == faculty);

            var students = await studentsQuery.ToListAsync();

            // Get all meal transactions in date range
            var mealTransactions = await _unitOfWork.MealTransactions
                .Query()
                .Where(m => m.Date.Date >= fromDate.Date &&
                           m.Date.Date <= toDate.Date &&
                           m.DormLocationId == dormLocationId)
                .Include(m => m.MealType)
                .ToListAsync();

            // Get holidays in date range
            var holidays = await _unitOfWork.Holidays
                .Query()
                .Where(h => h.StartDate.Date <= toDate.Date &&
                           h.EndDate.Date >= fromDate.Date)
                .ToListAsync();

            // Group by building
            var buildingGroups = students.GroupBy(s => s.BuildingNumber);
            var buildingReports = new List<BuildingAbsenceDto>();

            // Calculate total days in range
            var totalDays = (toDate.Date - fromDate.Date).Days + 1;

            foreach (var buildingGroup in buildingGroups)
            {
                var buildingStudents = new List<StudentAbsenceDetailDto>();
                int buildingTotalMissedMeals = 0;
                decimal buildingTotalPenalty = 0;

                foreach (var student in buildingGroup)
                {
                    // Get student's holidays
                    var studentHolidays = holidays.Where(h => h.StudentId == student.StudentId).ToList();

                    // Get student's meal transactions
                    var studentMealTransactions = mealTransactions
                        .Where(m => m.StudentId == student.StudentId)
                        .ToList();

                    // Count days on holiday
                    int daysOnHoliday = 0;
                    for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                    {
                        if (studentHolidays.Any(h => h.StartDate.Date <= date && h.EndDate.Date >= date))
                        {
                            daysOnHoliday++;
                        }
                    }

                    // Calculate missed meals for each type
                    int missedBreakfast = 0;
                    int missedLunch = 0;
                    int missedDinner = 0;

                    // Count expected and received meals
                    for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                    {
                        // Check if student was on holiday this day
                        var wasOnHoliday = studentHolidays.Any(h =>
                            h.StartDate.Date <= date && h.EndDate.Date >= date);

                        if (!wasOnHoliday)
                        {
                            // Check each meal type
                            var dayTransactions = studentMealTransactions
                                .Where(m => m.Date.Date == date)
                                .ToList();

                            // Breakfast (assuming BreakfastDinner includes both)
                            var hadBreakfastDinner = dayTransactions.Any(m =>
                                m.MealType.Name == "BreakfastDinner");

                            if (!hadBreakfastDinner)
                            {
                                missedBreakfast++;
                                missedDinner++;
                            }

                            // Lunch
                            var hadLunch = dayTransactions.Any(m =>
                                m.MealType.Name == "Lunch");

                            if (!hadLunch)
                            {
                                missedLunch++;
                            }
                        }
                    }

                    int totalMissedMeals = missedBreakfast + missedLunch + missedDinner;

                    if (totalMissedMeals > 0)
                    {
                        decimal penalty = totalMissedMeals * MEAL_PENALTY_AMOUNT;
                        buildingTotalMissedMeals += totalMissedMeals;
                        buildingTotalPenalty += penalty;

                        buildingStudents.Add(new StudentAbsenceDetailDto
                        {
                            StudentId = student.StudentId,
                            StudentName = $"{student.FirstName} {student.LastName}",
                            BuildingNumber = student.BuildingNumber,
                            RoomNumber = student.RoomNumber,
                            Faculty = student.Faculty,
                            Grade = student.Grade,
                            MissedBreakfastCount = missedBreakfast,
                            MissedLunchCount = missedLunch,
                            MissedDinnerCount = missedDinner,
                            TotalMissedMeals = totalMissedMeals,
                            TotalPenalty = penalty,
                            DaysOnHoliday = daysOnHoliday,
                            IsCurrentlyOnHoliday = studentHolidays.Any(h =>
                                h.StartDate.Date <= DateTime.Today &&
                                h.EndDate.Date >= DateTime.Today)
                        });
                    }
                }

                if (buildingStudents.Any())
                {
                    buildingReports.Add(new BuildingAbsenceDto
                    {
                        BuildingNumber = buildingGroup.Key,
                        TotalStudents = buildingGroup.Count(),
                        Students = buildingStudents.OrderBy(s => s.StudentName).ToList(),
                        TotalMissedMeals = buildingTotalMissedMeals,
                        TotalPenalty = buildingTotalPenalty
                    });
                }
            }

            // Calculate overall summary
            var summary = new ReportSummaryDto
            {
                TotalStudents = students.Count,
                TotalAbsences = buildingReports.Sum(b => b.Students.Count),
                TotalMissedBreakfasts = buildingReports.Sum(b =>
                    b.Students.Sum(s => s.MissedBreakfastCount)),
                TotalMissedLunches = buildingReports.Sum(b =>
                    b.Students.Sum(s => s.MissedLunchCount)),
                TotalMissedDinners = buildingReports.Sum(b =>
                    b.Students.Sum(s => s.MissedDinnerCount)),
                TotalMissedMeals = buildingReports.Sum(b => b.TotalMissedMeals),
                TotalPenalty = buildingReports.Sum(b => b.TotalPenalty)
            };

            return new MealAbsenceReportDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                BuildingNumber = buildingNumber,
                Buildings = buildingReports.OrderBy(b => b.BuildingNumber).ToList(),
                Summary = summary
            };
        }

        public async Task<AllBuildingsStatisticsDto> GetAllBuildingsStatisticsAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

            var students = await _unitOfWork.Students.Query()
                .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted)
                .ToListAsync();

            var mealTransactions = await _unitOfWork.MealTransactions
                .Query()
                .Where(m => m.Date.Date >= fromDate.Date &&
                           m.Date.Date <= toDate.Date &&
                           m.DormLocationId == dormLocationId)
                .ToListAsync();

            var buildingStats = students.GroupBy(s => s.BuildingNumber)
                .Select(g => new BuildingStatisticsDto
                {
                    BuildingNumber = g.Key,
                    TotalStudents = g.Count(),
                    CurrentCapacity = g.Count(), // You can add max capacity field if needed
                    TotalMealsServed = mealTransactions.Count(m =>
                        g.Any(s => s.StudentId == m.StudentId)),
                    AttendanceRate = CalculateAttendanceRate(g.ToList(), mealTransactions, fromDate, toDate)
                })
                .OrderBy(b => b.BuildingNumber)
                .ToList();

            return new AllBuildingsStatisticsDto
            {
                Buildings = buildingStats,
                TotalStudentsAllBuildings = students.Count,
                TotalMealsServedAllBuildings = mealTransactions.Count,
                AverageAttendanceRate = buildingStats.Any() ?
                    buildingStats.Average(b => b.AttendanceRate) : 0
            };
        }

        // ====================================================================
        // RESTAURANT USER REPORTS
        // ====================================================================


        public async Task<RestaurantDailyReportDto> GetRestaurantTodayReportAsync(
            string buildingNumber = null)
        {
            return await GetRestaurantDailyReportAsync(DateTime.Today, buildingNumber);
        }

        public async Task<RestaurantDailyReportDto> GetRestaurantDailyReportAsync(
            DateTime date,
            string buildingNumber = null)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

            // Get all students in the dorm location
            var studentsQuery = _unitOfWork.Students.Query()
                .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted);

            // Filter by building if specified
            if (!string.IsNullOrEmpty(buildingNumber))
                studentsQuery = studentsQuery.Where(s => s.BuildingNumber == buildingNumber);

            var students = await studentsQuery.ToListAsync();
            var totalStudents = students.Count;

            // Get students on holiday on this specific date
            var holidays = await _unitOfWork.Holidays
                .Query()
                .Where(h => h.StartDate.Date <= date.Date && h.EndDate.Date >= date.Date)
                .ToListAsync();

            var studentIdsOnHoliday = holidays.Select(h => h.StudentId).Distinct().ToList();

            // Calculate students NOT on holiday (these are the ones who should eat)
            var studentsNotOnHoliday = totalStudents - studentIdsOnHoliday.Count;

            // Get meal transactions for this date
            var studentIds = students.Select(s => s.StudentId).ToList();
            var mealTransactions = await _unitOfWork.MealTransactions
                .Query()
                .Where(m => m.Date.Date == date.Date &&
                            m.DormLocationId == dormLocationId &&
                            studentIds.Contains(m.StudentId))
                .Include(m => m.MealType)
                .ToListAsync();

            // Calculate BreakfastDinner stats
            var breakfastDinnerReceived = mealTransactions
                .Count(m => m.MealType.Name == "BreakfastDinner");
            var breakfastDinnerRemaining = studentsNotOnHoliday - breakfastDinnerReceived;

            // Calculate Lunch stats
            var lunchReceived = mealTransactions
                .Count(m => m.MealType.Name == "Lunch");
            var lunchRemaining = studentsNotOnHoliday - lunchReceived;

            // Total meals expected = students not on holiday × 2 meal times (BreakfastDinner + Lunch)
            var totalMealsExpected = studentsNotOnHoliday * 2;
            var totalMealsReceived = breakfastDinnerReceived + lunchReceived;
            var totalMealsRemaining = breakfastDinnerRemaining + lunchRemaining;

            var report = new RestaurantDailyReportDto
            {
                Date = date,
                BuildingNumber = buildingNumber,

                BreakfastDinnerStats = new MealTypeStatsDto
                {
                    MealType = "Breakfast & Dinner",
                    TotalMeals = studentsNotOnHoliday,
                    ReceivedMeals = breakfastDinnerReceived,
                    RemainingMeals = breakfastDinnerRemaining,
                    AttendancePercentage = studentsNotOnHoliday > 0
                        ? (decimal)breakfastDinnerReceived / studentsNotOnHoliday * 100
                        : 0
                },

                LunchStats = new MealTypeStatsDto
                {
                    MealType = "Lunch",
                    TotalMeals = studentsNotOnHoliday,
                    ReceivedMeals = lunchReceived,
                    RemainingMeals = lunchRemaining,
                    AttendancePercentage = studentsNotOnHoliday > 0
                        ? (decimal)lunchReceived / studentsNotOnHoliday * 100
                        : 0
                },

                Summary = new DailySummaryDto
                {
                    TotalStudentsInBuilding = totalStudents,
                    StudentsNotOnHoliday = studentsNotOnHoliday,
                    StudentsOnHoliday = studentIdsOnHoliday.Count,
                    TotalMealsExpected = totalMealsExpected,
                    TotalMealsReceived = totalMealsReceived,
                    TotalMealsRemaining = totalMealsRemaining,
                    OverallAttendancePercentage = totalMealsExpected > 0
                        ? (decimal)totalMealsReceived / totalMealsExpected * 100
                        : 0
                }
            };

            return report;
        }

        // ====================================================================
        // HELPER METHODS
        // ====================================================================

        private decimal CalculateAttendanceRate(
            List<Domain.Entities.Student> students,
            List<Domain.Entities.MealTransaction> transactions,
            DateTime fromDate,
            DateTime toDate)
        {
            var totalDays = (toDate.Date - fromDate.Date).Days + 1;
            var expectedMeals = students.Count * totalDays * 3; // 3 meals per day

            if (expectedMeals == 0) return 0;

            var studentIds = students.Select(s => s.StudentId).ToList();
            var actualMeals = transactions.Count(t => studentIds.Contains(t.StudentId));

            return (decimal)actualMeals / expectedMeals * 100;
        }
    }
}
