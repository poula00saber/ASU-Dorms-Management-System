using ASUDorms.Application.DTOs.Students;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Enums;
using ASUDorms.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class StudentService : IStudentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly ILogger<StudentService> _logger;

        public StudentService(IUnitOfWork unitOfWork, IAuthService authService, ILogger<StudentService> logger)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _logger = logger;
        }

        public async Task<StudentDto> CreateStudentAsync(CreateStudentDto dto)
        {
            var nationalIdHash = HashString(dto.NationalId);

            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during student creation");
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            _logger.LogInformation("Creating student: StudentId={StudentId}, NationalIdHash={NationalIdHash}, DormLocationId={DormLocationId}",
                dto.StudentId, nationalIdHash, dormLocationId);

            // Validate secondary school info for new students
            if (dto.Status == StudentStatus.NewStudent)
            {
                if (string.IsNullOrWhiteSpace(dto.SecondarySchoolName))
                {
                    _logger.LogWarning("Missing secondary school name for new student: StudentId={StudentId}", dto.StudentId);
                    throw new InvalidOperationException("Secondary school name is required for new students");
                }
                if (string.IsNullOrWhiteSpace(dto.SecondarySchoolGovernment))
                {
                    _logger.LogWarning("Missing secondary school government for new student: StudentId={StudentId}", dto.StudentId);
                    throw new InvalidOperationException("Secondary school government is required for new students");
                }
                if (!dto.HighSchoolPercentage.HasValue)
                {
                    _logger.LogWarning("Missing high school percentage for new student: StudentId={StudentId}", dto.StudentId);
                    throw new InvalidOperationException("High school percentage is required for new students");
                }
            }

            // Check if StudentId already exists in this location
            var existingStudent = await _unitOfWork.Students
                .FindAsync(s => s.StudentId == dto.StudentId && s.DormLocationId == dormLocationId);

            if (existingStudent.Any())
            {
                _logger.LogWarning("Student ID already exists: StudentId={StudentId}, DormLocationId={DormLocationId}",
                    dto.StudentId, dormLocationId);
                throw new InvalidOperationException("Student ID already exists in this location");
            }

            // Check if NationalId already exists globally
            var existingNationalId = await _unitOfWork.Students
                .FindAsync(s => s.NationalId == dto.NationalId);

            if (existingNationalId.Any())
            {
                _logger.LogWarning("National ID already exists: NationalIdHash={NationalIdHash}", nationalIdHash);
                throw new InvalidOperationException("National ID already exists");
            }

            var student = new Student
            {
                DormLocationId = dormLocationId,
                StudentId = dto.StudentId,
                NationalId = dto.NationalId,
                IsEgyptian = dto.IsEgyptian,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Status = dto.Status,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Religion = dto.Religion,
                Government = dto.Government,
                District = dto.District,
                StreetName = dto.StreetName,
                Faculty = dto.Faculty,
                Level = dto.Level,
                Grade = dto.Grade,
                PercentageGrade = dto.PercentageGrade,
                SecondarySchoolName = dto.SecondarySchoolName,
                SecondarySchoolGovernment = dto.SecondarySchoolGovernment,
                HighSchoolPercentage = dto.HighSchoolPercentage,
                DormType = dto.DormType,
                BuildingNumber = dto.BuildingNumber,
                RoomNumber = dto.RoomNumber,
                HasSpecialNeeds = dto.HasSpecialNeeds,
                SpecialNeedsDetails = dto.SpecialNeedsDetails,
                IsExemptFromFees = dto.IsExemptFromFees,
                FatherName = dto.FatherName,
                FatherNationalId = dto.FatherNationalId,
                FatherProfession = dto.FatherProfession,
                FatherPhone = dto.FatherPhone,
                GuardianName = dto.GuardianName,
                GuardianRelationship = dto.GuardianRelationship,
                GuardianPhone = dto.GuardianPhone,
                PhotoUrl = dto.PhotoUrl,
                MissedMealsCount = 0,
                HasOutstandingPayment = false,
                OutstandingAmount = 0
            };

            await _unitOfWork.Students.AddAsync(student);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Student created successfully: StudentId={StudentId}, Name={FirstName} {LastName}",
                student.StudentId, student.FirstName, student.LastName);

            return MapToDto(student);
        }

        public async Task<StudentDto> UpdateStudentAsync(string studentId, CreateStudentDto dto)
        {
            var nationalIdHash = HashString(dto.NationalId);

            var dormLocationId = await _authService.GetCurrentDormLocationIdAsync();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during student update: StudentId={StudentId}", studentId);
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            _logger.LogInformation("Updating student: StudentId={StudentId}, NationalIdHash={NationalIdHash}",
                studentId, nationalIdHash);

            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DormLocationId == dormLocationId);

            if (student == null)
            {
                _logger.LogWarning("Student not found for update: StudentId={StudentId}, DormLocationId={DormLocationId}",
                    studentId, dormLocationId);
                throw new KeyNotFoundException("Student not found");
            }

            // Validate secondary school info for new students
            if (dto.Status == StudentStatus.NewStudent)
            {
                if (string.IsNullOrWhiteSpace(dto.SecondarySchoolName))
                {
                    _logger.LogWarning("Missing secondary school name for updated student: StudentId={StudentId}", studentId);
                    throw new InvalidOperationException("Secondary school name is required for new students");
                }
                if (string.IsNullOrWhiteSpace(dto.SecondarySchoolGovernment))
                {
                    _logger.LogWarning("Missing secondary school government for updated student: StudentId={StudentId}", studentId);
                    throw new InvalidOperationException("Secondary school government is required for new students");
                }
                if (!dto.HighSchoolPercentage.HasValue)
                {
                    _logger.LogWarning("Missing high school percentage for updated student: StudentId={StudentId}", studentId);
                    throw new InvalidOperationException("High school percentage is required for new students");
                }
            }

            // Check if NationalId is being changed and if it already exists
            if (student.NationalId != dto.NationalId)
            {
                var existingNationalId = await _unitOfWork.Students
                    .FindAsync(s => s.NationalId == dto.NationalId &&
                                   !(s.StudentId == studentId && s.DormLocationId == dormLocationId));

                if (existingNationalId.Any())
                {
                    _logger.LogWarning("Duplicate national ID during update: StudentId={StudentId}, NationalIdHash={NationalIdHash}",
                        studentId, nationalIdHash);
                    throw new InvalidOperationException("National ID already exists");
                }
            }

            // Update properties
            student.NationalId = dto.NationalId;
            student.IsEgyptian = dto.IsEgyptian;
            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            student.Status = dto.Status;
            student.Email = dto.Email;
            student.PhoneNumber = dto.PhoneNumber;
            student.Religion = dto.Religion;
            student.Government = dto.Government;
            student.District = dto.District;
            student.StreetName = dto.StreetName;
            student.Faculty = dto.Faculty;
            student.Level = dto.Level;
            student.Grade = dto.Grade;
            student.PercentageGrade = dto.PercentageGrade;
            student.SecondarySchoolName = dto.SecondarySchoolName;
            student.SecondarySchoolGovernment = dto.SecondarySchoolGovernment;
            student.HighSchoolPercentage = dto.HighSchoolPercentage;
            student.DormType = dto.DormType;
            student.BuildingNumber = dto.BuildingNumber;
            student.RoomNumber = dto.RoomNumber;
            student.HasSpecialNeeds = dto.HasSpecialNeeds;
            student.SpecialNeedsDetails = dto.SpecialNeedsDetails;
            student.IsExemptFromFees = dto.IsExemptFromFees;
            student.FatherName = dto.FatherName;
            student.FatherNationalId = dto.FatherNationalId;
            student.FatherProfession = dto.FatherProfession;
            student.FatherPhone = dto.FatherPhone;
            student.GuardianName = dto.GuardianName;
            student.GuardianRelationship = dto.GuardianRelationship;
            student.GuardianPhone = dto.GuardianPhone;

            if (!string.IsNullOrEmpty(dto.PhotoUrl))
            {
                student.PhotoUrl = dto.PhotoUrl;
            }

            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Student updated successfully: StudentId={StudentId}, Name={FirstName} {LastName}",
                student.StudentId, student.FirstName, student.LastName);

            return MapToDto(student);
        }

        public async Task<StudentDto> GetStudentByIdAsync(string studentId)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during student retrieval: StudentId={StudentId}", studentId);
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            _logger.LogDebug("Getting student: StudentId={StudentId}, DormLocationId={DormLocationId}",
                studentId, dormLocationId);

            var student = await _unitOfWork.Students
                .Query()
                .Include(s => s.DormLocation)
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DormLocationId == dormLocationId);

            if (student == null)
            {
                _logger.LogWarning("Student not found: StudentId={StudentId}, DormLocationId={DormLocationId}",
                    studentId, dormLocationId);
                throw new KeyNotFoundException("Student not found");
            }

            return MapToDto(student);
        }

        public async Task<List<StudentDto>> GetAllStudentsAsync()
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

            if (dormLocationId == 0)
            {
                _logger.LogDebug("User not associated with dorm location, returning empty student list");
                return new List<StudentDto>();
            }

            _logger.LogDebug("Getting all students: DormLocationId={DormLocationId}", dormLocationId);

            var students = await _unitOfWork.Students
                .Query()
                .Include(s => s.DormLocation)
                .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted)
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} students for dorm location {DormLocationId}",
                students.Count, dormLocationId);

            return students.Select(MapToDto).ToList();
        }

        public async Task<List<StudentDto>> SearchStudentsAsync(string searchTerm)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

            if (dormLocationId == 0)
            {
                _logger.LogDebug("User not associated with dorm location, returning empty search results");
                return new List<StudentDto>();
            }

            _logger.LogDebug("Searching students: SearchTerm={SearchTerm}, DormLocationId={DormLocationId}",
                searchTerm, dormLocationId);

            var students = await _unitOfWork.Students
                .Query()
                .Include(s => s.DormLocation)
                .Where(s => s.DormLocationId == dormLocationId &&
                           !s.IsDeleted &&
                           (s.StudentId.Contains(searchTerm) ||
                            s.NationalId.Contains(searchTerm) ||
                            s.FirstName.Contains(searchTerm) ||
                            s.LastName.Contains(searchTerm) ||
                            s.Email.Contains(searchTerm) ||
                            s.Faculty.Contains(searchTerm)))
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();

            _logger.LogDebug("Found {Count} students matching search term '{SearchTerm}'",
                students.Count, searchTerm);

            return students.Select(MapToDto).ToList();
        }

        public async Task DeleteStudentAsync(string studentId)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during student deletion: StudentId={StudentId}", studentId);
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            _logger.LogInformation("Deleting student: StudentId={StudentId}, DormLocationId={DormLocationId}",
                studentId, dormLocationId);

            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DormLocationId == dormLocationId);

            if (student == null)
            {
                _logger.LogWarning("Student not found for deletion: StudentId={StudentId}, DormLocationId={DormLocationId}",
                    studentId, dormLocationId);
                throw new KeyNotFoundException("Student not found");
            }

            // Physical delete
            _unitOfWork.Students.Delete(student);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Student deleted: StudentId={StudentId}", studentId);
        }

        public async Task SoftDeleteStudentAsync(string studentId)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during soft delete: StudentId={StudentId}", studentId);
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            _logger.LogInformation("Soft deleting student: StudentId={StudentId}, DormLocationId={DormLocationId}",
                studentId, dormLocationId);

            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DormLocationId == dormLocationId);

            if (student == null)
            {
                _logger.LogWarning("Student not found for soft delete: StudentId={StudentId}, DormLocationId={DormLocationId}",
                    studentId, dormLocationId);
                throw new KeyNotFoundException("Student not found");
            }

            // Soft delete - mark as deleted
            student.IsDeleted = true;
            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Student soft deleted: StudentId={StudentId}", studentId);
        }

        public async Task<string> UploadPhotoAsync(IFormFile file, string studentId)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Invalid file uploaded for student: StudentId={StudentId}", studentId);
                throw new ArgumentException("Invalid file");
            }

            var dormLocationId = _authService.GetCurrentDormLocationId();

            if (dormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during photo upload: StudentId={StudentId}", studentId);
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            var student = await _unitOfWork.Students
                .Query()
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DormLocationId == dormLocationId);

            if (student == null)
            {
                _logger.LogWarning("Student not found for photo upload: StudentId={StudentId}, DormLocationId={DormLocationId}",
                    studentId, dormLocationId);
                throw new KeyNotFoundException("Student not found");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Invalid file type for student photo: StudentId={StudentId}, FileName={FileName}, Extension={Extension}",
                    studentId, file.FileName, extension);
                throw new ArgumentException("Only JPG, JPEG, PNG and GIF files are allowed");
            }

            // Validate file size (5MB max)
            if (file.Length > 5 * 1024 * 1024)
            {
                _logger.LogWarning("File too large for student photo: StudentId={StudentId}, FileName={FileName}, Size={Size} bytes",
                    studentId, file.FileName, file.Length);
                throw new ArgumentException("File size cannot exceed 5MB");
            }

            // Determine if male or female based on dorm location
            bool isMale = dormLocationId == 1;
            string genderFolder = isMale ? "males" : "females";

            // Create uploads folder if it doesn't exist
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", genderFolder, "photos");
            Directory.CreateDirectory(uploadsFolder);

            // Delete old photo if exists
            if (!string.IsNullOrEmpty(student.PhotoUrl))
            {
                var oldPhotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.PhotoUrl.TrimStart('/'));
                if (File.Exists(oldPhotoPath))
                {
                    try
                    {
                        File.Delete(oldPhotoPath);
                        _logger.LogDebug("Deleted old photo: {PhotoPath}", oldPhotoPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Could not delete old photo: {ErrorMessage}", ex.Message);
                    }
                }
            }

            // Generate unique filename
            var fileName = $"{dormLocationId}_{studentId}_{DateTime.UtcNow.Ticks}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photoUrl = $"/uploads/{genderFolder}/photos/{fileName}";

            // Update student photo URL
            student.PhotoUrl = photoUrl;
            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Photo uploaded for student: StudentId={StudentId}, Gender={Gender}, PhotoUrl={PhotoUrl}",
                studentId, genderFolder, photoUrl);

            return photoUrl;
        }

        public async Task<List<StudentDto>> GetStudentsByBuildingAsync(string buildingNumber)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

            if (dormLocationId == 0)
            {
                _logger.LogDebug("User not associated with dorm location, returning empty building list");
                return new List<StudentDto>();
            }

            _logger.LogDebug("Getting students by building: BuildingNumber={BuildingNumber}, DormLocationId={DormLocationId}",
                buildingNumber, dormLocationId);

            var students = await _unitOfWork.Students
                .Query()
                .Include(s => s.DormLocation)
                .Where(s => s.DormLocationId == dormLocationId &&
                           !s.IsDeleted &&
                           s.BuildingNumber == buildingNumber)
                .OrderBy(s => s.RoomNumber)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            _logger.LogDebug("Found {Count} students in building {BuildingNumber}",
                students.Count, buildingNumber);

            return students.Select(MapToDto).ToList();
        }

        public async Task<List<StudentDto>> GetStudentsByFacultyAsync(string faculty)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

            if (dormLocationId == 0)
            {
                _logger.LogDebug("User not associated with dorm location, returning empty faculty list");
                return new List<StudentDto>();
            }

            _logger.LogDebug("Getting students by faculty: Faculty={Faculty}, DormLocationId={DormLocationId}",
                faculty, dormLocationId);

            var students = await _unitOfWork.Students
                .Query()
                .Include(s => s.DormLocation)
                .Where(s => s.DormLocationId == dormLocationId &&
                           !s.IsDeleted &&
                           s.Faculty == faculty)
                .OrderBy(s => s.Level)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            _logger.LogDebug("Found {Count} students in faculty {Faculty}", students.Count, faculty);

            return students.Select(MapToDto).ToList();
        }

        public async Task<List<StudentDto>> GetStudentsByDormLocationAsync(int dormLocationId)
        {
            var currentDormLocationId = _authService.GetCurrentDormLocationId();

            if (currentDormLocationId == 0)
            {
                _logger.LogWarning("User not associated with dorm location during cross-location request");
                throw new UnauthorizedAccessException("User is not associated with a dorm location");
            }

            if (dormLocationId != currentDormLocationId)
            {
                _logger.LogWarning("Cross-dorm location access attempt: Requested={Requested}, Current={Current}",
                    dormLocationId, currentDormLocationId);
            }

            _logger.LogDebug("Getting students by dorm location: DormLocationId={DormLocationId}", dormLocationId);

            var students = await _unitOfWork.Students
                .Query()
                .Include(s => s.DormLocation)
                .Where(s => s.DormLocationId == dormLocationId && !s.IsDeleted)
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} students for dorm location {DormLocationId}",
                students.Count, dormLocationId);

            return students.Select(MapToDto).ToList();
        }

        private StudentDto MapToDto(Student student)
        {
            return new StudentDto
            {
                DormLocationId = student.DormLocationId,
                StudentId = student.StudentId,
                NationalId = student.NationalId,
                IsEgyptian = student.IsEgyptian,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Status = student.Status,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                Religion = student.Religion,
                PhotoUrl = student.PhotoUrl,
                Government = student.Government,
                District = student.District,
                StreetName = student.StreetName,
                Faculty = student.Faculty,
                Level = student.Level,
                Grade = student.Grade,
                PercentageGrade = student.PercentageGrade,
                SecondarySchoolName = student.SecondarySchoolName,
                SecondarySchoolGovernment = student.SecondarySchoolGovernment,
                HighSchoolPercentage = student.HighSchoolPercentage,
                DormType = student.DormType,
                BuildingNumber = student.BuildingNumber,
                RoomNumber = student.RoomNumber,
                HasSpecialNeeds = student.HasSpecialNeeds,
                SpecialNeedsDetails = student.SpecialNeedsDetails,
                IsExemptFromFees = student.IsExemptFromFees,
                MissedMealsCount = student.MissedMealsCount,
                HasOutstandingPayment = student.HasOutstandingPayment,
                OutstandingAmount = student.OutstandingAmount,
                FatherName = student.FatherName,
                FatherNationalId = student.FatherNationalId,
                FatherProfession = student.FatherProfession,
                FatherPhone = student.FatherPhone,
                GuardianName = student.GuardianName,
                GuardianRelationship = student.GuardianRelationship,
                GuardianPhone = student.GuardianPhone,
                CreatedAt = student.CreatedAt,
                UpdatedAt = student.UpdatedAt,
                ModifiedBy = student.LastModifiedBy
            };
        }

        private string HashString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "null";

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes)[..8];
        }
    }
}