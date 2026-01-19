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
            var documents = await _documentService.GetAllDocumentsAsync(userId, subjectId, documentType);
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
        public async Task<ActionResult<DocumentDto>> UploadDocument([FromForm] IFormFile file, [FromForm] string title,
            [FromForm] string documentType = "Essay", [FromForm] int? subjectId = null,
            [FromForm] string? semester = null, [FromForm] int? year = null, [FromForm] bool isPublic = false)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded" });

                // Read file content
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileContent = memoryStream.ToArray();

                var dto = new DocumentUploadDto
                {
                    Title = title,
                    DocumentType = documentType,
                    SubjectId = subjectId,
                    Semester = semester,
                    Year = year,
                    IsPublic = isPublic,
                    FileContent = fileContent,
                    FileName = file.FileName
                };

                var document = await _documentService.UploadDocumentAsync(userId, dto);
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
                return NotFound(new { message = "File not found" });

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
