using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Data.Models;
using BAU_Plagiarism_System.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace BAU_Plagiarism_System.API.Data
{
    public class MassiveDataSeeder
    {
        private static readonly string[] Topics = {
            "Chuyển đổi số trong ngân hàng thương mại",
            "Quản trị rủi ro tín dụng theo chuẩn Basel II",
            "Phân tích báo cáo tài chính các ngân hàng niêm yết",
            "Thúc đẩy thanh toán không dùng tiền mặt tại Việt Nam",
            "Ứng dụng Fintech trong dịch vụ tài chính cá nhân",
            "Nâng cao năng lực cạnh tranh của Ngân hàng Agribank",
            "Phát triển dịch vụ ngân hàng bán lẻ tại BIDV",
            "Quản lý nợ xấu tại các ngân hàng thương mại cổ phần"
        };

        private static readonly string[] ContentTemplates = {
            "Trong bối cảnh cách mạng công nghiệp 4.0, việc {0} đang trở thành xu thế tất yếu. Nghiên cứu này tập trung vào thực trạng tại Học viện Ngân hàng và các ngân hàng đối tác. Kết quả cho thấy việc áp dụng công nghệ mới giúp giảm 30% chi phí vận hành.",
            "Bài báo cáo phân tích sâu về {0}. Qua khảo sát 500 khách hàng tại Hà Nội, nhóm tác giả nhận thấy các nhân tố ảnh hưởng lớn nhất bao gồm sự tin tưởng và tính tiện dụng của hệ thống phần mềm.",
            "Mục tiêu của nghiên cứu là tìm ra giải pháp cho {0}. Chúng tôi sử dụng phương pháp định lượng qua mô hình hồi quy đa biến để đánh giá tác động của các biến số kinh tế vĩ mô.",
            "Quy trình {0} tại Việt Nam vẫn còn nhiều hạn chế về khung pháp lý. Cần có sự phối hợp chặt chẽ giữa Ngân hàng Nhà nước và các tổ chức tài chính để đảm bảo tính minh bạch."
        };

        public static async Task SeedMassiveAsync(BAUDbContext context, int count = 100)
        {
            if (context.Documents.Count() > 10) return;

            Console.WriteLine($"Starting Massive Seed: {count} documents...");
            
            var random = new Random();
            var users = await context.Users.Where(u => u.Role == "Student").ToListAsync();
            var subjects = await context.Subjects.ToListAsync();
            
            if (!users.Any() || !subjects.Any()) return;

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var newDocs = new List<Document>();

            for (int i = 1; i <= count; i++)
            {
                var topic = Topics[random.Next(Topics.Length)];
                var template = ContentTemplates[random.Next(ContentTemplates.Length)];
                var title = $"{topic} - Nhóm {random.Next(1, 20)} - K{random.Next(20, 26)}";
                var content = string.Format(template, topic.ToLower()) + 
                             "\n" + string.Join(" ", Enumerable.Repeat("Dữ liệu phân tích chi tiết dựa trên báo cáo thường niên của các ngân hàng giai đoạn 2020-2024.", 5));

                var doc = new Document
                {
                    Title = title,
                    DocumentType = i % 3 == 0 ? "Thesis" : "Essay",
                    Content = content,
                    UserId = users[random.Next(users.Count)].Id,
                    SubjectId = subjects[random.Next(subjects.Count)].Id,
                    Semester = $"HK{random.Next(1, 3)}-2024",
                    Year = 2024,
                    IsPublic = true,
                    OriginalFileName = $"Bao_cao_{Guid.NewGuid().ToString().Substring(0, 8)}.docx",
                    FilePath = Path.Combine(uploadPath, $"massive_doc_{i}.txt"),
                    FileSize = content.Length * 2,
                    UploadDate = DateTime.Now.AddDays(-random.Next(1, 30))
                };
                
                newDocs.Add(doc);
                // Create dummy physical file
                await File.WriteAllTextAsync(doc.FilePath, content);
            }

            context.Documents.AddRange(newDocs);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Successfully seeded {count} massive documents for HVNH plagiarism database!");
        }
    }
}
