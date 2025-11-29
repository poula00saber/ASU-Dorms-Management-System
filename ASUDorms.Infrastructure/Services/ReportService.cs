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
        private const decimal MEAL_PENALTY_AMOUNT = 10.00m; // Configurable penalty per meal

        public ReportService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<MealAbsenceReportDto> GetMealAbsenceReportAsync(
            DateTime date,
            string buildingNumber = null,
            string government = null,
            string district = null,
            string faculty = null)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

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

            // Get all meal transactions for the date
            var mealTransactions = await _unitOfWork.MealTransactions
                .Query()
                .Where(m => m.Date.Date == date.Date && m.DormLocationId == dormLocationId)
                .Include(m => m.MealType)
                .ToListAsync();

            // Get holidays for the date
            var holidays = await _unitOfWork.Holidays
                .Query()
                .Where(h => h.StartDate.Date <= date.Date && h.EndDate.Date >= date.Date)
                .ToListAsync();

            var absences = new List<StudentAbsenceDto>();
            var totalPenalty = 0m;

            foreach (var student in students)
            {
                // Check if student was on holiday
                var wasOnHoliday = holidays.Any(h => h.StudentId == student.StudentId);

                // Get meals this student received
                var studentMeals = mealTransactions
                    .Where(m => m.StudentId == student.StudentId)
                    .Select(m => m.MealType.DisplayName)
                    .ToList();

                // All possible meals
                var allMeals = new List<string> { "Breakfast & Dinner", "Lunch" };
                var missedMeals = allMeals.Except(studentMeals).ToList();

                // If student missed any meal and was not on holiday, add to report
                if (missedMeals.Any())
                {
                    var penalty = wasOnHoliday ? 0m : (missedMeals.Count * MEAL_PENALTY_AMOUNT);
                    totalPenalty += penalty;

                    absences.Add(new StudentAbsenceDto
                    {
                        StudentId = student.StudentId,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        BuildingNumber = student.BuildingNumber,
                        RoomNumber = student.RoomNumber,
                        Faculty = student.Faculty,
                        Grade = student.Grade,
                        WasOnHoliday = wasOnHoliday,
                        Penalty = penalty,
                        MissedMeals = missedMeals
                    });
                }
            }

            return new MealAbsenceReportDto
            {
                Date = date,
                Absences = absences,
                TotalPenalty = totalPenalty,
                TotalAbsences = absences.Count
            };
        }
    }
}
