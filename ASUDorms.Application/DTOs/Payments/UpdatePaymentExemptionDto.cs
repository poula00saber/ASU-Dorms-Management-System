using System.ComponentModel.DataAnnotations;

namespace ASUDorms.Application.DTOs.Payments
{
    public class UpdatePaymentExemptionDto
    {
        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateTime EndDate { get; set; }

        [StringLength(1000, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 1000 حرف")]
        public string Notes { get; set; }

        // Remove ApprovedBy from here - it will be set from current user in service
    }
}