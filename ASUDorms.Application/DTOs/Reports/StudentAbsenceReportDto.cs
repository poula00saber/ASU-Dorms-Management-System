using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Reports
{
    public class StudentAbsenceReportDto
    {
        public int DormLocationId { get; set; }
        public string StudentNationalId { get; set; }
        public string StudentName { get; set; }
        public string Faculty { get; set; }
        public int Level { get; set; }
        public string BuildingNumber { get; set; }
        public string RoomNumber { get; set; }
        public int MissedDaysCount { get; set; }
        public decimal TotalPenaltyCost { get; set; }
        public List<DateTime> MissedDates { get; set; }
        public bool HasOutstandingPayment { get; set; }
        public decimal OutstandingAmount { get; set; }
    }
}
