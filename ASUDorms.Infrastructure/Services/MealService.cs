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
            var today = DateTime.Today;
            var now = DateTime.Now;

            // 1. Find student by national ID
            var student = await _unitOfWork.Students.Query()
                .Where(s => s.NationalId == request.NationalId)
                .FirstOrDefaultAsync();

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
                .AnyAsync(h => h.StudentNationalId == student.NationalId &&
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

            // 6. Payment and Exemption Logic
            // Check if student has outstanding payment
            if (student.HasOutstandingPayment)
            {
                // If student is exempt from fees, allow meal regardless of payment
                if (student.IsExemptFromFees)
                {
                    // Allow meal - student is exempt from all fees
                    // Continue to next checks
                }
                else
                {
                    // Check for valid payment exemption
                    var hasValidExemption = await _unitOfWork.PaymentExemptions.Query()
                        .AnyAsync(pe => pe.StudentNationalId == student.NationalId &&
                                       pe.IsActive &&
                                       pe.StartDate.Date <= today &&
                                       pe.EndDate.Date >= today);

                    if (!hasValidExemption)
                    {
                        return new MealScanResultDto
                        {
                            Success = false,
                            Message = "يوجد مستحقات مالية معلقة - يرجى مراجعة الإدارة",
                            Student = MapToStudentDto(student)
                        };
                    }
                    // If has valid exemption, allow meal
                }
            }

            // 7. Check if student already received this meal today and get the previous transaction
            var previousTransaction = await _unitOfWork.MealTransactions.Query()
                .Where(mt => mt.StudentNationalId == student.NationalId &&
                           mt.MealTypeId == request.MealTypeId &&
                           mt.Date.Date == today)
                .FirstOrDefaultAsync();

            if (previousTransaction != null)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "تم تناول هذه الوجبة بالفعل اليوم",
                    Student = MapToStudentDto(student, previousTransaction.Date) // Pass previous time
                };
            }

            // 8. Create meal transaction
            var mealTransaction = new MealTransaction
            {
                StudentNationalId = student.NationalId,
                StudentId = student.StudentId,
                MealTypeId = request.MealTypeId,
                Date = now,
                DormLocationId = dormLocationId,
                ScannedByUserId = dormLocationId,
                Time = now.TimeOfDay
            };

            await _unitOfWork.MealTransactions.AddAsync(mealTransaction);
            await _unitOfWork.SaveChangesAsync();

            // 9. Return success
            return new MealScanResultDto
            {
                Success = true,
                Message = "تم مسح الوجبة بنجاح",
                Student = MapToStudentDto(student, now) // Pass current time
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

        private StudentScanDto MapToStudentDto(Student student,DateTime? scanTime = null)
        {
            return new StudentScanDto
            {
                StudentId = student.StudentId,
                NationalId=student.NationalId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                BuildingNumber = student.BuildingNumber,
                PhotoUrl = student.PhotoUrl,
                timeScanned = scanTime // Add this property to your DTO
            };
        }
    }
}