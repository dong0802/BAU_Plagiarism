using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BAU_Plagiarism_System.Data.Models
{
    /// <summary>
    /// Người dùng - User (Giảng viên / Quản trị và Sinh viên)
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty; // Tên đăng nhập

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty; // Mật khẩu đã hash

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty; // Họ và tên   

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Student"; // "Student", "Admin"

        [StringLength(50)]
        public string? StudentId { get; set; } // Mã sinh viên (nếu là sinh viên)

        [StringLength(50)]
        public string? LecturerId { get; set; } // Mã giảng viên (nếu có - dành cho Admin là giảng viên)

        public int? FacultyId { get; set; } // Thuộc khoa nào

        public int? DepartmentId { get; set; } // Thuộc bộ môn nào (với giảng viên)

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastLoginDate { get; set; }

        [StringLength(10)]
        public string? PasswordResetToken { get; set; } // Token/Code để quên mật khẩu

        public DateTime? PasswordResetTokenExpires { get; set; } // Thời gian hết hạn token

        // Daily Check Limit System (Credits)
        public int DailyCheckLimit { get; set; } = 5; // Số lượt kiểm tra tối đa mỗi ngày (mặc định 5)
        public int ChecksUsedToday { get; set; } = 0; // Số lượt đã sử dụng hôm nay
        public DateTime? LastCheckResetDate { get; set; } // Ngày reset lần cuối

        // Navigation Properties
        [ForeignKey("FacultyId")]
        public virtual Faculty? Faculty { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<PlagiarismCheck> PlagiarismChecks { get; set; } = new List<PlagiarismCheck>();
    }
}
