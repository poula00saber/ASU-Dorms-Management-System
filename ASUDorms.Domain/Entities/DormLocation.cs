using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Domain.Entities
{
    public class DormLocation : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Address { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<AppUser> Users { get; set; }
        public virtual ICollection<Student> Students { get; set; }
        public virtual ICollection<MealTransaction> MealTransactions { get; set; }
    }
}
