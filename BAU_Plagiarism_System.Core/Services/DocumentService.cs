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

            // Ensure upload directory exists
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
            // Check if user already uploaded this file recently (within 2 hours) to avoid duplicates in storage
            var existingDoc = await _context.Documents
                .Where(d => d.UserId == userId && 
                           d.OriginalFileName == dto.FileName && 
                           d.FileSize == dto.FileContent.Length &&
                           d.UploadDate > DateTime.Now.AddHours(-2))
                .OrderByDescending(d => d.UploadDate)
                .FirstOrDefaultAsync();

            if (existingDoc != null)
            {
                // Reuse existing document if found
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

            // Save file to disk
            var fileName = $"{Guid.NewGuid()}_{dto.FileName}";
            var filePath = Path.Combine(_uploadPath, fileName);
            await File.WriteAllBytesAsync(filePath, dto.FileContent);

            // Extract text content from file
            string content;
            try
            {
                content = await _documentReader.ExtractTextAsync(filePath);
            }
            catch (Exception ex)
            {
                // If extraction fails, delete the file and throw
                File.Delete(filePath);
                throw new Exception($"Failed to extract text from document: {ex.Message}");
            }

            // Create document entity
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

            // Load related entities
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

            // Soft delete
            document.IsActive = false;
            await _context.SaveChangesAsync();

            // Optionally delete physical file
            if (!string.IsNullOrEmpty(document.FilePath) && File.Exists(document.FilePath))
            {
                try
                {
                    File.Delete(document.FilePath);
                }
                catch
                {
                    // Log error but don't fail the operation
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
