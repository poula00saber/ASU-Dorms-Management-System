using System;

namespace ASUDorms.Application.DTOs.Holidays
{
    public class HolidayDto
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public string StudentNationalId { get; set; }
        public string StudentName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Single field to show who made the last change
        public string ModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}