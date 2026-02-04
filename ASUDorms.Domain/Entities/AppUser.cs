// ASUDorms.Domain/Entities/AppUser.cs
using ASUDorms.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;

namespace ASUDorms.Domain.Entities
{
    public class AppUser : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [Required]
        [ForeignKey(nameof(DormLocation))]
        public int DormLocationId { get; set; } // Primary/Default location

        // NEW: JSON array string like "[2,3,4,5,6,7]"
        [MaxLength(500)]
        public string AccessibleDormLocationIds { get; set; } = "[]";

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual DormLocation DormLocation { get; set; }

       
        public List<int> GetAccessibleLocations()
        {
            if (string.IsNullOrEmpty(AccessibleDormLocationIds))
                return new List<int> { DormLocationId };

            try
            {
                return JsonSerializer.Deserialize<List<int>>(AccessibleDormLocationIds) ?? new List<int> { DormLocationId };
            }
            catch
            {
                return new List<int> { DormLocationId };
            }
        }

        // Helper method to check if user can access a specific dorm
        public bool CanAccessDormLocation(int dormLocationId)
        {
            return GetAccessibleLocations().Contains(dormLocationId);
        }
    }
}
