using System;

namespace ASUDorms.Application.DTOs.Holidays
{
    public class CreateHolidayDto
    {
        public string StudentNationalId { get; set; } // Use NationalId for creation
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }


}