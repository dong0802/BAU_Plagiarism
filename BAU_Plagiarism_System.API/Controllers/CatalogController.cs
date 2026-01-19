using BAU_Plagiarism_System.Core.DTOs;
using BAU_Plagiarism_System.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BAU_Plagiarism_System.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CatalogController : ControllerBase
    {
        private readonly CatalogService _catalogService;

        public CatalogController(CatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        // ============= FACULTY ENDPOINTS =============
        [HttpGet("faculties")]
        public async Task<ActionResult<List<FacultyDto>>> GetFaculties()
        {
            var faculties = await _catalogService.GetAllFacultiesAsync();
            return Ok(faculties);
        }

        [HttpGet("faculties/{id}")]
        public async Task<ActionResult<FacultyDto>> GetFaculty(int id)
        {
            var faculty = await _catalogService.GetFacultyByIdAsync(id);
            if (faculty == null)
                return NotFound();
            return Ok(faculty);
        }

        [HttpPost("faculties")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FacultyDto>> CreateFaculty([FromBody] CreateFacultyDto dto)
        {
            var faculty = await _catalogService.CreateFacultyAsync(dto);
            return CreatedAtAction(nameof(GetFaculty), new { id = faculty.Id }, faculty);
        }

        [HttpPut("faculties/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FacultyDto>> UpdateFaculty(int id, [FromBody] UpdateFacultyDto dto)
        {
            var faculty = await _catalogService.UpdateFacultyAsync(id, dto);
            if (faculty == null)
                return NotFound();
            return Ok(faculty);
        }

        [HttpDelete("faculties/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteFaculty(int id)
        {
            var result = await _catalogService.DeleteFacultyAsync(id);
            if (!result)
                return NotFound();
            return NoContent();
        }

        // ============= DEPARTMENT ENDPOINTS =============
        [HttpGet("departments")]
        public async Task<ActionResult<List<DepartmentDto>>> GetDepartments([FromQuery] int? facultyId = null)
        {
            var departments = await _catalogService.GetAllDepartmentsAsync(facultyId);
            return Ok(departments);
        }

        [HttpGet("departments/{id}")]
        public async Task<ActionResult<DepartmentDto>> GetDepartment(int id)
        {
            var department = await _catalogService.GetDepartmentByIdAsync(id);
            if (department == null)
                return NotFound();
            return Ok(department);
        }

        [HttpPost("departments")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DepartmentDto>> CreateDepartment([FromBody] CreateDepartmentDto dto)
        {
            var department = await _catalogService.CreateDepartmentAsync(dto);
            return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department);
        }

        [HttpPut("departments/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DepartmentDto>> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
        {
            var department = await _catalogService.UpdateDepartmentAsync(id, dto);
            if (department == null)
                return NotFound();
            return Ok(department);
        }

        [HttpDelete("departments/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteDepartment(int id)
        {
            var result = await _catalogService.DeleteDepartmentAsync(id);
            if (!result)
                return NotFound();
            return NoContent();
        }

        // ============= SUBJECT ENDPOINTS =============
        [HttpGet("subjects")]
        public async Task<ActionResult<List<SubjectDto>>> GetSubjects([FromQuery] int? departmentId = null)
        {
            var subjects = await _catalogService.GetAllSubjectsAsync(departmentId);
            return Ok(subjects);
        }

        [HttpGet("subjects/{id}")]
        public async Task<ActionResult<SubjectDto>> GetSubject(int id)
        {
            var subject = await _catalogService.GetSubjectByIdAsync(id);
            if (subject == null)
                return NotFound();
            return Ok(subject);
        }

        [HttpPost("subjects")]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<ActionResult<SubjectDto>> CreateSubject([FromBody] CreateSubjectDto dto)
        {
            var subject = await _catalogService.CreateSubjectAsync(dto);
            return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subject);
        }

        [HttpPut("subjects/{id}")]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<ActionResult<SubjectDto>> UpdateSubject(int id, [FromBody] UpdateSubjectDto dto)
        {
            var subject = await _catalogService.UpdateSubjectAsync(id, dto);
            if (subject == null)
                return NotFound();
            return Ok(subject);
        }

        [HttpDelete("subjects/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteSubject(int id)
        {
            var result = await _catalogService.DeleteSubjectAsync(id);
            if (!result)
                return NotFound();
            return NoContent();
        }
    }
}
