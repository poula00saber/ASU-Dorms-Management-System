using System;

namespace ASUDorms.Application.DTOs.Payments
{
    public class PaymentExemptionDto
    {
        public int Id { get; set; }
        public int DormLocationId { get; set; }
        public string StudentNationalId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public string ModifiedBy { get; set; } // Changed from ApprovedBy
        public DateTime ApprovedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}