using ASUDorms.Application.DTOs.Common;
using ASUDorms.Application.DTOs.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.Interfaces
{
    public interface IPaymentService
    {
        // Payment Transactions
        Task<PaymentTransactionDto> CreatePaymentTransactionAsync(CreatePaymentTransactionDto dto);
        Task<PaymentTransactionDto> GetPaymentTransactionByIdAsync(int id);
        Task<List<PaymentTransactionDto>> GetPaymentTransactionsByStudentAsync(string studentNationalId);
        Task<List<PaymentTransactionDto>> GetPaymentTransactionsAsync(PaymentFilterDto filter);
        Task DeletePaymentTransactionAsync(int id);

        // Payment Summary
        Task<PaymentSummaryDto> GetPaymentSummaryByStudentAsync(string studentNationalId);
        Task<decimal> GetTotalPaymentsByStudentAsync(string studentNationalId);

        // Payment Exemptions
        Task<PaymentExemptionDto> CreatePaymentExemptionAsync(CreatePaymentExemptionDto dto);
        Task<PaymentExemptionDto> UpdatePaymentExemptionAsync(int id, UpdatePaymentExemptionDto dto);
        Task<PaymentExemptionDto> GetPaymentExemptionByIdAsync(int id);
        Task<List<PaymentExemptionDto>> GetPaymentExemptionsByStudentAsync(string studentNationalId);
        Task<List<PaymentExemptionDto>> GetPaymentExemptionsAsync(PaymentExemptionFilterDto filter);
        Task TogglePaymentExemptionStatusAsync(int id, bool isActive);
        Task DeletePaymentExemptionAsync(int id);

        // Validation
        Task<bool> IsPaymentExemptionValidAsync(string studentNationalId, DateTime date);

        // PAGINATION METHODS:
        Task<PagedResult<PaymentTransactionDto>> GetPaymentTransactionsPagedAsync(int pageNumber, int pageSize, string? search = null, string? filterStudentId = null);
        Task<PagedResult<PaymentTransactionDto>> GetStudentPaymentTransactionsPagedAsync(string studentId, int pageNumber, int pageSize);

        // BULK MONTHLY FEES:
        Task<List<DormTypeAvailableDto>> GetAvailableDormTypesForBulkFeesAsync();
        Task<BulkFeesResultDto> BulkAddMonthlyFeesAsync(BulkMonthlyFeesDto dto);
    }
}
