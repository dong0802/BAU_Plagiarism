using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Data.Models;
using BAU_Plagiarism_System.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace BAU_Plagiarism_System.API.Data
{
    public class SeedData
    {
        public static async Task SeedAsync(BAUDbContext context)
        {
            // Only seed if empty to prevent ID shifts and breaking tokens
            if (context.Faculties.Any())
            {
                Console.WriteLine("Database already has data. Skipping re-seed to maintain ID consistency.");
                return;
            }

            Console.WriteLine("Seeding database for Bank Academy of Vietnam (HVNH)...");

            // Ensure upload directory exists
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            // ============= SEED FACULTY =============
            var faculty = new Faculty 
            { 
                Code = "CNTT", 
                Name = "Khoa Công nghệ thông tin và Kinh tế số", 
                Description = "Faculty of IT and Digital Economy" 
            };
            context.Faculties.Add(faculty);
            await context.SaveChangesAsync();

            // ============= SEED DEPARTMENT =============
            var department = new Department 
            { 
                Code = "NLS", 
                Name = "Bộ môn Năng lực số", 
                Description = "Department of Digital Competency", 
                FacultyId = faculty.Id 
            };
            context.Departments.Add(department);
            await context.SaveChangesAsync();

            // ============= SEED SUBJECT =============
            var subject = new Subject 
            { 
                Code = "NLS01", 
                Name = "Năng lực số", 
                Description = "Digital Competency", 
                Credits = 3, 
                DepartmentId = department.Id 
            };
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            // ============= SEED ADMIN USER =============
            var admin = new User 
            { 
                Username = "admin", 
                PasswordHash = UserService.HashPassword("admin123"), 
                FullName = "Quản trị viên HVNH", 
                Email = "admin@hvnh.edu.vn", 
                Role = "Admin", 
                FacultyId = faculty.Id,
                IsActive = true,
                DailyCheckLimit = 999,
                CreatedDate = DateTime.Now
            };
            
            context.Users.Add(admin);
            await context.SaveChangesAsync();

            Console.WriteLine("Database setup completed: Master data for HVNH created.");
        }
    }
}
