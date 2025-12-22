using ASUDorms.Application.DTOs.Payments;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Enums;
using ASUDorms.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public PaymentService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        // Payment Transactions

        public async Task<PaymentTransactionDto> CreatePaymentTransactionAsync(CreatePaymentTransactionDto dto)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

            // Find student
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == dto.StudentNationalId && !s.IsDeleted);

            if (student == null)
            {
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
                // ProcessedBy REMOVED - LastModifiedBy will be set automatically by DbContext
            };

            // Update student's outstanding payment status
            await UpdateStudentPaymentStatus(student, dto.Amount);

            await _unitOfWork.PaymentTransactions.AddAsync(paymentTransaction);
            await _unitOfWork.SaveChangesAsync();

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
                throw new KeyNotFoundException($"عملية الدفع بالرقم {id} غير موجودة");
            }

            return MapToPaymentTransactionDto(payment, payment.Student);
        }

        public async Task<List<PaymentTransactionDto>> GetPaymentTransactionsByStudentAsync(string studentNationalId)
        {
            var payments = await _unitOfWork.PaymentTransactions
                .Query()
                .Include(p => p.Student)
                .Where(p => p.StudentNationalId == studentNationalId && !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(p => MapToPaymentTransactionDto(p, p.Student)).ToList();
        }

        public async Task<List<PaymentTransactionDto>> GetPaymentTransactionsAsync(PaymentFilterDto filter)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var query = _unitOfWork.PaymentTransactions
                .Query()
                .Include(p => p.Student)
                .Where(p => p.DormLocationId == dormLocationId && !p.IsDeleted);

            if (!string.IsNullOrEmpty(filter.StudentNationalId))
            {
                query = query.Where(p => p.StudentNationalId.Contains(filter.StudentNationalId));
            }

            if (!string.IsNullOrEmpty(filter.StudentId))
            {
                query = query.Where(p => p.StudentId.Contains(filter.StudentId));
            }

            if (!string.IsNullOrEmpty(filter.StudentName))
            {
                query = query.Where(p =>
                    p.Student.FirstName.Contains(filter.StudentName) ||
                    p.Student.LastName.Contains(filter.StudentName));
            }

            if (filter.PaymentType.HasValue)
            {
                query = query.Where(p => p.PaymentType == filter.PaymentType.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= filter.FromDate.Value.Date);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate <= filter.ToDate.Value.Date);
            }

            if (filter.Month.HasValue)
            {
                query = query.Where(p => p.Month == filter.Month.Value);
            }

            if (filter.Year.HasValue)
            {
                query = query.Where(p => p.Year == filter.Year.Value);
            }

            if (!string.IsNullOrEmpty(filter.ReceiptNumber))
            {
                query = query.Where(p => p.ReceiptNumber.Contains(filter.ReceiptNumber));
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(p => MapToPaymentTransactionDto(p, p.Student)).ToList();
        }

        public async Task DeletePaymentTransactionAsync(int id)
        {
            var payment = await _unitOfWork.PaymentTransactions.GetByIdAsync(id);
            if (payment == null)
            {
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
            }

            // Soft delete
            payment.IsDeleted = true;
            _unitOfWork.PaymentTransactions.Update(payment);
            await _unitOfWork.SaveChangesAsync();
        }

        // Payment Summary

        public async Task<PaymentSummaryDto> GetPaymentSummaryByStudentAsync(string studentNationalId)
        {
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == studentNationalId && !s.IsDeleted);

            if (student == null)
            {
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
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

            // Find student
            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == dto.StudentNationalId && !s.IsDeleted);

            if (student == null)
            {
                throw new KeyNotFoundException($"الطالب بالرقم القومي '{dto.StudentNationalId}' غير موجود");
            }

            // Validate dates
            if (dto.StartDate > dto.EndDate)
            {
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
                // ApprovedBy REMOVED - LastModifiedBy will be set automatically by DbContext
                ApprovedDate = DateTime.UtcNow
            };

            await _unitOfWork.PaymentExemptions.AddAsync(paymentExemption);
            await _unitOfWork.SaveChangesAsync();

            return MapToPaymentExemptionDto(paymentExemption, student);
        }

        public async Task<PaymentExemptionDto> UpdatePaymentExemptionAsync(int id, UpdatePaymentExemptionDto dto)
        {
            var exemption = await _unitOfWork.PaymentExemptions.GetByIdAsync(id);
            if (exemption == null)
            {
                throw new KeyNotFoundException($"الإعفاء بالرقم {id} غير موجود");
            }

            // Validate dates
            if (dto.StartDate > dto.EndDate)
            {
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
                throw new InvalidOperationException("تواريخ الإعفاء تتداخل مع إعفاء آخر للطالب");
            }

            exemption.StartDate = dto.StartDate.Date;
            exemption.EndDate = dto.EndDate.Date;
            exemption.Notes = dto.Notes;
            // ApprovedBy REMOVED - LastModifiedBy will be updated automatically by DbContext
            exemption.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.PaymentExemptions.Update(exemption);
            await _unitOfWork.SaveChangesAsync();

            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.NationalId == exemption.StudentNationalId);

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
                throw new KeyNotFoundException($"الإعفاء بالرقم {id} غير موجود");
            }

            return MapToPaymentExemptionDto(exemption, exemption.Student);
        }

        public async Task<List<PaymentExemptionDto>> GetPaymentExemptionsByStudentAsync(string studentNationalId)
        {
            var exemptions = await _unitOfWork.PaymentExemptions
                .Query()
                .Include(e => e.Student)
                .Where(e => e.StudentNationalId == studentNationalId)
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            return exemptions.Select(e => MapToPaymentExemptionDto(e, e.Student)).ToList();
        }

        public async Task<List<PaymentExemptionDto>> GetPaymentExemptionsAsync(PaymentExemptionFilterDto filter)
        {
            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();
            var query = _unitOfWork.PaymentExemptions
                .Query()
                .Include(e => e.Student)
                .Where(e => e.DormLocationId == dormLocationId);

            if (!string.IsNullOrEmpty(filter.StudentNationalId))
            {
                query = query.Where(e => e.StudentNationalId.Contains(filter.StudentNationalId));
            }

            if (!string.IsNullOrEmpty(filter.StudentId))
            {
                query = query.Where(e => e.StudentId.Contains(filter.StudentId));
            }

            if (!string.IsNullOrEmpty(filter.StudentName))
            {
                query = query.Where(e =>
                    e.Student.FirstName.Contains(filter.StudentName) ||
                    e.Student.LastName.Contains(filter.StudentName));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(e => e.IsActive == filter.IsActive.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(e => e.StartDate >= filter.FromDate.Value.Date);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(e => e.EndDate <= filter.ToDate.Value.Date);
            }

            var exemptions = await query
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            return exemptions.Select(e => MapToPaymentExemptionDto(e, e.Student)).ToList();
        }

        public async Task TogglePaymentExemptionStatusAsync(int id, bool isActive)
        {
            var exemption = await _unitOfWork.PaymentExemptions.GetByIdAsync(id);
            if (exemption == null)
            {
                throw new KeyNotFoundException($"الإعفاء بالرقم {id} غير موجود");
            }

            exemption.IsActive = isActive;
            exemption.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.PaymentExemptions.Update(exemption);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeletePaymentExemptionAsync(int id)
        {
            var exemption = await _unitOfWork.PaymentExemptions.GetByIdAsync(id);
            if (exemption == null)
            {
                throw new KeyNotFoundException($"الإعفاء بالرقم {id} غير موجود");
            }

            // Hard delete
            _unitOfWork.PaymentExemptions.Delete(exemption);
            await _unitOfWork.SaveChangesAsync();
        }

        // Validation

        public async Task<bool> IsPaymentExemptionValidAsync(string studentNationalId, DateTime date)
        {
            return await _unitOfWork.PaymentExemptions
                .Query()
                .AnyAsync(e => e.StudentNationalId == studentNationalId &&
                             e.IsActive &&
                             e.StartDate.Date <= date.Date &&
                             e.EndDate.Date >= date.Date);
        }

        // Helper Methods

        private void ValidatePaymentTypeFields(CreatePaymentTransactionDto dto)
        {
            switch (dto.PaymentType)
            {
                case PaymentType.MonthlyFee:
                    if (!dto.Month.HasValue || !dto.Year.HasValue)
                    {
                        throw new ArgumentException("الشهر والسنة مطلوبان لدفع الإيجار الشهري");
                    }
                    break;

                case PaymentType.MissedMealPenalty:
                    if (!dto.MissedMealsCount.HasValue || dto.MissedMealsCount.Value <= 0)
                    {
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
                ModifiedBy = payment.LastModifiedBy, // Only this field
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
                ModifiedBy = exemption.LastModifiedBy, // Only this field
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
    }
}