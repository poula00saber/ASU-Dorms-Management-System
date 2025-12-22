using System.ComponentModel.DataAnnotations;

namespace ASUDorms.Domain.Entities
{
    public abstract class AuditableEntity : BaseEntity
    {
        // Single field to store who made the last change
        [MaxLength(100)]
        public string LastModifiedBy { get; set; } = "System";
    }
}