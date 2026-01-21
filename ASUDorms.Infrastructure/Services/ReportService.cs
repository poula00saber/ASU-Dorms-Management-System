using ASUDorms.Application.DTOs.Reports;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly ILogger<ReportService> _logger;
        private const decimal MEAL_PENALTY_AMOUNT = 95.00m;

        public ReportService(IUnitOfWork unitOfWork, IAuthService authService, ILogger<ReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _logger = logger;
        }

        public async Task<RegistrationDashboardDto> GetRegistrationDashboardStatsAsync()
        {
            _logger.LogDebug("🎬 ReportService.GetRegistrationDashboardStatsAsync STARTED");

            // Try the direct method first
            var dormLocationId = _authService.GetDormIdFromHeaderOrToken();

            _logger.LogDebug("📊 Direct method returned dormLocationId: {DormLocationId}", dormLocationId);

            // If direct method fails, try async method
            if (dormLocationId == 0)
            {
                dormLocationId = await _authService.GetSelectedDormLocationIdAsync();
                _logger.LogDebug("📊 Async method returned dormLocationId: {DormLocationId}", dormLocationId);
            }

            var currentDate = DateTime.UtcNow.Date;

            _logger.LogDebug("🏠 Final dormLocationId: {DormLocationId}, Date: {Date}",
                dormLocationId, currentDate.ToString("yyyy-MM-dd"));

            // Direct database check for debugging
            var studentCount = await _unitOfWork.Students
                .Query()
                .CountAsync(s => s.DormLocationId == dormLocationId && !s.IsDeleted);

            _logger.LogDebug("🧮 Direct DB check: {StudentCount} students in dorm {DormLocationId}",
                studentCount, dormLocationId);

            try
            {
                var students = await _unitOfWork.Students
                    .Query()
                    .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted)
                    .ToListAsync();

                var todayHolidays = await _unitOfWork.Holidays
                    .Query()
                    .Where(h => h.StartDate.Date <= currentDate &&
                              h.EndDate.Date >= currentDate &&
                              !h.IsDeleted)
                    .ToListAsync();

                var todayMealTransactions = await _unitOfWork.MealTransactions
                    .Query()
                    .Where(m => m.Date.Date == currentDate && m.DormLocationId == dormLocationId)
                    .Include(m => m.MealType)
                    .ToListAsync();

                var paymentExemptions = await _unitOfWork.PaymentExemptions
                    .Query()
                    .Where(pe => pe.IsActive &&
                                pe.StartDate.Date <= currentDate &&
                                pe.EndDate.Date >= currentDate)
                    .ToListAsync();

                var eligibleStudents = students
                    .Where(s =>
                    {
                        if (s.IsExemptFromFees)
                            return true;

                        if (s.HasOutstandingPayment)
                        {
                            var hasValidExemption = paymentExemptions
                                .Any(pe => pe.StudentNationalId == s.NationalId);
                            return hasValidExemption;
                        }

                        return true;
                    })
                    .ToList();

                var totalStudents = students.Count;
                var activeStudents = students.Count(s => s.IsDeleted == false);
                var onLeaveStudents = todayHolidays
                    .Select(h => h.StudentNationalId)
                    .Distinct()
                    .Count();

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

                var buildingStats = students
                    .GroupBy(s => s.BuildingNumber)
                    .Select(g =>
                    {
                        var buildingStudents = g.ToList();
                        var buildingStudentIds = buildingStudents.Select(s => s.NationalId).ToList();

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
                        var buildingTotalExpected = buildingNotOnHoliday * 2;
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

                _logger.LogDebug("Registration dashboard stats calculated: TotalStudents={TotalStudents}, ActiveStudents={ActiveStudents}, Attendance={AttendancePercentage}%",
                    totalStudents, activeStudents, overallAttendancePercentage);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating registration dashboard stats: DormLocationId={DormLocationId}", dormLocationId);
                throw;
            }
        }

        public async Task<MealAbsenceReportDto> GetMealAbsenceReportAsync(
            DateTime fromDate,
            DateTime toDate,
            string buildingNumber = null,
            string government = null,
            string district = null,
            string faculty = null)
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            _logger.LogDebug("Generating meal absence report: FromDate={FromDate}, ToDate={ToDate}, Building={BuildingNumber}",
                fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"), buildingNumber ?? "All");

            try
            {
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

                var paymentExemptions = await _unitOfWork.PaymentExemptions
                    .Query()
                    .Where(pe => pe.IsActive &&
                                ((pe.StartDate.Date <= toDate.Date && pe.EndDate.Date >= fromDate.Date)))
                    .ToListAsync();

                var mealTransactions = await _unitOfWork.MealTransactions
                    .Query()
                    .Where(m => m.Date.Date >= fromDate.Date &&
                               m.Date.Date <= toDate.Date &&
                               m.DormLocationId == dormLocationId)
                    .Include(m => m.MealType)
                    .ToListAsync();

                var holidays = await _unitOfWork.Holidays
                    .Query()
                    .Where(h => h.StartDate.Date <= toDate.Date &&
                               h.EndDate.Date >= fromDate.Date &&
                               !h.IsDeleted)
                    .ToListAsync();

                var buildingReports = new List<BuildingAbsenceDto>();

                foreach (var buildingGroup in students.GroupBy(s => s.BuildingNumber))
                {
                    var buildingStudents = new List<StudentAbsenceDetailDto>();
                    int buildingTotalMissedMeals = 0;
                    decimal buildingTotalPenalty = 0;

                    foreach (var student in buildingGroup)
                    {
                        bool isEligible = true;

                        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                        {
                            if (student.IsExemptFromFees) continue;

                            if (student.HasOutstandingPayment)
                            {
                                var hasValidExemption = paymentExemptions
                                    .Any(pe => pe.StudentNationalId == student.NationalId &&
                                              pe.StartDate.Date <= date &&
                                              pe.EndDate.Date >= date);

                                if (!hasValidExemption)
                                {
                                    isEligible = false;
                                    break;
                                }
                            }
                        }

                        if (!isEligible) continue;

                        var studentHolidays = holidays.Where(h => h.StudentNationalId == student.NationalId).ToList();
                        var studentMealTransactions = mealTransactions
                            .Where(m => m.StudentNationalId == student.NationalId)
                            .ToList();

                        int daysOnHoliday = 0;
                        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                        {
                            if (studentHolidays.Any(h => h.StartDate.Date <= date && h.EndDate.Date >= date))
                            {
                                daysOnHoliday++;
                            }
                        }

                        int missedBreakfast = 0;
                        int missedLunch = 0;
                        int missedDinner = 0;

                        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                        {
                            var wasOnHoliday = studentHolidays.Any(h =>
                                h.StartDate.Date <= date && h.EndDate.Date >= date);

                            if (!wasOnHoliday)
                            {
                                var dayTransactions = studentMealTransactions
                                    .Where(m => m.Date.Date == date)
                                    .ToList();

                                var hadBreakfastDinner = dayTransactions.Any(m =>
                                    m.MealType.Name == "BreakfastDinner");

                                if (!hadBreakfastDinner)
                                {
                                    missedBreakfast++;
                                    missedDinner++;
                                }

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

                _logger.LogDebug("Meal absence report generated: TotalStudents={TotalStudents}, TotalMissedMeals={TotalMissedMeals}, TotalPenalty={TotalPenalty}",
                    summary.TotalStudents, summary.TotalMissedMeals, summary.TotalPenalty);

                return new MealAbsenceReportDto
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    BuildingNumber = buildingNumber,
                    Buildings = buildingReports.OrderBy(b => b.BuildingNumber).ToList(),
                    Summary = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating meal absence report: FromDate={FromDate}, ToDate={ToDate}",
                    fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<AllBuildingsStatisticsDto> GetAllBuildingsStatisticsAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            _logger.LogDebug("Getting buildings statistics: FromDate={FromDate}, ToDate={ToDate}",
                fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));

            try
            {
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
                        CurrentCapacity = g.Count(),
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting buildings statistics: FromDate={FromDate}, ToDate={ToDate}",
                    fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<RestaurantDailyReportDto> GetRestaurantTodayReportAsync(string buildingNumber = null)
        {
            return await GetRestaurantDailyReportAsync(DateTime.Today, buildingNumber);
        }

        public async Task<RestaurantDailyReportDto> GetRestaurantDailyReportAsync(
            DateTime date,
            string buildingNumber = null)
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            _logger.LogDebug("Generating restaurant daily report: Date={Date}, Building={BuildingNumber}",
                date.ToString("yyyy-MM-dd"), buildingNumber ?? "All");

            try
            {
                var studentsQuery = _unitOfWork.Students.Query()
                    .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted);

                if (!string.IsNullOrEmpty(buildingNumber))
                    studentsQuery = studentsQuery.Where(s => s.BuildingNumber == buildingNumber);

                var students = await studentsQuery.ToListAsync();
                var totalStudents = students.Count;

                var paymentExemptions = await _unitOfWork.PaymentExemptions
                    .Query()
                    .Where(pe => pe.IsActive &&
                                pe.StartDate.Date <= date.Date &&
                                pe.EndDate.Date >= date.Date)
                    .ToListAsync();

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

                var holidays = await _unitOfWork.Holidays
                    .Query()
                    .Where(h => h.StartDate.Date <= date.Date && h.EndDate.Date >= date.Date && !h.IsDeleted)
                    .ToListAsync();

                var studentIdsOnHoliday = holidays
                    .Where(h => eligibleStudentIds.Contains(h.StudentNationalId))
                    .Select(h => h.StudentNationalId)
                    .Distinct()
                    .ToList();

                var studentsNotOnHoliday = eligibleStudents.Count - studentIdsOnHoliday.Count;

                var mealTransactions = await _unitOfWork.MealTransactions
                    .Query()
                    .Where(m => m.Date.Date == date.Date &&
                               m.DormLocationId == dormLocationId &&
                               eligibleStudentIds.Contains(m.StudentNationalId))
                    .Include(m => m.MealType)
                    .ToListAsync();

                var breakfastDinnerReceived = mealTransactions
                    .Count(m => m.MealType.Name == "BreakfastDinner");
                var breakfastDinnerRemaining = studentsNotOnHoliday - breakfastDinnerReceived;

                var lunchReceived = mealTransactions
                    .Count(m => m.MealType.Name == "Lunch");
                var lunchRemaining = studentsNotOnHoliday - lunchReceived;

                var totalMealsExpected = studentsNotOnHoliday * 2;
                var totalMealsReceived = breakfastDinnerReceived + lunchReceived;
                var totalMealsRemaining = breakfastDinnerRemaining + lunchRemaining;

                _logger.LogDebug("Restaurant report calculated: EligibleStudents={EligibleStudents}, StudentsOnHoliday={StudentsOnHoliday}, MealsReceived={MealsReceived}",
                    eligibleStudents.Count, studentIdsOnHoliday.Count, totalMealsReceived);

                return new RestaurantDailyReportDto
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating restaurant daily report: Date={Date}", date.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        private decimal CalculateAttendanceRate(
            List<Domain.Entities.Student> students,
            List<Domain.Entities.MealTransaction> transactions,
            DateTime fromDate,
            DateTime toDate)
        {
            var totalDays = (toDate.Date - fromDate.Date).Days + 1;
            var expectedMeals = students.Count * totalDays * 3;

            if (expectedMeals == 0) return 0;

            var studentIds = students.Select(s => s.StudentId).ToList();
            var actualMeals = transactions.Count(t => studentIds.Contains(t.StudentNationalId));

            return (decimal)actualMeals / expectedMeals * 100;
        }

        public async Task<DailyAbsenceReportDto> GetDailyAbsenceReportAsync(DateTime date)
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            _logger.LogDebug("Generating daily absence report: Date={Date}", date.ToString("yyyy-MM-dd"));

            try
            {
                var students = await _unitOfWork.Students
                    .Query()
                    .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted)
                    .ToListAsync();

                var paymentExemptions = await _unitOfWork.PaymentExemptions
                    .Query()
                    .Where(pe => pe.IsActive &&
                                pe.StartDate.Date <= date.Date &&
                                pe.EndDate.Date >= date.Date)
                    .ToListAsync();

                var eligibleStudents = students
                    .Where(s =>
                    {
                        if (s.IsExemptFromFees) return true;
                        if (s.HasOutstandingPayment)
                            return paymentExemptions.Any(pe => pe.StudentNationalId == s.NationalId);
                        return true;
                    })
                    .ToList();

                var holidays = await _unitOfWork.Holidays
                    .Query()
                    .Where(h => h.StartDate.Date <= date.Date &&
                               h.EndDate.Date >= date.Date &&
                               !h.IsDeleted)
                    .ToListAsync();

                var studentIdsOnHoliday = holidays.Select(h => h.StudentNationalId).Distinct().ToList();

                var studentsExpectedToEat = eligibleStudents
                    .Where(s => !studentIdsOnHoliday.Contains(s.NationalId))
                    .ToList();

                var mealTransactions = await _unitOfWork.MealTransactions
                    .Query()
                    .Where(m => m.Date.Date == date.Date && m.DormLocationId == dormLocationId)
                    .Include(m => m.MealType)
                    .ToListAsync();

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

                var totalMissedBreakfastDinner = buildingGroups
                    .Sum(b => b.Students.Count(s => s.MissedBreakfastDinner));
                var totalMissedLunch = buildingGroups
                    .Sum(b => b.Students.Count(s => s.MissedLunch));
                var totalMissedMeals = totalMissedBreakfastDinner + totalMissedLunch;

                _logger.LogDebug("Daily absence report generated: TotalStudents={TotalStudents}, StudentsWhoDidntEat={StudentsWhoDidntEat}, TotalMissedMeals={TotalMissedMeals}",
                    students.Count, buildingGroups.Sum(b => b.StudentsWhoDidntEat), totalMissedMeals);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily absence report: Date={Date}", date.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<MonthlyAbsenceReportDto> GetMonthlyAbsenceReportAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            var dormLocationId = await _authService.GetSelectedDormLocationIdAsync();

            _logger.LogDebug("Generating monthly absence report: FromDate={FromDate}, ToDate={ToDate}",
                fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));

            try
            {
                var students = await _unitOfWork.Students
                    .Query()
                    .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted)
                    .ToListAsync();

                var paymentExemptions = await _unitOfWork.PaymentExemptions
                    .Query()
                    .Where(pe => pe.IsActive &&
                                ((pe.StartDate.Date <= toDate.Date && pe.EndDate.Date >= fromDate.Date)))
                    .ToListAsync();

                var mealTransactions = await _unitOfWork.MealTransactions
                    .Query()
                    .Where(m => m.Date.Date >= fromDate.Date &&
                               m.Date.Date <= toDate.Date &&
                               m.DormLocationId == dormLocationId)
                    .Include(m => m.MealType)
                    .ToListAsync();

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

                _logger.LogDebug("Monthly absence report generated: TotalDays={TotalDays}, TotalStudentsWithAbsences={TotalStudentsWithAbsences}, TotalMissedMeals={TotalMissedMeals}",
                    totalDays, buildingGroups.Sum(b => b.Students.Count), buildingGroups.Sum(b => b.Students.Sum(s => s.TotalMissedMeals)));

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly absence report: FromDate={FromDate}, ToDate={ToDate}",
                    fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));
                throw;
            }
        }
    }
}