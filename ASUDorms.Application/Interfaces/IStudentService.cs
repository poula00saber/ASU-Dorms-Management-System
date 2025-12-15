using ASUDorms.Application.DTOs.Students;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.Interfaces
{
    public interface IStudentService
    {
        Task<StudentDto> CreateStudentAsync(CreateStudentDto dto);
        Task<StudentDto> UpdateStudentAsync(string studentId, CreateStudentDto dto);
        Task<StudentDto> GetStudentByIdAsync(string studentId);
        Task<List<StudentDto>> GetAllStudentsAsync();
        Task DeleteStudentAsync(string studentId);
        Task<string> UploadPhotoAsync(IFormFile file, string studentId);
        Task<List<StudentDto>> GetStudentsByDormLocationAsync(int dormLocationId);

    }
}
