// Add this property to your DormLocation entity class

using System.ComponentModel.DataAnnotations;

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

        // NEW PROPERTY - Add this
        public bool AllowCombinedMealScan { get; set; } = false;

        // Navigation Properties
        public virtual ICollection<AppUser> Users { get; set; }
        public virtual ICollection<Student> Students { get; set; }
        public virtual ICollection<MealTransaction> MealTransactions { get; set; }
    }
}

// After adding this property, you'll need to create a migration:
// Add-Migration AddAllowCombinedMealScanToDormLocation
// Update-Database