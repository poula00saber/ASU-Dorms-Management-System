using ASUDorms.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<PaymentExemption> PaymentExemptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // ✅ KEEP soft delete filter
            modelBuilder.Entity<Student>().HasQueryFilter(s => !s.IsDeleted);
            modelBuilder.Entity<MealTransaction>().HasQueryFilter(m => !m.IsDeleted);


            ///problem of students not visible  where always ask from query but we need from header
            //// Global query filter for multi-tenancy
            //modelBuilder.Entity<Student>().HasQueryFilter(s =>
            //    !s.IsDeleted &&
            //    s.DormLocationId == GetCurrentDormLocationId());

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
                .IsUnique()
                .HasDatabaseName("IX_Students_StudentId_DormLocationId");

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.NationalId)
                .IsUnique()
                .HasDatabaseName("IX_Students_NationalId");

            // Critical for scanner!
            modelBuilder.Entity<MealTransaction>()
                .HasIndex(m => new { m.StudentNationalId, m.Date, m.MealTypeId })
                .HasDatabaseName("IX_MealTransactions_Student_Date_MealType");

            modelBuilder.Entity<MealTransaction>()
                .HasIndex(m => m.DormLocationId)
                .HasDatabaseName("IX_MealTransactions_DormLocationId");

            modelBuilder.Entity<MealTransaction>()
                .HasIndex(m => m.StudentNationalId)
                .HasDatabaseName("IX_MealTransactions_StudentNationalId");

            modelBuilder.Entity<MealTransaction>()
                .HasIndex(m => m.Date)
                .HasDatabaseName("IX_MealTransactions_Date");

            modelBuilder.Entity<Holiday>()
                .HasKey(h => h.Id);

            modelBuilder.Entity<Holiday>()
                .HasIndex(h => h.StudentNationalId)
                .HasDatabaseName("IX_Holidays_StudentNationalId");

            modelBuilder.Entity<Holiday>()
                .HasIndex(h => new { h.StartDate, h.EndDate })
                .HasDatabaseName("IX_Holidays_Dates");
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
            var currentUsername = GetCurrentUsername();
            UpdateAuditFields(currentUsername);
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields(string currentUsername)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is AuditableEntity &&
                    (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (AuditableEntity)entry.Entity;

                // Always update LastModifiedBy for both Add and Update
                entity.LastModifiedBy = currentUsername;

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

        private string GetCurrentUsername()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null) return "System";

            return httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                   ?? httpContext.User.FindFirst("name")?.Value
                   ?? httpContext.User.Identity?.Name
                   ?? "Unknown";
        }
    }
}