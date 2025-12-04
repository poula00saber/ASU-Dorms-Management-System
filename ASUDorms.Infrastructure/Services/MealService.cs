using ASUDorms.Application.DTOs.Meals;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class MealService : IMealService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public MealService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<MealScanResultDto> ScanMealAsync(MealScanRequestDto request)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            Console.WriteLine(dormLocationId);
            var today = DateTime.Today;

            // 1. Find student by national ID
            var student = await _unitOfWork.Students.Query()
                .Where(s => s.NationalId == request.NationalId)
                .FirstOrDefaultAsync();



            Console.WriteLine("============================================================");
            Console.WriteLine("Received NationalId: " + request.NationalId);
            Console.WriteLine("============================================================");


            if (student == null)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "لم يتم العثور على الطالب"
                };
            }

            // 2. Check if student is deleted
            if (student.IsDeleted)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "حساب الطالب غير نشط",
                    Student = MapToStudentDto(student)
                };
            }

            // 3. Check if student belongs to same dorm location
            if (student.DormLocationId != dormLocationId)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "الطالب لا ينتمي لهذا السكن",
                    Student = MapToStudentDto(student)
                };
            }

            // 4. Check if time is valid for this meal type
            if (!await IsTimeValidForMealTypeAsync(request.MealTypeId))
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "خارج أوقات الوجبة",
                    Student = MapToStudentDto(student)
                };
            }

            // 5. Check if student is on holiday today
            var isOnHoliday = await _unitOfWork.Holidays.Query()
                .AnyAsync(h => h.StudentNationalId == student.NationalId&&
                              h.StartDate.Date <= today &&
                              h.EndDate.Date >= today);

            if (isOnHoliday)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "الطالب في إجازة",
                    Student = MapToStudentDto(student)
                };
            }

            // 6. Check if student already received this meal today
            var alreadyAte = await _unitOfWork.MealTransactions.Query()
                .AnyAsync(mt => mt.StudentNationalId == student.NationalId &&
                               mt.MealTypeId == request.MealTypeId &&
                               mt.Date.Date == today);

            if (alreadyAte)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "تم تناول هذه الوجبة بالفعل اليوم",
                    Student = MapToStudentDto(student)
                };
            }
            
            // 7. Create meal transaction
            var mealTransaction = new MealTransaction
            {
                StudentNationalId = student.NationalId,
                MealTypeId = request.MealTypeId,
                Date = DateTime.Now,
                DormLocationId = dormLocationId,
                ScannedByUserId = dormLocationId
            };

            await _unitOfWork.MealTransactions.AddAsync(mealTransaction);
            await _unitOfWork.SaveChangesAsync();

            // 8. Return success
            return new MealScanResultDto
            {
                Success = true,
                Message = "تم مسح الوجبة بنجاح",
                Student = MapToStudentDto(student)
            };
        }

        public async Task<bool> IsTimeValidForMealTypeAsync(int mealTypeId)
        {
            var currentTime = DateTime.Now.TimeOfDay;

            // MealTypeId 1: Breakfast+Dinner (7:00 AM - 10:00 AM OR 6:00 PM - 9:00 PM)
            if (mealTypeId == 1)
            {
                var morningStart = new TimeSpan(7, 0, 0);   // 7:00 AM
                var morningEnd = new TimeSpan(10, 0, 0);    // 10:00 AM
                var eveningStart = new TimeSpan(18, 0, 0);  // 6:00 PM
                var eveningEnd = new TimeSpan(21, 0, 0);    // 9:00 PM

                return (currentTime >= morningStart && currentTime <= morningEnd) ||
                       (currentTime >= eveningStart && currentTime <= eveningEnd);
            }

            // MealTypeId 2: Lunch (1:00 PM - 9:00 PM)
            if (mealTypeId == 2)
            {
                var startTime = new TimeSpan(13, 0, 0);  // 1:00 PM
                var endTime = new TimeSpan(24, 0, 0);    // 9:00 PM
                return currentTime >= startTime && currentTime <= endTime;
            }

            return false;
        }

        private StudentScanDto MapToStudentDto(Student student)
        {
            return new StudentScanDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                BuildingNumber = student.BuildingNumber,
                PhotoUrl = student.PhotoUrl,
                timeScanned = DateTime.Now
            };
        }
    }
}