using BAU_Plagiarism_System.Core.DTOs;
using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BAU_Plagiarism_System.Core.Services
{
    /// <summary>
    /// Service quản lý Danh mục (Khoa, Bộ môn, Môn học)
    /// </summary>
    public class CatalogService
    {
        private readonly BAUDbContext _context;

        public CatalogService(BAUDbContext context)
        {
            _context = context;
        }

        // ============= FACULTY MANAGEMENT =============
        public async Task<List<FacultyDto>> GetAllFacultiesAsync()
        {
            return await _context.Faculties
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .Select(f => new FacultyDto
                {
                    Id = f.Id,
                    Code = f.Code,
                    Name = f.Name,
                    Description = f.Description,
                    IsActive = f.IsActive,
                    CreatedDate = f.CreatedDate
                })
                .ToListAsync();
        }

        public async Task<FacultyDto?> GetFacultyByIdAsync(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return null;

            return new FacultyDto
            {
                Id = faculty.Id,
                Code = faculty.Code,
                Name = faculty.Name,
                Description = faculty.Description,
                IsActive = faculty.IsActive,
                CreatedDate = faculty.CreatedDate
            };
        }

        public async Task<FacultyDto> CreateFacultyAsync(CreateFacultyDto dto)
        {
            var faculty = new Faculty
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                CreatedDate = DateTime.Now
            };

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            return new FacultyDto
            {
                Id = faculty.Id,
                Code = faculty.Code,
                Name = faculty.Name,
                Description = faculty.Description,
                IsActive = faculty.IsActive,
                CreatedDate = faculty.CreatedDate
            };
        }

        public async Task<FacultyDto?> UpdateFacultyAsync(int id, UpdateFacultyDto dto)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return null;

            faculty.Code = dto.Code;
            faculty.Name = dto.Name;
            faculty.Description = dto.Description;
            faculty.IsActive = dto.IsActive;
            faculty.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return new FacultyDto
            {
                Id = faculty.Id,
                Code = faculty.Code,
                Name = faculty.Name,
                Description = faculty.Description,
                IsActive = faculty.IsActive,
                CreatedDate = faculty.CreatedDate
            };
        }

        public async Task<bool> DeleteFacultyAsync(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return false;

            faculty.IsActive = false;
            faculty.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // ============= DEPARTMENT MANAGEMENT =============
        public async Task<List<DepartmentDto>> GetAllDepartmentsAsync(int? facultyId = null)
        {
            var query = _context.Departments
                .Include(d => d.Faculty)
                .Where(d => d.IsActive);

            if (facultyId.HasValue)
                query = query.Where(d => d.FacultyId == facultyId.Value);

            return await query
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    FacultyId = d.FacultyId,
                    FacultyName = d.Faculty.Name,
                    IsActive = d.IsActive,
                    CreatedDate = d.CreatedDate
                })
                .ToListAsync();
        }

        public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Faculty)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null) return null;

            return new DepartmentDto
            {
                Id = department.Id,
                Code = department.Code,
                Name = department.Name,
                Description = department.Description,
                FacultyId = department.FacultyId,
                FacultyName = department.Faculty.Name,
                IsActive = department.IsActive,
                CreatedDate = department.CreatedDate
            };
        }

        public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto)
        {
            var department = new Department
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                FacultyId = dto.FacultyId,
                CreatedDate = DateTime.Now
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            var faculty = await _context.Faculties.FindAsync(dto.FacultyId);

            return new DepartmentDto
            {
                Id = department.Id,
                Code = department.Code,
                Name = department.Name,
                Description = department.Description,
                FacultyId = department.FacultyId,
                FacultyName = faculty?.Name ?? "",
                IsActive = department.IsActive,
                CreatedDate = department.CreatedDate
            };
        }

        public async Task<DepartmentDto?> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto)
        {
            var department = await _context.Departments
                .Include(d => d.Faculty)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null) return null;

            department.Code = dto.Code;
            department.Name = dto.Name;
            department.Description = dto.Description;
            department.FacultyId = dto.FacultyId;
            department.IsActive = dto.IsActive;
            department.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return new DepartmentDto
            {
                Id = department.Id,
                Code = department.Code,
                Name = department.Name,
                Description = department.Description,
                FacultyId = department.FacultyId,
                FacultyName = department.Faculty.Name,
                IsActive = department.IsActive,
                CreatedDate = department.CreatedDate
            };
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return false;

            department.IsActive = false;
            department.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // ============= SUBJECT MANAGEMENT =============
        public async Task<List<SubjectDto>> GetAllSubjectsAsync(int? departmentId = null)
        {
            var query = _context.Subjects
                .Include(s => s.Department)
                .Where(s => s.IsActive);

            if (departmentId.HasValue)
                query = query.Where(s => s.DepartmentId == departmentId.Value);

            return await query
                .OrderBy(s => s.Name)
                .Select(s => new SubjectDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Description = s.Description,
                    Credits = s.Credits,
                    DepartmentId = s.DepartmentId,
                    DepartmentName = s.Department.Name,
                    IsActive = s.IsActive,
                    CreatedDate = s.CreatedDate
                })
                .ToListAsync();
        }

        public async Task<SubjectDto?> GetSubjectByIdAsync(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return null;

            return new SubjectDto
            {
                Id = subject.Id,
                Code = subject.Code,
                Name = subject.Name,
                Description = subject.Description,
                Credits = subject.Credits,
                DepartmentId = subject.DepartmentId,
                DepartmentName = subject.Department.Name,
                IsActive = subject.IsActive,
                CreatedDate = subject.CreatedDate
            };
        }

        public async Task<SubjectDto> CreateSubjectAsync(CreateSubjectDto dto)
        {
            var subject = new Subject
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                Credits = dto.Credits,
                DepartmentId = dto.DepartmentId,
                CreatedDate = DateTime.Now
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            var department = await _context.Departments.FindAsync(dto.DepartmentId);

            return new SubjectDto
            {
                Id = subject.Id,
                Code = subject.Code,
                Name = subject.Name,
                Description = subject.Description,
                Credits = subject.Credits,
                DepartmentId = subject.DepartmentId,
                DepartmentName = department?.Name ?? "",
                IsActive = subject.IsActive,
                CreatedDate = subject.CreatedDate
            };
        }

        public async Task<SubjectDto?> UpdateSubjectAsync(int id, UpdateSubjectDto dto)
        {
            var subject = await _context.Subjects
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return null;

            subject.Code = dto.Code;
            subject.Name = dto.Name;
            subject.Description = dto.Description;
            subject.Credits = dto.Credits;
            subject.DepartmentId = dto.DepartmentId;
            subject.IsActive = dto.IsActive;
            subject.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return new SubjectDto
            {
                Id = subject.Id,
                Code = subject.Code,
                Name = subject.Name,
                Description = subject.Description,
                Credits = subject.Credits,
                DepartmentId = subject.DepartmentId,
                DepartmentName = subject.Department.Name,
                IsActive = subject.IsActive,
                CreatedDate = subject.CreatedDate
            };
        }

        public async Task<bool> DeleteSubjectAsync(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return false;

            subject.IsActive = false;
            subject.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
