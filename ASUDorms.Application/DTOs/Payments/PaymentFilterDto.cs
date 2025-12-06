using ASUDorms.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Payments
{
    public class PaymentFilterDto
    {
        public string StudentNationalId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public PaymentType? PaymentType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public string ReceiptNumber { get; set; }
    }
}
