using BAU_Plagiarism_System.Core.DTOs;
using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BAU_Plagiarism_System.Core.Services
{
    /// <summary>
    /// Service quản lý người dùng (Giảng viên / Quản trị và Sinh viên)
    /// </summary>
    public class UserService
    {
        private readonly BAUDbContext _context;
        private readonly IEmailService _emailService;

        public UserService(BAUDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<List<UserDto>> GetAllUsersAsync(string? role = null)
        {
            var query = _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .Where(u => u.IsActive);

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            return await query
                .OrderBy(u => u.FullName)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    StudentId = u.StudentId,
                    LecturerId = u.LecturerId,
                    FacultyId = u.FacultyId,
                    FacultyName = u.Faculty != null ? u.Faculty.Name : null,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : null,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    LastLoginDate = u.LastLoginDate,
                    DailyCheckLimit = u.DailyCheckLimit,
                    ChecksUsedToday = u.ChecksUsedToday
                })
                .ToListAsync();
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                StudentId = user.StudentId,
                LecturerId = user.LecturerId,
                FacultyId = user.FacultyId,
                FacultyName = user.Faculty?.Name,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                DailyCheckLimit = user.DailyCheckLimit,
                ChecksUsedToday = user.ChecksUsedToday
            };
        }

        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                StudentId = user.StudentId,
                LecturerId = user.LecturerId,
                FacultyId = user.FacultyId,
                FacultyName = user.Faculty?.Name,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                DailyCheckLimit = user.DailyCheckLimit,
                ChecksUsedToday = user.ChecksUsedToday
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            // Find any user with same username or email
            var existingUserByUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower());
            if (existingUserByUsername != null && existingUserByUsername.IsActive)
                 throw new Exception("Tên đăng nhập đã tồn tại trên hệ thống.");

            var existingUserByEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (existingUserByEmail != null && existingUserByEmail.IsActive)
                throw new Exception("Email đã được sử dụng bởi một tài khoản khác.");

            // If we found an inactive user (either by email or username), reactivate and update them
            var targetUser = existingUserByEmail ?? existingUserByUsername;

            if (targetUser != null && !targetUser.IsActive)
            {
                targetUser.Username = dto.Username;
                targetUser.Email = dto.Email;
                targetUser.FullName = dto.FullName;
                targetUser.PasswordHash = HashPassword(dto.Password);
                targetUser.Role = dto.Role;
                targetUser.PhoneNumber = dto.PhoneNumber;
                targetUser.StudentId = dto.StudentId;
                targetUser.LecturerId = dto.LecturerId;
                targetUser.FacultyId = dto.FacultyId;
                targetUser.DepartmentId = dto.DepartmentId;
                targetUser.IsActive = true;
                targetUser.CreatedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                
                await _context.Entry(targetUser).Reference(u => u.Faculty).LoadAsync();
                await _context.Entry(targetUser).Reference(u => u.Department).LoadAsync();
                
                return MapToDto(targetUser);
            }

            // Otherwise create new
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = HashPassword(dto.Password),
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Role = dto.Role,
                StudentId = dto.StudentId,
                LecturerId = dto.LecturerId,
                FacultyId = dto.FacultyId,
                DepartmentId = dto.DepartmentId,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Load related entities
            await _context.Entry(user).Reference(u => u.Faculty).LoadAsync();
            await _context.Entry(user).Reference(u => u.Department).LoadAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                StudentId = user.StudentId,
                LecturerId = user.LecturerId,
                FacultyId = user.FacultyId,
                FacultyName = user.Faculty?.Name,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                DailyCheckLimit = user.DailyCheckLimit,
                ChecksUsedToday = user.ChecksUsedToday
            };
        }

        public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.FacultyId = dto.FacultyId;
            user.DepartmentId = dto.DepartmentId;
            user.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                StudentId = user.StudentId,
                LecturerId = user.LecturerId,
                FacultyId = user.FacultyId,
                FacultyName = user.Faculty?.Name,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                DailyCheckLimit = user.DailyCheckLimit,
                ChecksUsedToday = user.ChecksUsedToday
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Verify current password
            if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                return false;

            // Update password
            user.PasswordHash = HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ProcessForgotPasswordAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null) return false;

            // Generate 6-digit code
            var code = new Random().Next(100000, 999999).ToString();
            
            user.PasswordResetToken = code;
            user.PasswordResetTokenExpires = DateTime.Now.AddMinutes(15);

            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetCodeAsync(user.Email, code);
            return true;
        }

        public async Task<bool> VerifyAndResetPasswordAsync(ResetPasswordWithCodeDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.Email == dto.Email && 
                u.PasswordResetToken == dto.Code &&
                u.PasswordResetTokenExpires > DateTime.Now &&
                u.IsActive);

            if (user == null) return false;

            user.PasswordHash = HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;

            await _context.SaveChangesAsync();
            return true;
        }

        // ============= HELPER METHODS =============
        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                StudentId = user.StudentId,
                LecturerId = user.LecturerId,
                FacultyId = user.FacultyId,
                FacultyName = user.Faculty?.Name,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                DailyCheckLimit = user.DailyCheckLimit,
                ChecksUsedToday = user.ChecksUsedToday
            };
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }
    }
}
