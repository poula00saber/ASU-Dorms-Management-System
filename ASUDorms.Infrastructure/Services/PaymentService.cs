using ASUDorms.Application.DTOs.Payments;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Enums;
using ASUDorms.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork unitOfWork, IAuthService authService, ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _logger = logger;
        }

        // Payment Transactions

        public async Task<PaymentTransactionDto> CreatePaymentTransactionAsync(CreatePaymentTransactionDto dto)
        {
            var nationalIdHash = HashString(dto.StudentNationalId);

            _logger.LogInformation("Creating payment transaction: NationalIdHash={NationalIdHash}, Amount={Amount}, Type={PaymentType}",
                nationalIdHash, dto.Amount, dto.PaymentType);

            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

            // Find student
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == dto.StudentNationalId && !s.IsDeleted);

            if (student == null)
            {
                _logger.LogWarning("Student not found for payment: NationalIdHash={NationalIdHash}", nationalIdHash);
                throw new KeyNotFoundException($"الطالب بالرقم القومي '{dto.StudentNationalId}' غير موجود");
            }

            // Validate payment type specific fields
            ValidatePaymentTypeFields(dto);

            var paymentTransaction = new PaymentTransaction
            {
                DormLocationId = dormLocationId,
                StudentNationalId = dto.StudentNationalId,
                StudentId = student.StudentId,
                Amount = dto.Amount,
                PaymentType = dto.PaymentType,
                PaymentDate = dto.PaymentDate.Date,
                ReceiptNumber = dto.ReceiptNumber,
                Month = dto.Month,
                Year = dto.Year,
                MissedMealsCount = dto.MissedMealsCount,
            };

            // Update student's outstanding payment status
            await UpdateStudentPaymentStatus(student, dto.Amount);

            await _unitOfWork.PaymentTransactions.AddAsync(paymentTransaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Payment transaction created: Id={PaymentId}, StudentId={StudentId}, Amount={Amount}",
                paymentTransaction.Id, student.StudentId, dto.Amount);

            return MapToPaymentTransactionDto(paymentTransaction, student);
        }

        public async Task<PaymentTransactionDto> GetPaymentTransactionByIdAsync(int id)
        {
            var payment = await _unitOfWork.PaymentTransactions
                .Query()
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (payment == null)
            {
                _logger.LogWarning("Payment transaction not found: PaymentId={PaymentId}", id);
                throw new KeyNotFoundException($"عملية الدفع بالرقم {id} غير موجودة");
            }

            return MapToPaymentTransactionDto(payment, payment.Student);
        }

        public async Task<List<PaymentTransactionDto>> GetPaymentTransactionsByStudentAsync(string studentNationalId)
        {
            var nationalIdHash = HashString(studentNationalId);

            _logger.LogDebug("Getting payment transactions for student: NationalIdHash={NationalIdHash}", nationalIdHash);

            var payments = await _unitOfWork.PaymentTransactions
                .Query()
                .Include(p => p.Student)
                .Where(p => p.StudentNationalId == studentNationalId && !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} payment transactions for student: NationalIdHash={NationalIdHash}",
                payments.Count, nationalIdHash);

            return payments.Select(p => MapToPaymentTransactionDto(p, p.Student)).ToList();
        }

        public async Task<List<PaymentTransactionDto>> GetPaymentTransactionsAsync(PaymentFilterDto filter)
        {
            _logger.LogDebug("Getting payment transactions with filter");

            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var query = _unitOfWork.PaymentTransactions
                .Query()
                .Include(p => p.Student)
                .Where(p => p.DormLocationId == dormLocationId && !p.IsDeleted);

            if (!string.IsNullOrEmpty(filter.StudentNationalId))
            {
                var studentHash = HashString(filter.StudentNationalId);
                query = query.Where(p => p.StudentNationalId.Contains(filter.StudentNationalId));
                _logger.LogDebug("Filtering by student national ID: NationalIdHash={NationalIdHash}", studentHash);
            }

            if (!string.IsNullOrEmpty(filter.StudentId))
            {
                query = query.Where(p => p.StudentId.Contains(filter.StudentId));
                _logger.LogDebug("Filtering by student ID: StudentId={StudentId}", filter.StudentId);
            }

            if (!string.IsNullOrEmpty(filter.StudentName))
            {
                query = query.Where(p =>
                    p.Student.FirstName.Contains(filter.StudentName) ||
                    p.Student.LastName.Contains(filter.StudentName));
                _logger.LogDebug("Filtering by student name: StudentName={StudentName}", filter.StudentName);
            }

            if (filter.PaymentType.HasValue)
            {
                query = query.Where(p => p.PaymentType == filter.PaymentType.Value);
                _logger.LogDebug("Filtering by payment type: PaymentType={PaymentType}", filter.PaymentType.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= filter.FromDate.Value.Date);
                _logger.LogDebug("Filtering from date: FromDate={FromDate}", filter.FromDate.Value.ToString("yyyy-MM-dd"));
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate <= filter.ToDate.Value.Date);
                _logger.LogDebug("Filtering to date: ToDate={ToDate}", filter.ToDate.Value.ToString("yyyy-MM-dd"));
            }

            if (filter.Month.HasValue)
            {
                query = query.Where(p => p.Month == filter.Month.Value);
                _logger.LogDebug("Filtering by month: Month={Month}", filter.Month.Value);
            }

            if (filter.Year.HasValue)
            {
                query = query.Where(p => p.Year == filter.Year.Value);
                _logger.LogDebug("Filtering by year: Year={Year}", filter.Year.Value);
            }

            if (!string.IsNullOrEmpty(filter.ReceiptNumber))
            {
                query = query.Where(p => p.ReceiptNumber.Contains(filter.ReceiptNumber));
                _logger.LogDebug("Filtering by receipt number: ReceiptNumber={ReceiptNumber}", filter.ReceiptNumber);
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} payment transactions with filter", payments.Count);

            return payments.Select(p => MapToPaymentTransactionDto(p, p.Student)).ToList();
        }

        public async Task DeletePaymentTransactionAsync(int id)
        {
            _logger.LogInformation("Deleting payment transaction: PaymentId={PaymentId}", id);

            var payment = await _unitOfWork.PaymentTransactions.GetByIdAsync(id);
            if (payment == null)
            {
                _logger.LogWarning("Payment transaction not found for deletion: PaymentId={PaymentId}", id);
                throw new KeyNotFoundException($"عملية الدفع بالرقم {id} غير موجودة");
            }

            // Revert student's outstanding payment status
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == payment.StudentNationalId);

            if (student != null)
            {
                student.OutstandingAmount += payment.Amount;
                if (student.OutstandingAmount > 0)
                {
                    student.HasOutstandingPayment = true;
                }
                _unitOfWork.Students.Update(student);
                _logger.LogDebug("Reverted student payment status: StudentId={StudentId}, Amount={Amount}",
                    student.StudentId, payment.Amount);
            }

            // Soft delete
            payment.IsDeleted = true;
            _unitOfWork.PaymentTransactions.Update(payment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Payment transaction deleted: PaymentId={PaymentId}", id);
        }

        // Payment Summary

        public async Task<PaymentSummaryDto> GetPaymentSummaryByStudentAsync(string studentNationalId)
        {
            var nationalIdHash = HashString(studentNationalId);

            _logger.LogDebug("Getting payment summary for student: NationalIdHash={NationalIdHash}", nationalIdHash);

            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == studentNationalId && !s.IsDeleted);

            if (student == null)
            {
                _logger.LogWarning("Student not found for payment summary: NationalIdHash={NationalIdHash}", nationalIdHash);
                throw new KeyNotFoundException($"الطالب بالرقم القومي '{studentNationalId}' غير موجود");
            }

            var payments = await _unitOfWork.PaymentTransactions
                .Query()
                .Where(p => p.StudentNationalId == studentNationalId && !p.IsDeleted)
                .ToListAsync();

            var totalPaid = payments.Sum(p => p.Amount);
            var lastPayment = payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

            var recentTransactions = payments
                .OrderByDescending(p => p.PaymentDate)
                .Take(10)
                .Select(p => MapToPaymentTransactionDto(p, student))
                .ToList();

            _logger.LogDebug("Payment summary retrieved: StudentId={StudentId}, TotalPaid={TotalPaid}, Outstanding={OutstandingAmount}",
                student.StudentId, totalPaid, student.OutstandingAmount);

            return new PaymentSummaryDto
            {
                StudentNationalId = student.NationalId,
                StudentId = student.StudentId,
                StudentName = $"{student.FirstName} {student.LastName}",
                TotalPaid = totalPaid,
                OutstandingAmount = student.OutstandingAmount,
                LastPaymentDate = lastPayment?.PaymentDate,
                RecentTransactions = recentTransactions
            };
        }

        public async Task<decimal> GetTotalPaymentsByStudentAsync(string studentNationalId)
        {
            var payments = await _unitOfWork.PaymentTransactions
                .Query()
                .Where(p => p.StudentNationalId == studentNationalId && !p.IsDeleted)
                .ToListAsync();

            return payments.Sum(p => p.Amount);
        }

        // Payment Exemptions

        public async Task<PaymentExemptionDto> CreatePaymentExemptionAsync(CreatePaymentExemptionDto dto)
        {
            var nationalIdHash = HashString(dto.StudentNationalId);

            _logger.LogInformation("Creating payment exemption: NationalIdHash={NationalIdHash}, StartDate={StartDate}, EndDate={EndDate}",
                nationalIdHash, dto.StartDate.ToString("yyyy-MM-dd"), dto.EndDate.ToString("yyyy-MM-dd"));

            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

            // Find student
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == dto.StudentNationalId && !s.IsDeleted);

            if (student == null)
            {
                _logger.LogWarning("Student not found for exemption: NationalIdHash={NationalIdHash}", nationalIdHash);
                throw new KeyNotFoundException($"الطالب بالرقم القومي '{dto.StudentNationalId}' غير موجود");
            }

            // Validate dates
            if (dto.StartDate > dto.EndDate)
            {
                _logger.LogWarning("Invalid exemption dates: StartDate={StartDate} > EndDate={EndDate}",
                    dto.StartDate.ToString("yyyy-MM-dd"), dto.EndDate.ToString("yyyy-MM-dd"));
                throw new ArgumentException("تاريخ البداية لا يمكن أن يكون بعد تاريخ النهاية");
            }

            // Check for overlapping exemptions
            var existingExemptions = await _unitOfWork.PaymentExemptions
                .Query()
                .Where(e => e.StudentNationalId == dto.StudentNationalId && e.IsActive)
                .ToListAsync();

            var isOverlapping = existingExemptions.Any(e =>
                (dto.StartDate <= e.EndDate && dto.EndDate >= e.StartDate));

            if (isOverlapping)
            {
                _logger.LogWarning("Exemption date overlap: StudentId={StudentId}, ExistingExemptions={Count}",
                    student.StudentId, existingExemptions.Count);
                throw new InvalidOperationException("تواريخ الإعفاء تتداخل مع إعفاء موجود للطالب");
            }

            var paymentExemption = new PaymentExemption
            {
                DormLocationId = dormLocationId,
                StudentNationalId = dto.StudentNationalId,
                StudentId = student.StudentId,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                Notes = dto.Notes,
                IsActive = true,
                ApprovedDate = DateTime.UtcNow
            };

            await _unitOfWork.PaymentExemptions.AddAsync(paymentExemption);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Payment exemption created: Id={ExemptionId}, StudentId={StudentId}",
                paymentExemption.Id, student.StudentId);

            return MapToPaymentExemptionDto(paymentExemption, student);
        }

        public async Task<PaymentExemptionDto> UpdatePaymentExemptionAsync(int id, UpdatePaymentExemptionDto dto)
        {
            _logger.LogInformation("Updating payment exemption: ExemptionId={ExemptionId}", id);

            var exemption = await _unitOfWork.PaymentExemptions.GetByIdAsync(id);
            if (exemption == null)
            {
                _logger.LogWarning("Exemption not found for update: ExemptionId={ExemptionId}", id);
                throw new KeyNotFoundException($"الإعفاء بالرقم {id} غير موجود");
            }

            // Validate dates
            if (dto.StartDate > dto.EndDate)
            {
                _logger.LogWarning("Invalid exemption dates on update: StartDate={StartDate} > EndDate={EndDate}",
                    dto.StartDate.ToString("yyyy-MM-dd"), dto.EndDate.ToString("yyyy-MM-dd"));
                throw new ArgumentException("تاريخ البداية لا يمكن أن يكون بعد تاريخ النهاية");
            }

            // Check for overlapping exemptions (excluding current one)
            var existingExemptions = await _unitOfWork.PaymentExemptions
                .Query()
                .Where(e => e.StudentNationalId == exemption.StudentNationalId &&
                           e.Id != id &&
                           e.IsActive)
                .ToListAsync();

            var isOverlapping = existingExemptions.Any(e =>
                (dto.StartDate <= e.EndDate && dto.EndDate >= e.StartDate));

            if (isOverlapping)
            {
                _logger.LogWarning("Exemption date overlap on update: ExemptionId={ExemptionId}, ExistingExemptions={Count}",
                    id, existingExemptions.Count);
                throw new InvalidOperationException("تواريخ الإعفاء تتداخل مع إعفاء آخر للطالب");
            }

            exemption.StartDate = dto.StartDate.Date;
            exemption.EndDate = dto.EndDate.Date;
            exemption.Notes = dto.Notes;
            exemption.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.PaymentExemptions.Update(exemption);
            await _unitOfWork.SaveChangesAsync();

            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == exemption.StudentNationalId);

            _logger.LogInformation("Payment exemption updated: ExemptionId={ExemptionId}", id);

            return MapToPaymentExemptionDto(exemption, student);
        }

        public async Task<PaymentExemptionDto> GetPaymentExemptionByIdAsync(int id)
        {
            var exemption = await _unitOfWork.PaymentExemptions
                .Query()
                .Include(e => e.Student)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exemption == null)
            {
                _logger.LogWarning("Exemption not found: ExemptionId={ExemptionId}", id);
                throw new KeyNotFoundException($"الإعفاء بالرقم {id} غير موجود");
            }

            return MapToPaymentExemptionDto(exemption, exemption.Student);
        }

        public async Task<List<PaymentExemptionDto>> GetPaymentExemptionsByStudentAsync(string studentNationalId)
        {
            var nationalIdHash = HashString(studentNationalId);

            _logger.LogDebug("Getting payment exemptions for student: NationalIdHash={NationalIdHash}", nationalIdHash);

            var exemptions = await _unitOfWork.PaymentExemptions
                .Query()
                .Include(e => e.Student)
                .Where(e => e.StudentNationalId == studentNationalId)
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} exemptions for student: NationalIdHash={NationalIdHash}",
                exemptions.Count, nationalIdHash);

            return exemptions.Select(e => MapToPaymentExemptionDto(e, e.Student)).ToList();
        }

        public async Task<List<PaymentExemptionDto>> GetPaymentExemptionsAsync(PaymentExemptionFilterDto filter)
        {
            _logger.LogDebug("Getting payment exemptions with filter");

            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var query = _unitOfWork.PaymentExemptions
                .Query()
                .Include(e => e.Student)
                .Where(e => e.DormLocationId == dormLocationId);

            if (!string.IsNullOrEmpty(filter.StudentNationalId))
            {
                var studentHash = HashString(filter.StudentNationalId);
                query = query.Where(e => e.StudentNationalId.Contains(filter.StudentNationalId));
                _logger.LogDebug("Filtering exemptions by student national ID: NationalIdHash={NationalIdHash}", studentHash);
            }

            if (!string.IsNullOrEmpty(filter.StudentId))
            {
                query = query.Where(e => e.StudentId.Contains(filter.StudentId));
                _logger.LogDebug("Filtering exemptions by student ID: StudentId={StudentId}", filter.StudentId);
            }

            if (!string.IsNullOrEmpty(filter.StudentName))
            {
                query = query.Where(e =>
                    e.Student.FirstName.Contains(filter.StudentName) ||
                    e.Student.LastName.Contains(filter.StudentName));
                _logger.LogDebug("Filtering exemptions by student name: StudentName={StudentName}", filter.StudentName);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(e => e.IsActive == filter.IsActive.Value);
                _logger.LogDebug("Filtering exemptions by active status: IsActive={IsActive}", filter.IsActive.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(e => e.StartDate >= filter.FromDate.Value.Date);
                _logger.LogDebug("Filtering exemptions from date: FromDate={FromDate}", filter.FromDate.Value.ToString("yyyy-MM-dd"));
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(e => e.EndDate <= filter.ToDate.Value.Date);
                _logger.LogDebug("Filtering exemptions to date: ToDate={ToDate}", filter.ToDate.Value.ToString("yyyy-MM-dd"));
            }

            var exemptions = await query
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} exemptions with filter", exemptions.Count);

            return exemptions.Select(e => MapToPaymentExemptionDto(e, e.Student)).ToList();
        }

        public async Task TogglePaymentExemptionStatusAsync(int id, bool isActive)
        {
            _logger.LogInformation("Toggling exemption status: ExemptionId={ExemptionId}, IsActive={IsActive}", id, isActive);

            var exemption = await _unitOfWork.PaymentExemptions.GetByIdAsync(id);
            if (exemption == null)
            {
                _logger.LogWarning("Exemption not found for status toggle: ExemptionId={ExemptionId}", id);
                throw new KeyNotFoundException($"الإعفاء بالرقم {id} غير موجود");
            }

            exemption.IsActive = isActive;
            exemption.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.PaymentExemptions.Update(exemption);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Exemption status updated: ExemptionId={ExemptionId}, IsActive={IsActive}", id, isActive);
        }

        public async Task DeletePaymentExemptionAsync(int id)
        {
            _logger.LogInformation("Deleting payment exemption: ExemptionId={ExemptionId}", id);

            var exemption = await _unitOfWork.PaymentExemptions.GetByIdAsync(id);
            if (exemption == null)
            {
                _logger.LogWarning("Exemption not found for deletion: ExemptionId={ExemptionId}", id);
                throw new KeyNotFoundException($"الإعفاء بالرقم {id} غير موجود");
            }

            // Hard delete
            _unitOfWork.PaymentExemptions.Delete(exemption);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Payment exemption deleted: ExemptionId={ExemptionId}", id);
        }

        // Validation

        public async Task<bool> IsPaymentExemptionValidAsync(string studentNationalId, DateTime date)
        {
            var nationalIdHash = HashString(studentNationalId);

            var isValid = await _unitOfWork.PaymentExemptions
                .Query()
                .AnyAsync(e => e.StudentNationalId == studentNationalId &&
                             e.IsActive &&
                             e.StartDate.Date <= date.Date &&
                             e.EndDate.Date >= date.Date);

            _logger.LogDebug("Exemption validation: NationalIdHash={NationalIdHash}, Date={Date}, IsValid={IsValid}",
                nationalIdHash, date.ToString("yyyy-MM-dd"), isValid);

            return isValid;
        }

        // Helper Methods

        private void ValidatePaymentTypeFields(CreatePaymentTransactionDto dto)
        {
            switch (dto.PaymentType)
            {
                case PaymentType.MonthlyFee:
                    if (!dto.Month.HasValue || !dto.Year.HasValue)
                    {
                        _logger.LogWarning("Missing month/year for monthly fee payment");
                        throw new ArgumentException("الشهر والسنة مطلوبان لدفع الإيجار الشهري");
                    }
                    break;

                case PaymentType.MissedMealPenalty:
                    if (!dto.MissedMealsCount.HasValue || dto.MissedMealsCount.Value <= 0)
                    {
                        _logger.LogWarning("Invalid missed meals count for penalty payment");
                        throw new ArgumentException("عدد الوجبات الفائتة مطلوب لدفع غرامة الوجبات");
                    }
                    break;

                case PaymentType.Other:
                    // No specific validation for other payments
                    break;
            }
        }

        private async Task UpdateStudentPaymentStatus(Student student, decimal paymentAmount)
        {
            if (student.OutstandingAmount >= paymentAmount)
            {
                student.OutstandingAmount -= paymentAmount;
            }
            else
            {
                student.OutstandingAmount = 0;
            }

            student.HasOutstandingPayment = student.OutstandingAmount > 0;
            student.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Students.Update(student);
        }

        private PaymentTransactionDto MapToPaymentTransactionDto(PaymentTransaction payment, Student student)
        {
            return new PaymentTransactionDto
            {
                Id = payment.Id,
                DormLocationId = payment.DormLocationId,
                StudentNationalId = payment.StudentNationalId,
                StudentId = payment.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : null,
                Amount = payment.Amount,
                PaymentType = payment.PaymentType,
                PaymentTypeDisplay = GetPaymentTypeDisplay(payment.PaymentType),
                PaymentDate = payment.PaymentDate,
                ReceiptNumber = payment.ReceiptNumber,
                Month = payment.Month,
                Year = payment.Year,
                MissedMealsCount = payment.MissedMealsCount,
                ModifiedBy = payment.LastModifiedBy,
                CreatedAt = payment.CreatedAt
            };
        }

        private PaymentExemptionDto MapToPaymentExemptionDto(PaymentExemption exemption, Student student)
        {
            return new PaymentExemptionDto
            {
                Id = exemption.Id,
                DormLocationId = exemption.DormLocationId,
                StudentNationalId = exemption.StudentNationalId,
                StudentId = exemption.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : null,
                StartDate = exemption.StartDate,
                EndDate = exemption.EndDate,
                Notes = exemption.Notes,
                IsActive = exemption.IsActive,
                ModifiedBy = exemption.LastModifiedBy,
                ApprovedDate = exemption.ApprovedDate,
                CreatedAt = exemption.CreatedAt,
                UpdatedAt = exemption.UpdatedAt
            };
        }

        private string GetPaymentTypeDisplay(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.MonthlyFee => "الإيجار الشهري",
                PaymentType.MissedMealPenalty => "غرامة الوجبات الفائتة",
                PaymentType.Other => "أخرى",
                _ => paymentType.ToString()
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