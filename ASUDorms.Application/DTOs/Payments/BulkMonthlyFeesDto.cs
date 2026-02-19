using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ASUDorms.Application.DTOs.Payments
{
    public class BulkMonthlyFeesDto
    {
        /// <summary>
        /// Dictionary of DormType (1=Regular, 2=Premium, 3=Hotel) to amount
        /// </summary>
        [Required]
        public Dictionary<int, decimal> DormTypeAmounts { get; set; } = new();

        /// <summary>
        /// Month and year for which fees are being added (e.g., "2025-01")
        /// </summary>
        [Required]
        public string Month { get; set; }

        /// <summary>
        /// Optional description (e.g., "January 2025 Monthly Fee")
        /// </summary>
        public string? Description { get; set; }
    }

    public class BulkFeesResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalStudentsProcessed { get; set; }
        public Dictionary<int, int> ProcessedByDormType { get; set; } = new();
        public decimal TotalAmountAdded { get; set; }
    }

    public class DormTypeAvailableDto
    {
        public int DormTypeId { get; set; }
        public string DormTypeName { get; set; }
        public int StudentCount { get; set; }
        public int NonExemptCount { get; set; }
    }
}
