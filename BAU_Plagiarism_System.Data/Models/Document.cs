using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BAU_Plagiarism_System.Data.Models
{
    /// <summary>
    /// Tài liệu - Document (Bài luận, đồ án tốt nghiệp, bài nghiên cứu)
    /// </summary>
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty; // Tiêu đề tài liệu

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = "Essay"; // "Essay", "Thesis", "Research"

        [Required]
        public string Content { get; set; } = string.Empty; // Nội dung văn bản đã trích xuất

        [StringLength(500)]
        public string? OriginalFileName { get; set; } // Tên file gốc

        [StringLength(500)]
        public string? FilePath { get; set; } // Đường dẫn lưu file

        public long FileSize { get; set; } // Kích thước file (bytes)

        [Required]
        public int UserId { get; set; } // Người upload

        public int? SubjectId { get; set; } // Thuộc môn học nào

        [StringLength(50)]
        public string? Semester { get; set; } // Học kỳ: "HK1-2024", "HK2-2024"

        public int? Year { get; set; } // Năm học: 2024

        public DateTime UploadDate { get; set; } = DateTime.Now;

        public bool IsPublic { get; set; } = false; // Có được dùng làm tài liệu tham chiếu không

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        public virtual ICollection<PlagiarismCheck> PlagiarismChecks { get; set; } = new List<PlagiarismCheck>();
    }
}
