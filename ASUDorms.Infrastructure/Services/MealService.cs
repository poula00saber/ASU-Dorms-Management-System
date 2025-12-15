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

        // NEW METHOD - Scan both meals together
        public async Task<MealScanResultDto> ScanCombinedMealAsync(MealScanRequestDto request)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var today = DateTime.Today;
            var now = DateTime.Now;

            // Check if combined meal scanning is allowed for this dorm location
            var dormLocation = await _unitOfWork.DormLocations.GetByIdAsync(dormLocationId);
            if (dormLocation == null || !dormLocation.AllowCombinedMealScan)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "المسح المجمع للوجبات غير مسموح به في هذا السكن"
                };
            }

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

            // 4. Check if time is valid for combined meal (Lunch time: 1:00 PM - 9:00 PM)
            var currentTime = now.TimeOfDay;
            var startTime = new TimeSpan(13, 0, 0);  // 1:00 PM
            var endTime = new TimeSpan(21, 0, 0);    // 9:00 PM

            if (currentTime < startTime || currentTime > endTime)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "المسح المجمع متاح فقط من 1:00 م إلى 9:00 م",
                    Student = MapToStudentDto(student)
                };
            }

            // 5. Check if student is on holiday today
            var isOnHoliday = await _unitOfWork.Holidays.Query()
                .AnyAsync(h => h.StudentNationalId == student.NationalId &&
                              h.StartDate.Date <= today &&
                              h.EndDate.Date >= today &&
                              !h.IsDeleted);

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
            if (student.HasOutstandingPayment)
            {
                if (student.IsExemptFromFees)
                {
                    // Allow meal - student is exempt from all fees
                }
                else
                {
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
                }
            }

            // 7. Check if student already received meals today
            var existingTransactions = await _unitOfWork.MealTransactions.Query()
                .Where(mt => mt.StudentNationalId == student.NationalId &&
                           mt.Date.Date == today)
                .ToListAsync();

            var hasBreakfastDinner = existingTransactions.Any(mt => mt.MealTypeId == 1);
            var hasLunch = existingTransactions.Any(mt => mt.MealTypeId == 2);

            if (hasBreakfastDinner && hasLunch)
            {
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "تم تناول جميع الوجبات بالفعل اليوم",
                    Student = MapToStudentDto(student, existingTransactions.First().Date)
                };
            }

            // 8. Create meal transactions for both meals
            var transactionsCreated = 0;

            if (!hasBreakfastDinner)
            {
                var breakfastDinnerTransaction = new MealTransaction
                {
                    StudentNationalId = student.NationalId,
                    StudentId = student.StudentId,
                    MealTypeId = 1, // BreakfastDinner
                    Date = now,
                    DormLocationId = dormLocationId,
                    ScannedByUserId = dormLocationId,
                    Time = now.TimeOfDay
                };
                await _unitOfWork.MealTransactions.AddAsync(breakfastDinnerTransaction);
                transactionsCreated++;
            }

            if (!hasLunch)
            {
                var lunchTransaction = new MealTransaction
                {
                    StudentNationalId = student.NationalId,
                    StudentId = student.StudentId,
                    MealTypeId = 2, // Lunch
                    Date = now,
                    DormLocationId = dormLocationId,
                    ScannedByUserId = dormLocationId,
                    Time = now.TimeOfDay
                };
                await _unitOfWork.MealTransactions.AddAsync(lunchTransaction);
                transactionsCreated++;
            }

            await _unitOfWork.SaveChangesAsync();

            // 9. Return success with appropriate message
            var message = transactionsCreated == 2
                ? "تم مسح الوجبتين بنجاح (الإفطار/العشاء + الغداء)"
                : transactionsCreated == 1 && !hasBreakfastDinner
                    ? "تم مسح الإفطار/العشاء بنجاح (الغداء تم مسحه مسبقاً)"
                    : "تم مسح الغداء بنجاح (الإفطار/العشاء تم مسحه مسبقاً)";

            return new MealScanResultDto
            {
                Success = true,
                Message = message,
                Student = MapToStudentDto(student, now)
            };
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
                              h.EndDate.Date >= today &&
                              !h.IsDeleted);

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
            if (student.HasOutstandingPayment)
            {
                if (student.IsExemptFromFees)
                {
                    // Allow meal - student is exempt from all fees
                }
                else
                {
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
                }
            }

            // 7. Check if student already received this meal today
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
                    Student = MapToStudentDto(student, previousTransaction.Date)
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
                Student = MapToStudentDto(student, now)
            };
        }

        public async Task<bool> IsTimeValidForMealTypeAsync(int mealTypeId)
        {
            var currentTime = DateTime.Now.TimeOfDay;

            // MealTypeId 1: Breakfast+Dinner (ONLY 5:00 PM - 9:00 PM)
            if (mealTypeId == 1)
            {
                var eveningStart = new TimeSpan(17, 0, 0);  // 5:00 PM
                var eveningEnd = new TimeSpan(21, 0, 0);    // 9:00 PM

                return currentTime >= eveningStart && currentTime <= eveningEnd;
            }

            // MealTypeId 2: Lunch (1:00 PM - 9:00 PM)
            if (mealTypeId == 2)
            {
                var startTime = new TimeSpan(13, 0, 0);  // 1:00 PM
                var endTime = new TimeSpan(21, 0, 0);    // 9:00 PM
                return currentTime >= startTime && currentTime <= endTime;
            }

            return false;
        }

        // NEW METHOD - Get meal settings
        public async Task<MealSettingsDto> GetMealSettingsAsync()
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var dormLocation = await _unitOfWork.DormLocations.GetByIdAsync(dormLocationId);

            return new MealSettingsDto
            {
                AllowCombinedMealScan = dormLocation?.AllowCombinedMealScan ?? false
            };
        }

        // NEW METHOD - Update meal settings (Registration only)
        public async Task<bool> UpdateMealSettingsAsync(UpdateMealSettingsDto dto)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var dormLocation = await _unitOfWork.DormLocations.GetByIdAsync(dormLocationId);

            if (dormLocation == null)
                return false;

            dormLocation.AllowCombinedMealScan = dto.AllowCombinedMealScan;
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // NEW METHOD - Get all dorm locations settings (Registration only)
        public async Task<AllDormLocationSettingsDto> GetAllDormLocationsSettingsAsync()
        {
            var dormLocations = await _unitOfWork.DormLocations
                .Query()
                .OrderBy(d => d.Id)
                .ToListAsync();

            return new AllDormLocationSettingsDto
            {
                DormLocations = dormLocations.Select(d => new DormLocationMealSettingsDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    AllowCombinedMealScan = d.AllowCombinedMealScan,
                    IsActive = d.IsActive
                }).ToList()
            };
        }

        // NEW METHOD - Update specific dorm location setting (Registration only)
        public async Task<bool> UpdateDormLocationMealSettingAsync(UpdateDormLocationMealSettingDto dto)
        {
            var dormLocation = await _unitOfWork.DormLocations.GetByIdAsync(dto.DormLocationId);

            if (dormLocation == null)
                return false;

            dormLocation.AllowCombinedMealScan = dto.AllowCombinedMealScan;
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // NEW METHOD - Bulk update all dorm locations (Registration only)
        public async Task<bool> BulkUpdateAllDormLocationsAsync(BulkUpdateMealSettingsDto dto)
        {
            var dormLocations = await _unitOfWork.DormLocations
                .Query()
                .Where(d => d.IsActive)
                .ToListAsync();

            foreach (var location in dormLocations)
            {
                location.AllowCombinedMealScan = dto.AllowCombinedMealScan;
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private StudentScanDto MapToStudentDto(Student student, DateTime? scanTime = null)
        {
            return new StudentScanDto
            {
                StudentId = student.StudentId,
                NationalId = student.NationalId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                BuildingNumber = student.BuildingNumber,
                PhotoUrl = student.PhotoUrl,
                timeScanned = scanTime
            };
        }
    }
}