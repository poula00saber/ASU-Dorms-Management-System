using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Enums;
using ASUDorms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                await SeedDormLocationsAsync();
                await SeedMealTypesAsync();
                await SeedUsersAsync();
                await SeedStudentsAsync();
                
                Console.WriteLine("✅ All seed data committed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Seeding failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private async Task SeedDormLocationsAsync()
        {
            if (await _context.DormLocations.AnyAsync())
            {
                Console.WriteLine("  ⏭️ DormLocations already seeded");
                return;
            }

            var locations = new[]
            {
                new DormLocation { Name = "مدينة طلبة العباسية", Address = "مدينة طلبة العباسية", IsActive = true, AllowCombinedMealScan = true },
                new DormLocation { Name = "مدينة طالبات مصر الجديدة", Address = "مدينة طالبات مصر الجديدة", IsActive = true, AllowCombinedMealScan = false },
                new DormLocation { Name = "مدينة نصر 1", Address = "مدينة نصر 1", IsActive = true, AllowCombinedMealScan = false },
                new DormLocation { Name = "مدينة نصر 2", Address = "مدينة نصر 2", IsActive = true, AllowCombinedMealScan = false },
                new DormLocation { Name = "زراعة أ", Address = "زراعة أ", IsActive = true, AllowCombinedMealScan = false },
                new DormLocation { Name = "زراعة ب", Address = "زراعة ب", IsActive = true, AllowCombinedMealScan = false },
                new DormLocation { Name = "الزيتون", Address = "الزيتون", IsActive = true, AllowCombinedMealScan = false }
            };

            await _context.DormLocations.AddRangeAsync(locations);
            await _context.SaveChangesAsync();
            
            Console.WriteLine("  ✅ DormLocations seeded (7 locations)");
        }

        private async Task SeedMealTypesAsync()
        {
            if (await _context.MealTypes.AnyAsync())
            {
                Console.WriteLine("  ⏭️ MealTypes already seeded");
                return;
            }

            var mealTypes = new[]
            {
                new MealType { Name = "BreakfastDinner", DisplayName = "إفطار وعشاء" },
                new MealType { Name = "Lunch", DisplayName = "غداء" }
            };

            await _context.MealTypes.AddRangeAsync(mealTypes);
            await _context.SaveChangesAsync();
            
            Console.WriteLine("  ✅ MealTypes seeded");
        }

        private async Task SeedUsersAsync()
        {
            if (await _context.Users.AnyAsync())
            {
                Console.WriteLine("  ⏭️ Users already seeded");
                return;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password");
            var locations = await _context.DormLocations.OrderBy(l => l.Id).ToListAsync();

            var users = new List<AppUser>();

            foreach (var loc in locations)
            {
                // Registration user for each location
                users.Add(new AppUser
                {
                    Username = $"reg_location{loc.Id}",
                    PasswordHash = passwordHash,
                    Role = UserRole.Registration,
                    DormLocationId = loc.Id,
                    AccessibleDormLocationIds = $"[{loc.Id}]",
                    IsActive = true
                });

                // Restaurant user for each location
                users.Add(new AppUser
                {
                    Username = $"rest_location{loc.Id}",
                    PasswordHash = passwordHash,
                    Role = UserRole.Restaurant,
                    DormLocationId = loc.Id,
                    AccessibleDormLocationIds = $"[{loc.Id}]",
                    IsActive = true
                });

                // Regular user for each location (holidays only)
                users.Add(new AppUser
                {
                    Username = $"user_location{loc.Id}",
                    PasswordHash = passwordHash,
                    Role = UserRole.User,
                    DormLocationId = loc.Id,
                    AccessibleDormLocationIds = $"[{loc.Id}]",
                    IsActive = true
                });
            }

            // Get the ID for location 2 (first female dorm)
            var femaleLocations = locations.Where(l => l.Id >= 2).Select(l => l.Id).ToList();
            if (femaleLocations.Any())
            {
                // Add a super-admin for female dorms that can access all locations 2-7
                users.Add(new AppUser
                {
                    Username = "admin_females",
                    PasswordHash = passwordHash,
                    Role = UserRole.Registration,
                    DormLocationId = femaleLocations.First(),
                    AccessibleDormLocationIds = $"[{string.Join(",", femaleLocations)}]",
                    IsActive = true
                });
            }

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();
            Console.WriteLine($"  ✅ Users seeded ({users.Count} users)");
        }

        private async Task SeedStudentsAsync()
        {
            if (await _context.Students.IgnoreQueryFilters().AnyAsync())
            {
                Console.WriteLine("  ⏭️ Students already seeded");
                return;
            }

            var locations = await _context.DormLocations.OrderBy(l => l.Id).ToListAsync();
            if (!locations.Any())
            {
                Console.WriteLine("  ⚠️ No locations found, skipping student seeding");
                return;
            }

            var students = new List<Student>();
            var random = new Random(42); // Fixed seed for reproducible data
            var usedNationalIds = new HashSet<string>();

            // Egyptian first names (male)
            var maleFirstNames = new[] { "أحمد", "محمد", "يوسف", "عمر", "علي", "خالد", "كريم", "مصطفى", "إبراهيم", "حسن", "طارق", "سامي", "رامي", "وليد", "هشام" };
            // Egyptian first names (female)
            var femaleFirstNames = new[] { "فاطمة", "نور", "سارة", "مريم", "ياسمين", "هدى", "رنا", "دينا", "منى", "سلمى", "آية", "ريم", "جنى", "لينا", "هبة" };
            // Egyptian last names
            var lastNames = new[] { "محمود", "عبدالله", "السيد", "حسين", "إبراهيم", "أحمد", "علي", "حسن", "خليل", "عمر", "سالم", "فؤاد", "رشدي", "نصر", "فهمي" };
            // Egyptian governorates
            var governorates = new[] { "القاهرة", "الجيزة", "الإسكندرية", "الدقهلية", "الشرقية", "المنوفية", "الغربية", "البحيرة", "أسيوط", "سوهاج", "المنيا", "الفيوم", "بني سويف", "قنا", "الأقصر" };
            // Districts
            var districts = new[] { "المركز", "مدينة", "قرية النجار", "العزبة", "البلد", "الحي الأول", "الحي الثاني" };
            // Faculties
            var faculties = new[] { "كلية الهندسة", "كلية الطب", "كلية العلوم", "كلية التجارة", "كلية الحقوق", "كلية الآداب", "كلية الصيدلة", "كلية طب الأسنان", "كلية الحاسبات والمعلومات", "كلية الزراعة" };
            // Grades
            var grades = new[] { "ممتاز", "جيد جداً", "جيد", "مقبول" };
            // Secondary schools
            var secondarySchools = new[] { "مدرسة الثانوية العامة", "مدرسة STEM", "مدرسة الأزهر الثانوية", "مدرسة المتفوقين", "مدرسة اللغات التجريبية" };

            int studentCounter = 1;

            // First location (index 0) is male dorm - 50 students
            var maleLocation = locations.FirstOrDefault();
            if (maleLocation != null)
            {
                for (int i = 0; i < 50; i++)
                {
                    var nationalId = GenerateUniqueNationalId(random, true, usedNationalIds);
                    var firstName = maleFirstNames[random.Next(maleFirstNames.Length)];
                    var lastName = lastNames[random.Next(lastNames.Length)];
                    var status = random.Next(100) < 30 ? StudentStatus.NewStudent : StudentStatus.OldStudent;

                    students.Add(CreateStudent(
                        nationalId: nationalId,
                        studentId: $"2024{studentCounter++:D4}",
                        firstName: firstName,
                        lastName: lastName,
                        dormLocationId: maleLocation.Id,
                        status: status,
                        governorate: governorates[random.Next(governorates.Length)],
                        district: districts[random.Next(districts.Length)],
                        faculty: faculties[random.Next(faculties.Length)],
                        level: random.Next(1, 5),
                        grade: grades[random.Next(grades.Length)],
                        percentageGrade: random.Next(60, 100),
                        dormType: (DormType)random.Next(1, 4),
                        buildingNumber: $"A{random.Next(1, 6)}",
                        roomNumber: $"{random.Next(100, 500)}",
                        religion: random.Next(100) < 85 ? Religion.Muslim : Religion.Christian,
                        secondarySchool: status == StudentStatus.NewStudent ? secondarySchools[random.Next(secondarySchools.Length)] : null,
                        highSchoolPercentage: status == StudentStatus.NewStudent ? random.Next(70, 100) : null,
                        random: random,
                        usedNationalIds: usedNationalIds
                    ));
                }
            }

            // Remaining locations are female dorms - 40 students each
            var femaleLocations = locations.Skip(1).ToList();
            foreach (var location in femaleLocations)
            {
                for (int i = 0; i < 40; i++)
                {
                    var nationalId = GenerateUniqueNationalId(random, false, usedNationalIds);
                    var firstName = femaleFirstNames[random.Next(femaleFirstNames.Length)];
                    var lastName = lastNames[random.Next(lastNames.Length)];
                    var status = random.Next(100) < 30 ? StudentStatus.NewStudent : StudentStatus.OldStudent;

                    students.Add(CreateStudent(
                        nationalId: nationalId,
                        studentId: $"2024{studentCounter++:D4}",
                        firstName: firstName,
                        lastName: lastName,
                        dormLocationId: location.Id,
                        status: status,
                        governorate: governorates[random.Next(governorates.Length)],
                        district: districts[random.Next(districts.Length)],
                        faculty: faculties[random.Next(faculties.Length)],
                        level: random.Next(1, 5),
                        grade: grades[random.Next(grades.Length)],
                        percentageGrade: random.Next(60, 100),
                        dormType: (DormType)random.Next(1, 4),
                        buildingNumber: $"B{random.Next(1, 6)}",
                        roomNumber: $"{random.Next(100, 500)}",
                        religion: random.Next(100) < 85 ? Religion.Muslim : Religion.Christian,
                        secondarySchool: status == StudentStatus.NewStudent ? secondarySchools[random.Next(secondarySchools.Length)] : null,
                        highSchoolPercentage: status == StudentStatus.NewStudent ? random.Next(70, 100) : null,
                        random: random,
                        usedNationalIds: usedNationalIds
                    ));
                }
            }

            // Add students in batches to avoid memory issues
            const int batchSize = 50;
            for (int i = 0; i < students.Count; i += batchSize)
            {
                var batch = students.Skip(i).Take(batchSize).ToList();
                await _context.Students.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
                Console.WriteLine($"    Seeded batch {i / batchSize + 1}/{(students.Count + batchSize - 1) / batchSize}");
            }

            Console.WriteLine($"  ✅ Students seeded ({students.Count} students)");
        }

        private Student CreateStudent(
            string nationalId, string studentId, string firstName, string lastName,
            int dormLocationId, StudentStatus status, string governorate, string district,
            string faculty, int level, string grade, decimal percentageGrade,
            DormType dormType, string buildingNumber, string roomNumber,
            Religion religion, string? secondarySchool, decimal? highSchoolPercentage,
            Random random, HashSet<string> usedNationalIds)
        {
            var fatherFirstName = new[] { "محمد", "أحمد", "علي", "حسن", "إبراهيم", "خالد", "عمر" }[random.Next(7)];
            var fatherNationalId = GenerateUniqueNationalId(random, true, usedNationalIds);
            
            return new Student
            {
                NationalId = nationalId,
                StudentId = studentId,
                DormLocationId = dormLocationId,
                IsEgyptian = true,
                FirstName = firstName,
                LastName = lastName,
                Status = status,
                Email = $"{studentId}@student.asu.edu.eg",
                PhoneNumber = $"01{random.Next(0, 3)}{random.Next(10000000, 99999999)}",
                Religion = religion,
                PhotoUrl = null,
                Government = governorate,
                District = district,
                StreetName = $"شارع {random.Next(1, 100)}",
                Faculty = faculty,
                Level = level,
                Grade = grade,
                PercentageGrade = percentageGrade,
                SecondarySchoolName = secondarySchool,
                SecondarySchoolGovernment = secondarySchool != null ? governorate : null,
                HighSchoolPercentage = highSchoolPercentage,
                DormType = dormType,
                BuildingNumber = buildingNumber,
                RoomNumber = roomNumber,
                HasSpecialNeeds = random.Next(100) < 5,
                SpecialNeedsDetails = null,
                IsExemptFromFees = random.Next(100) < 10,
                MissedMealsCount = random.Next(0, 10),
                HasOutstandingPayment = random.Next(100) < 15,
                OutstandingAmount = random.Next(100) < 15 ? random.Next(100, 1000) : 0,
                FatherName = $"{fatherFirstName} {lastName}",
                FatherNationalId = fatherNationalId,
                FatherProfession = new[] { "موظف حكومي", "مهندس", "طبيب", "تاجر", "مدرس", "محاسب", "محامي" }[random.Next(7)],
                FatherPhone = $"01{random.Next(0, 3)}{random.Next(10000000, 99999999)}",
                GuardianName = $"{fatherFirstName} {lastName}",
                GuardianRelationship = "الأب",
                GuardianPhone = $"01{random.Next(0, 3)}{random.Next(10000000, 99999999)}",
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                IsDeleted = false
            };
        }

        private string GenerateUniqueNationalId(Random random, bool isMale, HashSet<string> usedIds)
        {
            string nationalId;
            int attempts = 0;
            do
            {
                nationalId = GenerateNationalId(random, isMale);
                attempts++;
                if (attempts > 1000)
                {
                    throw new InvalidOperationException("Could not generate unique National ID");
                }
            } while (usedIds.Contains(nationalId));
            
            usedIds.Add(nationalId);
            return nationalId;
        }

        private string GenerateNationalId(Random random, bool isMale)
        {
            // Egyptian National ID format: CYYMMDDSSSSG
            int century = random.Next(100) < 70 ? 2 : 3;
            int year = century == 2 ? random.Next(98, 100) : random.Next(0, 6);
            int month = random.Next(1, 13);
            int day = random.Next(1, 29);
            int serial = random.Next(1000, 9999);
            
            // Make serial odd for male, even for female
            if (isMale && serial % 2 == 0) serial++;
            if (!isMale && serial % 2 == 1) serial++;
            
            int checkDigit = random.Next(1, 10);

            return $"{century}{year:D2}{month:D2}{day:D2}{serial:D4}{checkDigit}";
        }
    }
}
