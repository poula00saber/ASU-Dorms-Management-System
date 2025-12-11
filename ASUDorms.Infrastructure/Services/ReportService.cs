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
        private const decimal MEAL_PENALTY_AMOUNT = 95.00m;

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
                .Where(h => h.StartDate.Date <= currentDate &&
                h.EndDate.Date >= currentDate &&
                !h.IsDeleted) // ADD THIS
                .ToListAsync();

            // Get today's meal transactions
            var todayMealTransactions = await _unitOfWork.MealTransactions
                .Query()
                .Where(m => m.Date.Date == currentDate && m.DormLocationId == dormLocationId)
                .Include(m => m.MealType)
                .ToListAsync();

            // Get active payment exemptions for today
            var paymentExemptions = await _unitOfWork.PaymentExemptions
                .Query()
                .Where(pe => pe.IsActive &&
                            pe.StartDate.Date <= currentDate &&
                            pe.EndDate.Date >= currentDate)
                .ToListAsync();

            // Filter students who are eligible for meals
            var eligibleStudents = students
                .Where(s =>
                {
                    // If student is exempt from fees, always eligible
                    if (s.IsExemptFromFees)
                        return true;

                    // If student has outstanding payment
                    if (s.HasOutstandingPayment)
                    {
                        // Check if they have a valid payment exemption
                        var hasValidExemption = paymentExemptions
                            .Any(pe => pe.StudentNationalId == s.NationalId);
                        return hasValidExemption;
                    }

                    // No outstanding payment, eligible
                    return true;
                })
                .ToList();

            // Calculate statistics
            var totalStudents = students.Count;
            var activeStudents = students.Count(s => s.IsDeleted == false);
            var onLeaveStudents = todayHolidays
                .Select(h => h.StudentNationalId)
                .Distinct()
                .Count();

            // Calculate meal statistics - ONLY for eligible students
            var eligibleStudentIds = eligibleStudents.Select(s => s.NationalId).ToList();

            var breakfastDinnerReceived = todayMealTransactions
                .Count(m => m.MealType.Name == "BreakfastDinner" && eligibleStudentIds.Contains(m.StudentNationalId));
            var lunchReceived = todayMealTransactions
                .Count(m => m.MealType.Name == "Lunch" && eligibleStudentIds.Contains(m.StudentNationalId));

            var studentsNotOnHoliday = eligibleStudents.Count -
                todayHolidays.Count(h => eligibleStudentIds.Contains(h.StudentNationalId));

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
                    var buildingStudentIds = buildingStudents.Select(s => s.NationalId).ToList();

                    // Filter eligible students in this building
                    var eligibleBuildingStudents = buildingStudents
                        .Where(s =>
                        {
                            if (s.IsExemptFromFees)
                                return true;
                            if (s.HasOutstandingPayment)
                                return paymentExemptions.Any(pe => pe.StudentNationalId == s.NationalId);
                            return true;
                        })
                        .ToList();

                    var buildingEligibleIds = eligibleBuildingStudents.Select(s => s.NationalId).ToList();

                    var buildingOnLeave = todayHolidays
                        .Count(h => buildingStudentIds.Contains(h.StudentNationalId));

                    var buildingActive = buildingStudents.Count(s => s.IsDeleted == false);
                    var buildingNotOnHoliday = eligibleBuildingStudents.Count -
                        todayHolidays.Count(h => buildingEligibleIds.Contains(h.StudentNationalId));

                    var buildingBreakfastDinner = todayMealTransactions
                        .Count(m => buildingEligibleIds.Contains(m.StudentNationalId) &&
                                  m.MealType.Name == "BreakfastDinner");

                    var buildingLunch = todayMealTransactions
                        .Count(m => buildingEligibleIds.Contains(m.StudentNationalId) &&
                                  m.MealType.Name == "Lunch");

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
                           h.StartDate.Date >= currentDate.AddDays(-7) && 
                           !h.IsDeleted)
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
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

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

            // Get all payment exemptions in date range
            var paymentExemptions = await _unitOfWork.PaymentExemptions
                .Query()
                .Where(pe => pe.IsActive &&
                            ((pe.StartDate.Date <= toDate.Date && pe.EndDate.Date >= fromDate.Date)))
                .ToListAsync();

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
                           h.EndDate.Date >= fromDate.Date &&
                           !h.IsDeleted)
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
                    // Check if student is eligible for meals
                    bool isEligible = true;

                    // Check payment status for each day in the range
                    for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                    {
                        if (student.IsExemptFromFees)
                        {
                            // Always eligible if exempt from fees
                            continue;
                        }

                        if (student.HasOutstandingPayment)
                        {
                            // Check if has valid exemption for this specific date
                            var hasValidExemption = paymentExemptions
                                .Any(pe => pe.StudentNationalId == student.NationalId &&
                                          pe.StartDate.Date <= date &&
                                          pe.EndDate.Date >= date);

                            if (!hasValidExemption)
                            {
                                // Student is NOT eligible for meals on this day
                                isEligible = false;
                                break;
                            }
                        }
                    }

                    if (!isEligible)
                    {
                        // Skip student entirely - they shouldn't have meals counted
                        continue;
                    }

                    // Get student's holidays
                    var studentHolidays = holidays.Where(h => h.StudentNationalId == student.NationalId && !h.IsDeleted).ToList();

                    // Get student's meal transactions
                    var studentMealTransactions = mealTransactions
                        .Where(m => m.StudentNationalId == student.NationalId)
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
                            StudentNationalId = student.NationalId,
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
            var dormLocationId =await _authService.GetCurrentDormLocationIdAsync();

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
                        g.Any(s => s.StudentId == m.StudentNationalId)),
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

            if (!string.IsNullOrEmpty(buildingNumber))
                studentsQuery = studentsQuery.Where(s => s.BuildingNumber == buildingNumber);

            var students = await studentsQuery.ToListAsync();
            var totalStudents = students.Count;

            // Get payment exemptions for this date
            var paymentExemptions = await _unitOfWork.PaymentExemptions
                .Query()
                .Where(pe => pe.IsActive &&
                            pe.StartDate.Date <= date.Date &&
                            pe.EndDate.Date >= date.Date)
                .ToListAsync();

            // Filter eligible students for this date
            var eligibleStudents = students
                .Where(s =>
                {
                    if (s.IsExemptFromFees)
                        return true;

                    if (s.HasOutstandingPayment)
                    {
                        return paymentExemptions.Any(pe => pe.StudentNationalId == s.NationalId);
                    }

                    return true;
                })
                .ToList();

            var eligibleStudentIds = eligibleStudents.Select(s => s.NationalId).ToList();

            // Get students on holiday on this specific date
            var holidays = await _unitOfWork.Holidays
                .Query()
                .Where(h => h.StartDate.Date <= date.Date && h.EndDate.Date >= date.Date && !h.IsDeleted)
                .ToListAsync();

            var studentIdsOnHoliday = holidays
                .Where(h => eligibleStudentIds.Contains(h.StudentNationalId))
                .Select(h => h.StudentNationalId)
                .Distinct()
                .ToList();

            // Calculate students NOT on holiday (these are the ones who should eat)
            var studentsNotOnHoliday = eligibleStudents.Count - studentIdsOnHoliday.Count;

            var mealTransactions = await _unitOfWork.MealTransactions
                .Query()
                .Where(m => m.Date.Date == date.Date &&
                           m.DormLocationId == dormLocationId &&
                           eligibleStudentIds.Contains(m.StudentNationalId))
                .Include(m => m.MealType)
                .ToListAsync();

            // Calculate BreakfastDinner stats
            var breakfastDinnerReceived = mealTransactions
                .Count(m => m.MealType.Name == "BreakfastDinner");
            var breakfastDinnerRemaining = studentsNotOnHoliday - breakfastDinnerReceived;

            // Calculate lunch stats
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
                    EligibleStudents = eligibleStudents.Count,
                    StudentsNotEligible = totalStudents - eligibleStudents.Count,
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
            var actualMeals = transactions.Count(t => studentIds.Contains(t.StudentNationalId));

            return (decimal)actualMeals / expectedMeals * 100;
        }




        public async Task<DailyAbsenceReportDto> GetDailyAbsenceReportAsync(DateTime date)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

            // Get all students
            var students = await _unitOfWork.Students
                .Query()
                .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted)
                .ToListAsync();

            // Get payment exemptions for this date
            var paymentExemptions = await _unitOfWork.PaymentExemptions
                .Query()
                .Where(pe => pe.IsActive &&
                            pe.StartDate.Date <= date.Date &&
                            pe.EndDate.Date >= date.Date)
                .ToListAsync();

            // Filter eligible students
            var eligibleStudents = students
                .Where(s =>
                {
                    if (s.IsExemptFromFees) return true;
                    if (s.HasOutstandingPayment)
                        return paymentExemptions.Any(pe => pe.StudentNationalId == s.NationalId);
                    return true;
                })
                .ToList();

            // Get holidays for this date
            var holidays = await _unitOfWork.Holidays
                .Query()
                .Where(h => h.StartDate.Date <= date.Date &&
                           h.EndDate.Date >= date.Date &&
                           !h.IsDeleted)
                .ToListAsync();

            var studentIdsOnHoliday = holidays.Select(h => h.StudentNationalId).Distinct().ToList();

            // Students expected to eat (eligible and not on holiday)
            var studentsExpectedToEat = eligibleStudents
                .Where(s => !studentIdsOnHoliday.Contains(s.NationalId))
                .ToList();

            // Get meal transactions for this date
            var mealTransactions = await _unitOfWork.MealTransactions
                .Query()
                .Where(m => m.Date.Date == date.Date && m.DormLocationId == dormLocationId)
                .Include(m => m.MealType)
                .ToListAsync();

            // Group by building
            var buildingGroups = new List<BuildingDailyAbsenceDto>();

            foreach (var buildingGroup in studentsExpectedToEat.GroupBy(s => s.BuildingNumber))
            {
                var studentsWhoDidntEat = new List<StudentDailyAbsenceDto>();

                foreach (var student in buildingGroup)
                {
                    var studentTransactions = mealTransactions
                        .Where(m => m.StudentNationalId == student.NationalId)
                        .ToList();

                    var hadBreakfastDinner = studentTransactions
                        .Any(m => m.MealType.Name == "BreakfastDinner");
                    var hadLunch = studentTransactions
                        .Any(m => m.MealType.Name == "Lunch");

                    // If student missed any meal, add to report
                    if (!hadBreakfastDinner || !hadLunch)
                    {
                        var missedCount = 0;
                        if (!hadBreakfastDinner) missedCount++;
                        if (!hadLunch) missedCount++;

                        studentsWhoDidntEat.Add(new StudentDailyAbsenceDto
                        {
                            NationalId = student.NationalId,
                            StudentId = student.StudentId,
                            Name = $"{student.FirstName} {student.LastName}",
                            BuildingNumber = student.BuildingNumber,
                            RoomNumber = student.RoomNumber,
                            Faculty = student.Faculty,
                            MissedBreakfastDinner = !hadBreakfastDinner,
                            MissedLunch = !hadLunch,
                            TotalMissedMealsToday = missedCount
                        });
                    }
                }

                if (studentsWhoDidntEat.Any())
                {
                    buildingGroups.Add(new BuildingDailyAbsenceDto
                    {
                        BuildingNumber = buildingGroup.Key,
                        TotalStudentsInBuilding = buildingGroup.Count(),
                        StudentsWhoDidntEat = studentsWhoDidntEat.Count,
                        Students = studentsWhoDidntEat.OrderBy(s => s.Name).ToList()
                    });
                }
            }

            // Calculate summary
            var totalMissedBreakfastDinner = buildingGroups
                .Sum(b => b.Students.Count(s => s.MissedBreakfastDinner));
            var totalMissedLunch = buildingGroups
                .Sum(b => b.Students.Count(s => s.MissedLunch));
            var totalMissedMeals = totalMissedBreakfastDinner + totalMissedLunch;

            return new DailyAbsenceReportDto
            {
                Date = date,
                TotalStudents = students.Count,
                StudentsOnHoliday = studentIdsOnHoliday.Count,
                StudentsExpectedToEat = studentsExpectedToEat.Count,
                StudentsWhoDidntEat = buildingGroups.Sum(b => b.StudentsWhoDidntEat),
                BuildingGroups = buildingGroups.OrderBy(b => b.BuildingNumber).ToList(),
                Summary = new DailyAbsenceSummaryDto
                {
                    TotalMissedBreakfastDinner = totalMissedBreakfastDinner,
                    TotalMissedLunch = totalMissedLunch,
                    TotalMissedMeals = totalMissedMeals,
                    ExpectedPenalty = totalMissedMeals * MEAL_PENALTY_AMOUNT
                }
            };
        }

        public async Task<MonthlyAbsenceReportDto> GetMonthlyAbsenceReportAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

            // Get all students
            var students = await _unitOfWork.Students
                .Query()
                .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted)
                .ToListAsync();

            // Get payment exemptions
            var paymentExemptions = await _unitOfWork.PaymentExemptions
                .Query()
                .Where(pe => pe.IsActive &&
                            ((pe.StartDate.Date <= toDate.Date && pe.EndDate.Date >= fromDate.Date)))
                .ToListAsync();

            // Get meal transactions
            var mealTransactions = await _unitOfWork.MealTransactions
                .Query()
                .Where(m => m.Date.Date >= fromDate.Date &&
                           m.Date.Date <= toDate.Date &&
                           m.DormLocationId == dormLocationId)
                .Include(m => m.MealType)
                .ToListAsync();

            // Get holidays
            var holidays = await _unitOfWork.Holidays
                .Query()
                .Where(h => h.StartDate.Date <= toDate.Date &&
                           h.EndDate.Date >= fromDate.Date &&
                           !h.IsDeleted)
                .ToListAsync();

            var buildingGroups = new List<BuildingMonthlyAbsenceDto>();

            foreach (var buildingGroup in students.GroupBy(s => s.BuildingNumber))
            {
                var studentsWithAbsences = new List<StudentMonthlyAbsenceDto>();

                foreach (var student in buildingGroup)
                {
                    // Check eligibility
                    bool isEligibleForPeriod = true;
                    if (!student.IsExemptFromFees && student.HasOutstandingPayment)
                    {
                        isEligibleForPeriod = paymentExemptions
                            .Any(pe => pe.StudentNationalId == student.NationalId);
                    }

                    if (!isEligibleForPeriod) continue;

                    var studentHolidays = holidays
                        .Where(h => h.StudentNationalId == student.NationalId)
                        .ToList();

                    var studentTransactions = mealTransactions
                        .Where(m => m.StudentNationalId == student.NationalId)
                        .ToList();

                    int missedBreakfastDinnerCount = 0;
                    int missedLunchCount = 0;
                    var missedDates = new List<DateTime>();
                    int daysOnHoliday = 0;

                    // Check each day
                    for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                    {
                        var wasOnHoliday = studentHolidays
                            .Any(h => h.StartDate.Date <= date && h.EndDate.Date >= date);

                        if (wasOnHoliday)
                        {
                            daysOnHoliday++;
                            continue;
                        }

                        var dayTransactions = studentTransactions
                            .Where(m => m.Date.Date == date)
                            .ToList();

                        bool missedAnyMeal = false;

                        if (!dayTransactions.Any(m => m.MealType.Name == "BreakfastDinner"))
                        {
                            missedBreakfastDinnerCount++;
                            missedAnyMeal = true;
                        }

                        if (!dayTransactions.Any(m => m.MealType.Name == "Lunch"))
                        {
                            missedLunchCount++;
                            missedAnyMeal = true;
                        }

                        if (missedAnyMeal && !missedDates.Contains(date))
                        {
                            missedDates.Add(date);
                        }
                    }

                    var totalMissedMeals = missedBreakfastDinnerCount + missedLunchCount;

                    if (totalMissedMeals > 0)
                    {
                        studentsWithAbsences.Add(new StudentMonthlyAbsenceDto
                        {
                            NationalId = student.NationalId,
                            StudentId = student.StudentId,
                            Name = $"{student.FirstName} {student.LastName}",
                            BuildingNumber = student.BuildingNumber,
                            RoomNumber = student.RoomNumber,
                            Faculty = student.Faculty,
                            TotalMissedMeals = totalMissedMeals,
                            MissedBreakfastDinnerCount = missedBreakfastDinnerCount,
                            MissedLunchCount = missedLunchCount,
                            MissedDates = missedDates.OrderBy(d => d).ToList(),
                            DaysOnHoliday = daysOnHoliday,
                            TotalPenalty = totalMissedMeals * MEAL_PENALTY_AMOUNT
                        });
                    }
                }

                if (studentsWithAbsences.Any())
                {
                    buildingGroups.Add(new BuildingMonthlyAbsenceDto
                    {
                        BuildingNumber = buildingGroup.Key,
                        TotalStudents = buildingGroup.Count(),
                        Students = studentsWithAbsences
                            .OrderByDescending(s => s.TotalMissedMeals)
                            .ToList()
                    });
                }
            }

            var totalDays = (toDate.Date - fromDate.Date).Days + 1;

            return new MonthlyAbsenceReportDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalDays = totalDays,
                BuildingGroups = buildingGroups.OrderBy(b => b.BuildingNumber).ToList(),
                Summary = new MonthlyAbsenceSummaryDto
                {
                    TotalStudentsWithAbsences = buildingGroups.Sum(b => b.Students.Count),
                    TotalMissedMeals = buildingGroups.Sum(b => b.Students.Sum(s => s.TotalMissedMeals)),
                    TotalMissedBreakfastDinner = buildingGroups.Sum(b => b.Students.Sum(s => s.MissedBreakfastDinnerCount)),
                    TotalMissedLunch = buildingGroups.Sum(b => b.Students.Sum(s => s.MissedLunchCount)),
                    TotalPenalty = buildingGroups.Sum(b => b.Students.Sum(s => s.TotalPenalty))
                }
            };
        }







    }








}
