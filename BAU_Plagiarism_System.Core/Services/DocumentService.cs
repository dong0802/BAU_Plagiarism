using BAU_Plagiarism_System.Core.DTOs;
using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BAU_Plagiarism_System.Core.Services
{
    /// <summary>
    /// Service quản lý tài liệu (Upload, Download, Quản lý)
    /// </summary>
    public class DocumentService
    {
        private readonly BAUDbContext _context;
        private readonly DocumentReader _documentReader;
        private readonly string _uploadPath;

        public DocumentService(BAUDbContext context, DocumentReader documentReader, string uploadPath = "uploads")
        {
            _context = context;
            _documentReader = documentReader;
            _uploadPath = uploadPath;

            // Đảm bảo thư mục tải lên tồn tại
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        public async Task<List<DocumentDto>> GetAllDocumentsAsync(int? userId = null, int? subjectId = null, string? documentType = null)
        {
            var query = _context.Documents
                .Include(d => d.User)
                .Include(d => d.Subject)
                .Where(d => d.IsActive);

            if (userId.HasValue)
                query = query.Where(d => d.UserId == userId.Value);

            if (subjectId.HasValue)
                query = query.Where(d => d.SubjectId == subjectId.Value);

            if (!string.IsNullOrEmpty(documentType))
                query = query.Where(d => d.DocumentType == documentType);

            return await query
                .OrderByDescending(d => d.UploadDate)
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    Title = d.Title,
                    DocumentType = d.DocumentType,
                    OriginalFileName = d.OriginalFileName,
                    FileSize = d.FileSize,
                    UserId = d.UserId,
                    UserName = d.User.FullName,
                    SubjectId = d.SubjectId,
                    SubjectName = d.Subject != null ? d.Subject.Name : null,
                    Semester = d.Semester,
                    Year = d.Year,
                    UploadDate = d.UploadDate,
                    IsPublic = d.IsPublic,
                    IsActive = d.IsActive
                })
                .ToListAsync();
        }

        public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
        {
            var document = await _context.Documents
                .Include(d => d.User)
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null) return null;

            return new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                DocumentType = document.DocumentType,
                OriginalFileName = document.OriginalFileName,
                FileSize = document.FileSize,
                UserId = document.UserId,
                UserName = document.User.FullName,
                SubjectId = document.SubjectId,
                SubjectName = document.Subject?.Name,
                Semester = document.Semester,
                Year = document.Year,
                UploadDate = document.UploadDate,
                IsPublic = document.IsPublic,
                IsActive = document.IsActive
            };
        }

        public async Task<DocumentDto> UploadDocumentAsync(int userId, DocumentUploadDto dto)
        {
            // Kiểm tra xem người dùng đã tải tệp này lên gần đây không (trong vòng 2 giờ) để tránh bản sao trong bộ lưu trữ
            var existingDoc = await _context.Documents
                .Where(d => d.UserId == userId && 
                           d.OriginalFileName == dto.FileName && 
                           d.FileSize == dto.FileContent.Length &&
                           d.UploadDate > DateTime.Now.AddHours(-2))
                .OrderByDescending(d => d.UploadDate)
                .FirstOrDefaultAsync();

            if (existingDoc != null)
            {
                // Tái sử dụng tài liệu hiện có nếu tìm thấy
                await _context.Entry(existingDoc).Reference(d => d.User).LoadAsync();
                if (existingDoc.SubjectId.HasValue)
                    await _context.Entry(existingDoc).Reference(d => d.Subject).LoadAsync();

                return new DocumentDto
                {
                    Id = existingDoc.Id,
                    Title = existingDoc.Title,
                    DocumentType = existingDoc.DocumentType,
                    OriginalFileName = existingDoc.OriginalFileName,
                    FileSize = existingDoc.FileSize,
                    UserId = existingDoc.UserId,
                    UserName = existingDoc.User.FullName,
                    SubjectId = existingDoc.SubjectId,
                    SubjectName = existingDoc.Subject?.Name,
                    Semester = existingDoc.Semester,
                    Year = existingDoc.Year,
                    UploadDate = existingDoc.UploadDate,
                    IsPublic = existingDoc.IsPublic,
                    IsActive = existingDoc.IsActive
                };
            }

            // Lưu tệp vào đĩa
            var fileName = $"{Guid.NewGuid()}_{dto.FileName}";
            var filePath = Path.Combine(_uploadPath, fileName);
            await File.WriteAllBytesAsync(filePath, dto.FileContent);

            // Trích xuất nội dung văn bản từ tệp
            string content;
            try
            {
                content = await _documentReader.ExtractTextAsync(filePath);
            }
            catch (Exception ex)
            {
                // Nếu trích xuất thất bại, xóa tệp và ném lỗi
                File.Delete(filePath);
                throw new Exception($"Không thể trích xuất văn bản từ tài liệu: {ex.Message}");
            }

            // Tạo thực thể tài liệu
            var document = new Document
            {
                Title = dto.Title,
                DocumentType = dto.DocumentType,
                Content = content,
                OriginalFileName = dto.FileName,
                FilePath = filePath,
                FileSize = dto.FileContent.Length,
                UserId = userId,
                SubjectId = dto.SubjectId,
                Semester = dto.Semester,
                Year = dto.Year,
                IsPublic = dto.IsPublic,
                IsActive = dto.IsActive,
                UploadDate = DateTime.Now
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Tải các thực thể liên quan
            await _context.Entry(document).Reference(d => d.User).LoadAsync();
            if (document.SubjectId.HasValue)
                await _context.Entry(document).Reference(d => d.Subject).LoadAsync();

            return new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                DocumentType = document.DocumentType,
                OriginalFileName = document.OriginalFileName,
                FileSize = document.FileSize,
                UserId = document.UserId,
                UserName = document.User.FullName,
                SubjectId = document.SubjectId,
                SubjectName = document.Subject?.Name,
                Semester = document.Semester,
                Year = document.Year,
                UploadDate = document.UploadDate,
                IsPublic = document.IsPublic,
                IsActive = document.IsActive
            };
        }

        public async Task<DocumentDto> CreateDocumentFromTextAsync(int userId, DocumentTextDto dto)
        {
            // Tạo thực thể tài liệu
            var document = new Document
            {
                Title = string.IsNullOrWhiteSpace(dto.Title) ? $"Văn bản dán_{DateTime.Now:yyyyMMdd_HHmm}" : dto.Title,
                DocumentType = dto.DocumentType,
                Content = dto.Content,
                OriginalFileName = "pasted_text.txt",
                FilePath = string.Empty, // Không có tệp vật lý cho văn bản dán
                FileSize = System.Text.Encoding.UTF8.GetByteCount(dto.Content),
                UserId = userId,
                SubjectId = dto.SubjectId,
                Semester = dto.Semester,
                Year = dto.Year,
                IsPublic = dto.IsPublic,
                IsActive = dto.IsActive,
                UploadDate = DateTime.Now
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Tải các thực thể liên quan
            await _context.Entry(document).Reference(d => d.User).LoadAsync();
            if (document.SubjectId.HasValue)
                await _context.Entry(document).Reference(d => d.Subject).LoadAsync();

            return new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                DocumentType = document.DocumentType,
                OriginalFileName = document.OriginalFileName,
                FileSize = document.FileSize,
                UserId = document.UserId,
                UserName = document.User.FullName,
                SubjectId = document.SubjectId,
                SubjectName = document.Subject?.Name,
                Semester = document.Semester,
                Year = document.Year,
                UploadDate = document.UploadDate,
                IsPublic = document.IsPublic,
                IsActive = document.IsActive
            };
        }

        public async Task<DocumentDto?> UpdateDocumentAsync(int id, UpdateDocumentDto dto)
        {
            var document = await _context.Documents
                .Include(d => d.User)
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null) return null;

            document.Title = dto.Title;
            document.DocumentType = dto.DocumentType;
            document.SubjectId = dto.SubjectId;
            document.Semester = dto.Semester;
            document.Year = dto.Year;
            document.IsPublic = dto.IsPublic;
            document.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                DocumentType = document.DocumentType,
                OriginalFileName = document.OriginalFileName,
                FileSize = document.FileSize,
                UserId = document.UserId,
                UserName = document.User.FullName,
                SubjectId = document.SubjectId,
                SubjectName = document.Subject?.Name,
                Semester = document.Semester,
                Year = document.Year,
                UploadDate = document.UploadDate,
                IsPublic = document.IsPublic,
                IsActive = document.IsActive
            };
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null) return false;

            // Xóa mềm
            document.IsActive = false;
            await _context.SaveChangesAsync();

            // Tùy chọn xóa tệp vật lý
            if (!string.IsNullOrEmpty(document.FilePath) && File.Exists(document.FilePath))
            {
                try
                {
                    File.Delete(document.FilePath);
                }
                catch
                {
                    // Ghi lại lỗi nhưng không làm thất bại hoạt động
                }
            }

            return true;
        }

        public async Task<byte[]?> DownloadDocumentAsync(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null || string.IsNullOrEmpty(document.FilePath))
                return null;

            if (!File.Exists(document.FilePath))
                return null;

            return await File.ReadAllBytesAsync(document.FilePath);
        }

        public async Task<string?> GetDocumentContentAsync(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            return document?.Content;
        }
    }
}
