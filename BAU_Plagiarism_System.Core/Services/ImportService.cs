using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Data.Models;
using BAU_Plagiarism_System.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BAU_Plagiarism_System.Core.Services
{
    public class ImportService
    {
        private readonly BAUDbContext _context;
        private readonly DocumentReader _documentReader;
        private readonly string _uploadPath;

        public ImportService(BAUDbContext context, DocumentReader documentReader)
        {
            _context = context;
            _documentReader = documentReader;
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(_uploadPath)) Directory.CreateDirectory(_uploadPath);
        }

        public async Task<ImportResult> ImportFromFolderAsync(string folderPath, int userId)
        {
            var result = new ImportResult();
            
            if (!Directory.Exists(folderPath))
            {
                result.Message = "Thư mục không tồn tại.";
                return result;
            }

            var allowedExtensions = new[] { ".docx", ".pdf", ".txt" };
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                 .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()))
                                 .ToList();

            if (!files.Any())
            {
                result.Message = "Không tìm thấy file hợp lệ (.docx, .pdf, .txt) trong thư mục.";
                return result;
            }

            foreach (var filePath in files)
            {
                try
                {
                    var fileName = Path.GetFileName(filePath);
                    var title = Path.GetFileNameWithoutExtension(fileName).Replace("_", " ").Replace("-", " ");
                    
                    // Extract text
                    var content = await _documentReader.ExtractTextAsync(filePath);
                    
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        result.FailedFiles.Add($"{fileName} (Không trích xuất được nội dung)");
                        continue;
                    }

                    // Save file to uploads
                    var fileExtension = Path.GetExtension(fileName);
                    var newFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var destPath = Path.Combine(_uploadPath, newFileName);
                    File.Copy(filePath, destPath, true);

                    // Create document record
                    var doc = new Document
                    {
                        Title = title,
                        Content = content,
                        UserId = userId,
                        OriginalFileName = fileName,
                        FilePath = destPath,
                        FileSize = new FileInfo(filePath).Length,
                        UploadDate = DateTime.Now,
                        IsPublic = true,
                        IsActive = true,
                        DocumentType = "Thesis",
                        Year = DateTime.Now.Year,
                        Semester = "HK1"
                    };

                    _context.Documents.Add(doc);
                    result.SuccessCount++;
                    result.ImportedTitles.Add(title);
                }
                catch (Exception ex)
                {
                    result.FailedFiles.Add($"{Path.GetFileName(filePath)} (Lỗi: {ex.Message})");
                }
            }

            await _context.SaveChangesAsync();
            result.Message = $"Đã nhập thành công {result.SuccessCount}/{files.Count} tài liệu.";
            return result;
        }
    }

    public class ImportResult
    {
        public int SuccessCount { get; set; }
        public List<string> ImportedTitles { get; set; } = new();
        public List<string> FailedFiles { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
