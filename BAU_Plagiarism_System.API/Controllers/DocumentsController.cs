using BAU_Plagiarism_System.Core.DTOs;
using BAU_Plagiarism_System.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BAU_Plagiarism_System.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentService _documentService;

        public DocumentsController(DocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpGet]
        public async Task<ActionResult<List<DocumentDto>>> GetDocuments(
            [FromQuery] int? userId = null,
            [FromQuery] int? subjectId = null,
            [FromQuery] string? documentType = null)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole == "Student")
            {
                userId = currentUserId;
            }

            // Admin/Lecturer: exclude student-uploaded documents from the repository
            bool excludeStudentDocs = userRole != "Student" && !userId.HasValue;

            var documents = await _documentService.GetAllDocumentsAsync(userId, subjectId, documentType, excludeStudentDocs);
            return Ok(documents);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentDto>> GetDocument(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();
            return Ok(document);
        }

        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)] // 100MB
        public async Task<ActionResult<DocumentDto>> UploadDocument([FromForm] IFormFile file, [FromForm] string title,
            [FromForm] string documentType = "Essay", [FromForm] int? subjectId = null,
            [FromForm] string? semester = null, [FromForm] string? className = null, [FromForm] int? year = null, 
            [FromForm] bool? isPublic = false, [FromForm] bool? isActive = true)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdStr ?? "0");
            
            Console.WriteLine($"[DOC-CONTROLLER] UploadDocument started. User: {userId} ({userIdStr}), Title: {title}, Public: {isPublic}, Active: {isActive}");
            
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Không có tệp được tải lên" });

                Console.WriteLine($"[DOC-CONTROLLER] File received: {file.FileName} ({file.Length} bytes)");

                var dto = new DocumentUploadDto
                {
                    Title = title,
                    DocumentType = documentType,
                    SubjectId = subjectId,
                    Semester = semester,
                    ClassName = className,
                    Year = year,
                    IsPublic = isPublic ?? false,
                    IsActive = isActive ?? true,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    FileStream = file.OpenReadStream() // TỐI ƯU: Đọc trực tiếp từ Stream
                };

                Console.WriteLine($"[DOC-CONTROLLER] Calling DocumentService.UploadDocumentAsync...");
                var document = await _documentService.UploadDocumentAsync(userId, dto);
                Console.WriteLine($"[DOC-CONTROLLER] Upload successful! ID: {document.Id}");
                return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DOC-CONTROLLER] UPLOAD FAILED: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"[DOC-CONTROLLER] INNER ERROR: {ex.InnerException.Message}");
                Console.WriteLine(ex.StackTrace);
                return BadRequest(new { message = "Lỗi tải lên: " + ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpPost("paste-text")]
        public async Task<ActionResult<DocumentDto>> CreateFromText([FromBody] DocumentTextDto dto)
        {
            Console.WriteLine($"[DOC-CONTROLLER] CreateFromText called. Title: {dto.Title}");
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (string.IsNullOrWhiteSpace(dto.Content))
                    return BadRequest(new { message = "Nội dung không được để trống" });

                var document = await _documentService.CreateDocumentFromTextAsync(userId, dto);
                return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<DocumentDto>> UpdateDocument(int id, [FromBody] UpdateDocumentDto dto)
        {
            var document = await _documentService.UpdateDocumentAsync(id, dto);
            if (document == null)
                return NotFound();
            return Ok(document);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDocument(int id)
        {
            var result = await _documentService.DeleteDocumentAsync(id);
            if (!result)
                return NotFound();
            return NoContent();
        }

        [HttpGet("{id}/download")]
        public async Task<ActionResult> DownloadDocument(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();

            var fileContent = await _documentService.DownloadDocumentAsync(id);
            if (fileContent == null)
                return NotFound(new { message = "Không tìm thấy tệp" });

            return File(fileContent, "application/octet-stream", document.OriginalFileName);
        }

        [HttpGet("{id}/content")]
        public async Task<ActionResult<string>> GetDocumentContent(int id)
        {
            var content = await _documentService.GetDocumentContentAsync(id);
            if (content == null)
                return NotFound();
            return Ok(new { content });
        }
    }
}
