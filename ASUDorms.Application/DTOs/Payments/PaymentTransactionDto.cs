using ASUDorms.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Payments
{
    public class PaymentTransactionDto
    {
        public int Id { get; set; }
        public int DormLocationId { get; set; }
        public string StudentNationalId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public decimal Amount { get; set; }
        public PaymentType PaymentType { get; set; }
        public string PaymentTypeDisplay { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReceiptNumber { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? MissedMealsCount { get; set; }
        public string Notes { get; set; }
        public string ProcessedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
