namespace BAU_Plagiarism_System.Core.DTOs
{
    // ============= User DTOs =============
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public string? LecturerId { get; set; }
        public int? FacultyId { get; set; }
        public string? FacultyName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        
        // Daily Check Limit (Credits System)
        public int DailyCheckLimit { get; set; }
        public int ChecksUsedToday { get; set; }
        public int RemainingChecksToday => DailyCheckLimit - ChecksUsedToday;
    }

    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "Student";
        public string? StudentId { get; set; }
        public string? LecturerId { get; set; }
        public int? FacultyId { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class UpdateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int? FacultyId { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; }
    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
