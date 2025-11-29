using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Enums;
using ASUDorms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;

        public DatabaseSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Seed Dorm Locations if none exist
            if (!await _context.DormLocations.AnyAsync())
            {
                var locations = new[]
                {
                    new DormLocation { Name = "Dorm Location 1", Address = "Cairo - Nasr City", IsActive = true },
                    new DormLocation { Name = "Dorm Location 2", Address = "Giza - Dokki", IsActive = true },
                    new DormLocation { Name = "Dorm Location 3", Address = "Alexandria - Smouha", IsActive = true },
                    new DormLocation { Name = "Dorm Location 4", Address = "Cairo - Heliopolis", IsActive = true },
                    new DormLocation { Name = "Dorm Location 5", Address = "Giza - Mohandessin", IsActive = true },
                    new DormLocation { Name = "Dorm Location 6", Address = "Cairo - Maadi", IsActive = true },
                    new DormLocation { Name = "Dorm Location 7", Address = "Cairo - New Cairo", IsActive = true }
                };

                await _context.DormLocations.AddRangeAsync(locations);
                await _context.SaveChangesAsync();
            }

            // Seed Users if none exist (Password: Password123)
            if (!await _context.Users.AnyAsync())
            {
                var locations = await _context.DormLocations.ToListAsync();
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123");

                var users = locations.SelectMany(loc => new[]
                {
                    new AppUser
                    {
                        Username = $"reg_location{loc.Id}",
                        PasswordHash = passwordHash,
                        Role = UserRole.Registration,
                        DormLocationId = loc.Id,
                        IsActive = true
                    },
                    new AppUser
                    {
                        Username = $"rest_location{loc.Id}",
                        PasswordHash = passwordHash,
                        Role = UserRole.Restaurant,
                        DormLocationId = loc.Id,
                        IsActive = true
                    }
                }).ToList();

                await _context.Users.AddRangeAsync(users);
                await _context.SaveChangesAsync();
            }
        }
    }
}
