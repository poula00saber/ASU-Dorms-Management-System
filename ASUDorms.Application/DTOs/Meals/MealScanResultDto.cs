using ASUDorms.Application.DTOs.Students;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Meals
{
    public class MealScanResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public StudentDto Student { get; set; }
    }
}
