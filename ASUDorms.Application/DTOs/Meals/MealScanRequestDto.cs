using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Meals
{
    public class MealScanRequestDto
    {
        [Required]
        public string StudentId { get; set; }

        [Required]
        public int MealTypeId { get; set; }
    }
}
