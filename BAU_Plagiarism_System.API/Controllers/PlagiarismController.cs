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

        public PlagiarismController(PlagiarismService plagiarismService)
        {
            _plagiarismService = plagiarismService;
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

        /// <summary>
        /// Lấy lịch sử kiểm tra đạo văn
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<List<PlagiarismCheckDto>>> GetHistory(
            [FromQuery] int? userId = null,
            [FromQuery] int? documentId = null,
            [FromQuery] int? limit = null)
        {
            // If not admin, only show own history
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && userRole != "Lecturer")
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

            // Authorization: Only owner, lecturer, or admin can view
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && userRole != "Lecturer" && check.UserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(check);
        }

        /// <summary>
        /// Lấy thống kê đạo văn
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<ActionResult<PlagiarismStatisticsDto>> GetStatistics(
            [FromQuery] int? subjectId = null,
            [FromQuery] int? userId = null)
        {
            var stats = await _plagiarismService.GetStatisticsAsync(subjectId, userId);
            return Ok(stats);
        }
    }
}
