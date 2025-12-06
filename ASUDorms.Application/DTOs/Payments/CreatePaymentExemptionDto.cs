using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Payments
{
    public class CreatePaymentExemptionDto
    {
        [Required(ErrorMessage = "الرقم القومي للطالب مطلوب")]
        [StringLength(25, ErrorMessage = "الرقم القومي لا يمكن أن يتجاوز 25 حرف")]
        public string StudentNationalId { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateTime EndDate { get; set; }

        [StringLength(1000, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 1000 حرف")]
        public string Notes { get; set; }

        [Required(ErrorMessage = "اسم الموظف المعتمد مطلوب")]
        [StringLength(100, ErrorMessage = "اسم الموظف لا يمكن أن يتجاوز 100 حرف")]
        public string ApprovedBy { get; set; }
    }
}
