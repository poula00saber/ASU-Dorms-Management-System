using ASUDorms.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Payments
{
    public class CreatePaymentTransactionDto
    {
        [Required(ErrorMessage = "الرقم القومي للطالب مطلوب")]
        [StringLength(25, ErrorMessage = "الرقم القومي لا يمكن أن يتجاوز 25 حرف")]
        public string StudentNationalId { get; set; }

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "المبلغ يجب أن يكون أكبر من الصفر")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "نوع الدفع مطلوب")]
        public PaymentType PaymentType { get; set; }

        [Required(ErrorMessage = "تاريخ الدفع مطلوب")]
        public DateTime PaymentDate { get; set; }

        [StringLength(100, ErrorMessage = "رقم الإيصال لا يمكن أن يتجاوز 100 حرف")]
        public string ReceiptNumber { get; set; }

        // For monthly payments
        [Range(1, 12, ErrorMessage = "الشهر يجب أن يكون بين 1 و 12")]
        public int? Month { get; set; }

        [Range(2020, 2100, ErrorMessage = "السنة يجب أن تكون بين 2020 و 2100")]
        public int? Year { get; set; }

        // For missed meal payments
        [Range(1, int.MaxValue, ErrorMessage = "عدد الوجبات الفائتة يجب أن يكون أكبر من الصفر")]
        public int? MissedMealsCount { get; set; }

        [StringLength(1000, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 1000 حرف")]
        public string Notes { get; set; }
    }
}
