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
    public class PlagiarismController : ControllerBase
    {
        private readonly PlagiarismService _plagiarismService;
        private readonly BAU_Plagiarism_System.Data.BAUDbContext _context;

        public PlagiarismController(PlagiarismService plagiarismService, BAU_Plagiarism_System.Data.BAUDbContext context)
        {
            _plagiarismService = plagiarismService;
            _context = context;
        }

        /// <summary>
        /// Kiểm tra đạo văn cho một tài liệu
        /// </summary>
        [HttpPost("check")]
        public async Task<ActionResult<PlagiarismCheckResultDto>> CheckPlagiarism([FromBody] CreatePlagiarismCheckDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _plagiarismService.CheckPlagiarismAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        public class CompareTwoDto 
        {
            public int Document1Id { get; set; }
            public int Document2Id { get; set; }
        }

        /// <summary>
        /// So sánh trực tiếp 2 tài liệu với nhau (1v1)
        /// </summary>
        [HttpPost("compare-1v1")]
        public async Task<ActionResult> Compare1v1([FromBody] CompareTwoDto dto)
        {
            var doc1 = await _context.Documents.FindAsync(dto.Document1Id);
            var doc2 = await _context.Documents.FindAsync(dto.Document2Id);
            if (doc1 == null || doc2 == null) return NotFound("Không tìm thấy một trong hai tài liệu.");

            var checker = this.HttpContext.RequestServices.GetRequiredService<SimilarityChecker>();
            var result = checker.AnalyzeDetailed(doc1.Content ?? "", new List<BAU_Plagiarism_System.Data.Models.Document> { doc2 });

            var matches = result.Segments
                .Where(s => s.Score > 0 && !s.IsExcluded && !string.IsNullOrEmpty(s.MatchedText))
                .Select(s => new { matchedText = s.MatchedText })
                .ToList();

            return Ok(new
            {
                SourceTitle = doc1.Title,
                TargetTitle = doc2.Title,
                OverallScore = result.OverallScore,
                Matches = matches,
                Segments = result.Segments
            });
        }

        /// <summary>
        /// Lấy lịch sử kiểm tra đạo văn
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<List<PlagiarismCheckDto>>> GetHistory(
            [FromQuery] int? userId = null,
            [FromQuery] int? documentId = null,
            [FromQuery] int? limit = null)
        {
            // Nếu không phải quản trị viên, chỉ hiển thị lịch sử của chính mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin")
            {
                userId = currentUserId;
            }

            var history = await _plagiarismService.GetPlagiarismHistoryAsync(userId, documentId, limit);
            return Ok(history);
        }

        /// <summary>
        /// Lấy chi tiết kết quả kiểm tra
        /// </summary>
        [HttpGet("checks/{checkId}")]
        public async Task<ActionResult<PlagiarismCheckDto>> GetCheckDetail(int checkId)
        {
            var check = await _plagiarismService.GetCheckDetailAsync(checkId);
            if (check == null)
                return NotFound();

            // Phân quyền: Chỉ chủ sở hữu, giảng viên hoặc quản trị viên mới có thể xem
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && check.UserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(check);
        }

        /// <summary>
        /// Lấy thống kê đạo văn
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,Student")]
        public async Task<ActionResult<PlagiarismStatisticsDto>> GetStatistics(
            [FromQuery] int? subjectId = null,
            [FromQuery] int? userId = null)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Sinh viên chỉ thấy thống kê của chính họ
            if (userRole == "Student")
            {
                userId = currentUserId;
            }

            var stats = await _plagiarismService.GetStatisticsAsync(subjectId, userId);
            return Ok(stats);
        }

        /// <summary>
        /// Lấy danh sách kiểm tra có tỷ lệ đạo văn cao (Cảnh báo nóng)
        /// </summary>
        [HttpGet("high-risk")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<PlagiarismCheckDto>>> GetHighRiskChecks(
            [FromQuery] decimal threshold = 50.0m,
            [FromQuery] int limit = 10)
        {
            var checks = await _plagiarismService.GetHighRiskChecksAsync(threshold, limit);
            return Ok(checks);
        }
    }
}
