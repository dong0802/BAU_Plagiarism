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
            // Check if data already exists
            if (context.Faculties.Any())
            {
                Console.WriteLine("Database already seeded.");
                return;
            }

            Console.WriteLine("Seeding database...");

            // Ensure upload directory exists
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            // ============= SEED FACULTIES =============
            var faculties = new List<Faculty>
            {
                new Faculty { Code = "NH", Name = "Khoa Ngân hàng", Description = "Khoa đào tạo chuyên ngành Ngân hàng" },
                new Faculty { Code = "TC", Name = "Khoa Tài chính", Description = "Khoa đào tạo chuyên ngành Tài chính" },
                new Faculty { Code = "KT", Name = "Khoa Kế toán", Description = "Khoa đào tạo chuyên ngành Kế toán" },
                new Faculty { Code = "QTKD", Name = "Khoa Quản trị kinh doanh", Description = "Khoa đào tạo chuyên ngành Quản trị" }
            };
            context.Faculties.AddRange(faculties);
            await context.SaveChangesAsync();

            // ============= SEED DEPARTMENTS =============
            var departments = new List<Department>
            {
                new Department { Code = "NHTM", Name = "Bộ môn Ngân hàng thương mại", FacultyId = faculties[0].Id },
                new Department { Code = "TCDN", Name = "Bộ môn Tài chính doanh nghiệp", FacultyId = faculties[1].Id },
                new Department { Code = "KTTC", Name = "Bộ môn Kế toán tài chính", FacultyId = faculties[2].Id }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();

            // ============= SEED SUBJECTS =============
            var subjects = new List<Subject>
            {
                new Subject { Code = "NH101", Name = "Nguyên lý ngân hàng", Credits = 3, DepartmentId = departments[0].Id },
                new Subject { Code = "TC101", Name = "Tài chính doanh nghiệp", Credits = 3, DepartmentId = departments[1].Id },
                new Subject { Code = "KT101", Name = "Nguyên lý kế toán", Credits = 3, DepartmentId = departments[2].Id }
            };
            context.Subjects.AddRange(subjects);
            await context.SaveChangesAsync();

            // ============= SEED USERS =============
            var users = new List<User>
            {
                new User { Username = "admin", PasswordHash = UserService.HashPassword("admin123"), FullName = "Quản trị viên", Email = "admin@bau.edu.vn", Role = "Admin", FacultyId = faculties[0].Id },
                new User { Username = "gv001", PasswordHash = UserService.HashPassword("gv001"), FullName = "TS. Nguyễn Văn An", Email = "nvan@bau.edu.vn", Role = "Admin", LecturerId = "GV001", FacultyId = faculties[0].Id, DepartmentId = departments[0].Id },
                new User { Username = "21a4010001", PasswordHash = UserService.HashPassword("student123"), FullName = "Nguyễn Văn A", Email = "21a4010001@sv.bau.edu.vn", Role = "Student", StudentId = "21A4010001", FacultyId = faculties[0].Id }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // ============= SEED SAMPLE DOCUMENTS =============
            var docs = new List<Document>
            {
                new Document
                {
                    Title = "Quản trị rủi ro tín dụng tại ngân hàng thương mại",
                    DocumentType = "Thesis",
                    Content = @"Rủi ro tín dụng là khả năng khách hàng không thực hiện được nghĩa vụ trả nợ theo cam kết. 
                    Để giảm thiểu rủi ro này, ngân hàng cần áp dụng quy trình thẩm định tín dụng chặt chẽ, chấm điểm tín dụng (Credit Scoring).",
                    UserId = users[2].Id,
                    SubjectId = subjects[0].Id,
                    Semester = "HK1-2024",
                    Year = 2024,
                    IsPublic = true,
                    OriginalFileName = "Quan_tri_rui_ro_tin_dung.docx",
                    FilePath = Path.Combine(uploadPath, "seed_doc_1.txt"),
                    FileSize = 1024,
                    UploadDate = DateTime.Now.AddDays(-10)
                },
                new Document
                {
                    Title = "Phân tích hiệu quả kinh doanh tại BIDV",
                    DocumentType = "Research",
                    Content = @"Hiệu quả hoạt động kinh doanh của ngân hàng được đánh giá qua các chỉ số như ROA (Return on Assets), ROE (Return on Equity) và NIM.",
                    UserId = users[2].Id,
                    SubjectId = subjects[1].Id,
                    Semester = "HK2-2024",
                    Year = 2024,
                    IsPublic = true,
                    OriginalFileName = "Phan_tich_BIDV.docx",
                    FilePath = Path.Combine(uploadPath, "seed_doc_2.txt"),
                    FileSize = 2048,
                    UploadDate = DateTime.Now.AddDays(-5)
                }
            };
            context.Documents.AddRange(docs);
            await context.SaveChangesAsync();

            // Create physical dummy files
            foreach (var d in docs)
            {
                if (!string.IsNullOrEmpty(d.FilePath))
                {
                    await File.WriteAllTextAsync(d.FilePath, d.Content);
                }
            }

            // ============= SEED PLAGIARISM CHECKS =============
            var check = new PlagiarismCheck
            {
                SourceDocumentId = docs[0].Id,
                UserId = users[2].Id,
                CheckDate = DateTime.Now.AddDays(-2),
                OverallSimilarityPercentage = 15.5m,
                Status = "Completed",
                TotalMatchedDocuments = 1,
                Notes = "Kiểm tra định kỳ"
            };
            context.PlagiarismChecks.Add(check);
            await context.SaveChangesAsync();

            var match = new PlagiarismMatch
            {
                PlagiarismCheckId = check.Id,
                MatchedDocumentId = docs[1].Id,
                MatchedText = "Hiệu quả hoạt động kinh doanh của ngân hàng",
                StartPosition = 0,
                EndPosition = 45,
                SimilarityScore = 45.0m
            };
            context.PlagiarismMatches.Add(match);
            await context.SaveChangesAsync();

            Console.WriteLine("Database seeded successfully with history!");
        }
    }
}
