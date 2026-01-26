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
    public class DocumentQualityController : ControllerBase
    {
        private readonly DocumentQualityService _qualityService;
        private readonly DocumentService _documentService;

        public DocumentQualityController(DocumentQualityService qualityService, DocumentService documentService)
        {
            _qualityService = qualityService;
            _documentService = documentService;
        }

        /// <summary>
        /// Phân tích chất lượng tài liệu và nhận phản hồi chấm điểm tự động
        /// </summary>
        [HttpPost("analyze/{documentId}")]
        public async Task<ActionResult<DocumentQualityAnalysisDto>> AnalyzeDocument(int documentId)
        {
            try
            {
                var content = await _documentService.GetDocumentContentAsync(documentId);
                if (content == null)
                    return NotFound(new { message = "Không tìm thấy tài liệu" });

                var document = await _documentService.GetDocumentByIdAsync(documentId);
                var analysis = _qualityService.AnalyzeDocument(content, document?.Title ?? "");

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Phân tích nội dung văn bản trực tiếp (không cần lưu dưới dạng tài liệu)
        /// </summary>
        [HttpPost("analyze-text")]
        public ActionResult<DocumentQualityAnalysisDto> AnalyzeText([FromBody] AnalyzeTextRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { message = "Nội dung không được để trống" });

                var analysis = _qualityService.AnalyzeDocument(request.Content, request.Title ?? "");
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class AnalyzeTextRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? Title { get; set; }
    }
}
