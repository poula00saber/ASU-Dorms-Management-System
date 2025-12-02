using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Meals
{
    public class StudentScanDto
    {
        public string StudentId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BuildingNumber { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime? timeScanned { get; set; }

    }
}
