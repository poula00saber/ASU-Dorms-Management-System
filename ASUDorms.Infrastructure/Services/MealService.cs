using ASUDorms.Application.DTOs.Meals;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class MealService : IMealService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly IStudentService _studentService;

        public MealService(
            IUnitOfWork unitOfWork,
            IAuthService authService,
            IStudentService studentService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _studentService = studentService;
        }

        public async Task<MealScanResultDto> ScanMealAsync(MealScanRequestDto request)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();
            var currentUser = await _authService.GetCurrentUserAsync();

            // 1. Check if student exists and belongs to same location
            var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId);

            if (student == null)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "Student not found"
                };
            }

            if (student.DormLocationId != dormLocationId)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "Student does not belong to this dorm location"
                };
            }

            // 2. Check if time is valid for this meal type
            if (!await IsTimeValidForMealTypeAsync(request.MealTypeId))
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "This meal type is not available at this time"
                };
            }

            // 3. Check if student is on holiday today
            var today = DateTime.Today;
            var holidays = await _unitOfWork.Holidays.FindAsync(h =>
                h.StudentId == request.StudentId &&
                h.StartDate.Date <= today &&
                h.EndDate.Date >= today);

            if (holidays.Any())
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "Student is currently on holiday"
                };
            }

            // 4. Check if student already received this meal today
            var existingMeal = await _unitOfWork.MealTransactions
                .Query()
                .AnyAsync(m =>
                    m.StudentId == request.StudentId &&
                    m.MealTypeId == request.MealTypeId &&
                    m.Date.Date == today);

            if (existingMeal)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "Student already received this meal today"
                };
            }

            // 5. Record the meal transaction
            var mealTransaction = new MealTransaction
            {
                StudentId = request.StudentId,
                MealTypeId = request.MealTypeId,
                Date = DateTime.UtcNow,
                Time = DateTime.UtcNow.TimeOfDay,
                DormLocationId = dormLocationId,
                ScannedByUserId = currentUser.Id
            };

            await _unitOfWork.MealTransactions.AddAsync(mealTransaction);
            await _unitOfWork.SaveChangesAsync();

            // 6. Return success with student details
            var studentDto = await _studentService.GetStudentByIdAsync(request.StudentId);

            return new MealScanResultDto
            {
                Success = true,
                Message = "Meal recorded successfully",
                Student = studentDto
            };
        }

        public async Task<bool> IsTimeValidForMealTypeAsync(int mealTypeId)
        {
            var currentTime = DateTime.Now.TimeOfDay;

            // MealTypeId 1: Breakfast+Dinner (6:00 PM - 9:00 PM)
            if (mealTypeId == 1)
            {
                var startTime = new TimeSpan(18, 0, 0); // 6:00 PM
                var endTime = new TimeSpan(21, 0, 0);   // 9:00 PM
                return currentTime >= startTime && currentTime <= endTime;
            }

            // MealTypeId 2: Lunch (1:00 PM - 9:00 PM)
            if (mealTypeId == 2)
            {
                var startTime = new TimeSpan(13, 0, 0); // 1:00 PM
                var endTime = new TimeSpan(21, 0, 0);   // 9:00 PM
                return currentTime >= startTime && currentTime <= endTime;
            }

            return false;
        }
    }
}
