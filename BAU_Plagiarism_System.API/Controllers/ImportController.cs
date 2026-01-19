using BAU_Plagiarism_System.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BAU_Plagiarism_System.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ImportController : ControllerBase
    {
        private readonly ImportService _importService;

        public ImportController(ImportService importService)
        {
            _importService = importService;
        }

        /// <summary>
        /// Nhập hàng loạt tài liệu từ một thư mục trên máy chủ
        /// </summary>
        [HttpPost("bulk-folder")]
        public async Task<ActionResult<ImportResult>> ImportFromFolder([FromBody] ImportRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _importService.ImportFromFolderAsync(request.FolderPath, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class ImportRequest
    {
        public string FolderPath { get; set; } = string.Empty;
    }
}
