using ASUDorms.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<DormLocation> DormLocations { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<MealType> MealTypes { get; set; }
        public DbSet<MealTransaction> MealTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Global query filter for multi-tenancy
            modelBuilder.Entity<Student>().HasQueryFilter(s =>
                !s.IsDeleted &&
                s.DormLocationId == GetCurrentDormLocationId());

            modelBuilder.Entity<MealTransaction>().HasQueryFilter(m =>
                !m.IsDeleted &&
                m.DormLocationId == GetCurrentDormLocationId());

            // Seed MealTypes
            modelBuilder.Entity<MealType>().HasData(
                new MealType { Id = 1, Name = "BreakfastDinner", DisplayName = "Breakfast & Dinner" },
                new MealType { Id = 2, Name = "Lunch", DisplayName = "Lunch" }
            );

            // Indexes for performance
            modelBuilder.Entity<Student>()
                .HasIndex(s => new { s.StudentId, s.DormLocationId })
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.NationalId);

            modelBuilder.Entity<MealTransaction>()
                .HasIndex(m => new { m.StudentId, m.Date, m.MealTypeId });
        }

        private int GetCurrentDormLocationId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null) return 0;

            var locationClaim = httpContext.User.FindFirst("DormLocationId");
            if (locationClaim != null && int.TryParse(locationClaim.Value, out int locationId))
            {
                return locationId;
            }

            return 0;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                    (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
