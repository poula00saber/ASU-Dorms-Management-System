using System.ComponentModel.DataAnnotations;

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
        public string? Notes { get; set; }

        // Remove ApprovedBy from here - it will be set from current user in service
    }
}