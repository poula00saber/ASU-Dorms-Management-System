using ASUDorms.Application.DTOs.Students;
using ASUDorms.Application.Interfaces;
using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Services
{
    public class StudentService : IStudentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public StudentService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<StudentDto> CreateStudentAsync(CreateStudentDto dto)
        {
            var dormLocationId = _authService.GetCurrentDormLocationId();

            // Check if StudentId already exists in this location
            var existingStudent = await _unitOfWork.Students
                .FindAsync(s => s.StudentId == dto.StudentId && s.DormLocationId == dormLocationId);

            if (existingStudent.Any())
            {
                throw new InvalidOperationException("Student ID already exists in this location");
            }

            // Check if NationalId already exists globally
            var existingNationalId = await _unitOfWork.Students
                .FindAsync(s => s.NationalId == dto.NationalId);

            if (existingNationalId.Any())
            {
                throw new InvalidOperationException("National ID already exists");
            }

            var student = new Student
            {
                StudentId = dto.StudentId,
                NationalId = dto.NationalId,
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
                Grade = dto.Grade,
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
                DormLocationId = dormLocationId,
                PhotoUrl= dto.PhotoUrl
            };

            await _unitOfWork.Students.AddAsync(student);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(student);
        }

        public async Task<StudentDto> UpdateStudentAsync(string studentId, CreateStudentDto dto)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId);

            if (student == null)
            {
                throw new KeyNotFoundException("Student not found");
            }

            // Update properties
            student.NationalId = dto.NationalId;
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
            student.Grade = dto.Grade;
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
            student.PhotoUrl = dto.PhotoUrl;

            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(student);
        }

        public async Task<StudentDto> GetStudentByIdAsync(string studentId)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId);

            if (student == null)
            {
                throw new KeyNotFoundException("Student not found");
            }

            return MapToDto(student);
        }

        public async Task<List<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _unitOfWork.Students.GetAllAsync();
            return students.Select(MapToDto).ToList();
        }

        public async Task DeleteStudentAsync(string studentId)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId);

            if (student == null)
            {
                throw new KeyNotFoundException("Student not found");
            }

            student.IsDeleted = true;
            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<string> UploadPhotoAsync(IFormFile file, string studentId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Invalid file");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Only JPG and PNG files are allowed");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{studentId}_{DateTime.UtcNow.Ticks}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photoUrl = $"/uploads/photos/{fileName}";

            var student = await _unitOfWork.Students.GetByIdAsync(studentId);
            if (student != null)
            {
                student.PhotoUrl = photoUrl;
                _unitOfWork.Students.Update(student);
                await _unitOfWork.SaveChangesAsync();
            }

            return photoUrl;
        }

        private StudentDto MapToDto(Student student)
        {
            return new StudentDto
            {
                StudentId = student.StudentId,
                NationalId = student.NationalId,
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
                Grade = student.Grade,
                DormType = student.DormType,
                BuildingNumber = student.BuildingNumber,
                RoomNumber = student.RoomNumber,
                HasSpecialNeeds = student.HasSpecialNeeds,
                SpecialNeedsDetails = student.SpecialNeedsDetails,
                IsExemptFromFees = student.IsExemptFromFees,
                FatherName = student.FatherName,
                FatherNationalId = student.FatherNationalId,
                FatherProfession = student.FatherProfession,
                FatherPhone = student.FatherPhone,
                GuardianName = student.GuardianName,
                GuardianRelationship = student.GuardianRelationship,
                GuardianPhone = student.GuardianPhone,
                DormLocationId = student.DormLocationId,
                
            };
        }
    }
}
