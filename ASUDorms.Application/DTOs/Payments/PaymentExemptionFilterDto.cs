using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Payments
{
    public class PaymentExemptionFilterDto
    {
        public string StudentNationalId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
