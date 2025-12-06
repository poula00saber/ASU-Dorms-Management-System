using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Payments
{
    public class PaymentSummaryDto
    {
        public string StudentNationalId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal OutstandingAmount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public List<PaymentTransactionDto> RecentTransactions { get; set; }
    }
}
