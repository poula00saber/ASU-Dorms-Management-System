using ASUDorms.Application.DTOs.Meals;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class MealService : IMealService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly ILogger<MealService> _logger;

        public MealService(IUnitOfWork unitOfWork, IAuthService authService, ILogger<MealService> logger)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _logger = logger;
        }

        public async Task<MealScanResultDto> ScanCombinedMealAsync(MealScanRequestDto request)
        {
            var nationalIdHash = HashString(request.NationalId);

            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var today = DateTime.Today;
            var now = DateTime.Now;

            // Check if combined meal scanning is allowed for this dorm location
            var dormLocation = await _unitOfWork.DormLocations.GetByIdAsync(dormLocationId);
            if (dormLocation == null || !dormLocation.AllowCombinedMealScan)
            {
                _logger.LogWarning("Combined scan not allowed: DormLocationId={DormLocationId}, NationalIdHash={NationalIdHash}",
                    dormLocationId, nationalIdHash);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "المسح المجمع للوجبات غير مسموح به في هذا السكن"
                };
            }

            // Find student by national ID
            var student = await _unitOfWork.Students.Query()
                .Where(s => s.NationalId == request.NationalId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                _logger.LogWarning("Student not found: NationalIdHash={NationalIdHash}", nationalIdHash);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "لم يتم العثور على الطالب"
                };
            }

            if (student.IsDeleted)
            {
                _logger.LogWarning("Student account inactive: StudentId={StudentId}, NationalIdHash={NationalIdHash}",
                    student.StudentId, nationalIdHash);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "حساب الطالب غير نشط",
                    Student = MapToStudentDto(student)
                };
            }

            if (student.DormLocationId != dormLocationId)
            {
                _logger.LogWarning("Wrong dorm location: StudentId={StudentId}, StudentDormLocation={StudentDormLocation}, CurrentDormLocation={CurrentDormLocation}",
                    student.StudentId, student.DormLocationId, dormLocationId);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "الطالب لا ينتمي لهذا السكن",
                    Student = MapToStudentDto(student)
                };
            }

            // Check if time is valid for combined meal (Lunch time: 1:00 PM - 9:00 PM)
            var currentTime = now.TimeOfDay;
            var startTime = new TimeSpan(13, 0, 0);
            var endTime = new TimeSpan(21, 0, 0);

            if (currentTime < startTime || currentTime > endTime)
            {
                _logger.LogWarning("Combined scan outside allowed time: CurrentTime={CurrentTime}, StudentId={StudentId}",
                    currentTime, student.StudentId);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "المسح المجمع متاح فقط من 1:00 م إلى 9:00 م",
                    Student = MapToStudentDto(student)
                };
            }

            // Check if student is on holiday today
            var isOnHoliday = await _unitOfWork.Holidays.Query()
                .AnyAsync(h => h.StudentNationalId == student.NationalId &&
                              h.StartDate.Date <= today &&
                              h.EndDate.Date >= today &&
                              !h.IsDeleted);

            if (isOnHoliday)
            {
                _logger.LogInformation("Student on holiday: StudentId={StudentId}", student.StudentId);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "الطالب في إجازة",
                    Student = MapToStudentDto(student)
                };
            }

            // Payment and Exemption Logic
            if (student.HasOutstandingPayment)
            {
                if (!student.IsExemptFromFees)
                {
                    var hasValidExemption = await _unitOfWork.PaymentExemptions.Query()
                        .AnyAsync(pe => pe.StudentNationalId == student.NationalId &&
                                       pe.IsActive &&
                                       pe.StartDate.Date <= today &&
                                       pe.EndDate.Date >= today);

                    if (!hasValidExemption)
                    {
                        _logger.LogWarning("Outstanding payment: StudentId={StudentId}", student.StudentId);
                        return new MealScanResultDto
                        {
                            Success = false,
                            Message = "يوجد مستحقات مالية معلقة - يرجى مراجعة الإدارة",
                            Student = MapToStudentDto(student)
                        };
                    }
                }
            }

            // Check if student already received meals today
            var existingTransactions = await _unitOfWork.MealTransactions.Query()
                .Where(mt => mt.StudentNationalId == student.NationalId &&
                           mt.Date.Date == today)
                .ToListAsync();

            var hasBreakfastDinner = existingTransactions.Any(mt => mt.MealTypeId == 1);
            var hasLunch = existingTransactions.Any(mt => mt.MealTypeId == 2);

            if (hasBreakfastDinner && hasLunch)
            {
                _logger.LogInformation("All meals already received: StudentId={StudentId}", student.StudentId);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "تم تناول جميع الوجبات بالفعل اليوم",
                    Student = MapToStudentDto(student, existingTransactions.First().Date)
                };
            }

            // Create meal transactions for both meals
            var transactionsCreated = 0;

            if (!hasBreakfastDinner)
            {
                var breakfastDinnerTransaction = new MealTransaction
                {
                    StudentNationalId = student.NationalId,
                    StudentId = student.StudentId,
                    MealTypeId = 1,
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
                    MealTypeId = 2,
                    Date = now,
                    DormLocationId = dormLocationId,
                    ScannedByUserId = dormLocationId,
                    Time = now.TimeOfDay
                };
                await _unitOfWork.MealTransactions.AddAsync(lunchTransaction);
                transactionsCreated++;
            }

            await _unitOfWork.SaveChangesAsync();

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
            var nationalIdHash = HashString(request.NationalId);

            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var today = DateTime.Today;
            var now = DateTime.Now;

            // Find student by national ID
            var student = await _unitOfWork.Students.Query()
                .Where(s => s.NationalId == request.NationalId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                _logger.LogWarning("Student not found: NationalIdHash={NationalIdHash}", nationalIdHash);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "لم يتم العثور على الطالب"
                };
            }

            if (student.IsDeleted)
            {
                _logger.LogWarning("Student account inactive: StudentId={StudentId}, NationalIdHash={NationalIdHash}",
                    student.StudentId, nationalIdHash);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "حساب الطالب غير نشط",
                    Student = MapToStudentDto(student)
                };
            }

            if (student.DormLocationId != dormLocationId)
            {
                _logger.LogWarning("Wrong dorm location: StudentId={StudentId}, StudentDormLocation={StudentDormLocation}, CurrentDormLocation={CurrentDormLocation}",
                    student.StudentId, student.DormLocationId, dormLocationId);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "الطالب لا ينتمي لهذا السكن",
                    Student = MapToStudentDto(student)
                };
            }

            if (!await IsTimeValidForMealTypeAsync(request.MealTypeId))
            {
                _logger.LogWarning("Meal time invalid: MealType={MealType}, StudentId={StudentId}",
                    request.MealTypeId, student.StudentId);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "خارج أوقات الوجبة",
                    Student = MapToStudentDto(student)
                };
            }

            // Check if student is on holiday today
            var isOnHoliday = await _unitOfWork.Holidays.Query()
                .AnyAsync(h => h.StudentNationalId == student.NationalId &&
                              h.StartDate.Date <= today &&
                              h.EndDate.Date >= today &&
                              !h.IsDeleted);

            if (isOnHoliday)
            {
                _logger.LogInformation("Student on holiday: StudentId={StudentId}", student.StudentId);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "الطالب في إجازة",
                    Student = MapToStudentDto(student)
                };
            }

            // Payment and Exemption Logic
            if (student.HasOutstandingPayment)
            {
                if (!student.IsExemptFromFees)
                {
                    var hasValidExemption = await _unitOfWork.PaymentExemptions.Query()
                        .AnyAsync(pe => pe.StudentNationalId == student.NationalId &&
                                       pe.IsActive &&
                                       pe.StartDate.Date <= today &&
                                       pe.EndDate.Date >= today);

                    if (!hasValidExemption)
                    {
                        _logger.LogWarning("Outstanding payment: StudentId={StudentId}", student.StudentId);
                        return new MealScanResultDto
                        {
                            Success = false,
                            Message = "يوجد مستحقات مالية معلقة - يرجى مراجعة الإدارة",
                            Student = MapToStudentDto(student)
                        };
                    }
                }
            }

            // Check if student already received this meal today
            var previousTransaction = await _unitOfWork.MealTransactions.Query()
                .Where(mt => mt.StudentNationalId == student.NationalId &&
                           mt.MealTypeId == request.MealTypeId &&
                           mt.Date.Date == today)
                .FirstOrDefaultAsync();

            if (previousTransaction != null)
            {
                _logger.LogInformation("Meal already received: StudentId={StudentId}, MealType={MealType}",
                    student.StudentId, request.MealTypeId);
                return new MealScanResultDto
                {
                    Success = false,
                    Message = "تم تناول هذه الوجبة بالفعل اليوم",
                    Student = MapToStudentDto(student, previousTransaction.Date)
                };
            }

            // Create meal transaction
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

            if (mealTypeId == 1)
            {
                var eveningStart = new TimeSpan(17, 0, 0);
                var eveningEnd = new TimeSpan(21, 0, 0);
                return currentTime >= eveningStart && currentTime <= eveningEnd;
            }

            if (mealTypeId == 2)
            {
                var startTime = new TimeSpan(13, 0, 0);
                var endTime = new TimeSpan(21, 0, 0);
                return currentTime >= startTime && currentTime <= endTime;
            }

            return false;
        }

        public async Task<MealSettingsDto> GetMealSettingsAsync()
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var dormLocation = await _unitOfWork.DormLocations.GetByIdAsync(dormLocationId);

            return new MealSettingsDto
            {
                AllowCombinedMealScan = dormLocation?.AllowCombinedMealScan ?? false
            };
        }

        public async Task<bool> UpdateMealSettingsAsync(UpdateMealSettingsDto dto)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var dormLocation = await _unitOfWork.DormLocations.GetByIdAsync(dormLocationId);

            if (dormLocation == null)
            {
                _logger.LogError("Dorm location not found: DormLocationId={DormLocationId}", dormLocationId);
                return false;
            }

            dormLocation.AllowCombinedMealScan = dto.AllowCombinedMealScan;
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

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

        public async Task<bool> UpdateDormLocationMealSettingAsync(UpdateDormLocationMealSettingDto dto)
        {
            var dormLocation = await _unitOfWork.DormLocations.GetByIdAsync(dto.DormLocationId);

            if (dormLocation == null)
            {
                _logger.LogError("Dorm location not found: DormLocationId={DormLocationId}", dto.DormLocationId);
                return false;
            }

            dormLocation.AllowCombinedMealScan = dto.AllowCombinedMealScan;
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

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

        private string HashString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "null";

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes)[..8];
        }
    }
}