using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BAU_Plagiarism_System.Data.Models
{
    /// <summary>
    /// Kết quả kiểm tra đạo văn - Plagiarism Check Result
    /// </summary>
    public class PlagiarismCheck
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SourceDocumentId { get; set; } // Tài liệu được kiểm tra

        [Required]
        public int UserId { get; set; } // Người thực hiện kiểm tra

        public DateTime CheckDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(5,2)")]
        public decimal OverallSimilarityPercentage { get; set; } // Tỷ lệ tương đồng tổng thể (%)

        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // "Processing", "Completed", "Failed"

        public int TotalMatchedDocuments { get; set; } // Số tài liệu trùng khớp

        [StringLength(1000)]
        public string? Notes { get; set; }

        // AI Detection Fields
        [Column(TypeName = "decimal(5,2)")]
        public decimal? AiProbability { get; set; } // Tỷ lệ nghi ngờ AI (%)

        [StringLength(50)]
        public string? AiDetectionLevel { get; set; } // Low, Medium, High

        public string? AiDetectionJson { get; set; } // Dữ liệu chi tiết về AI detection

        // Navigation Properties
        [ForeignKey("SourceDocumentId")]
        public virtual Document SourceDocument { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<PlagiarismMatch> Matches { get; set; } = new List<PlagiarismMatch>();
    }

    /// <summary>
    /// Chi tiết các đoạn văn trùng lặp
    /// </summary>
    public class PlagiarismMatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlagiarismCheckId { get; set; }

        [Required]
        public int MatchedDocumentId { get; set; } // Tài liệu bị trùng

        [Required]
        public string MatchedText { get; set; } = string.Empty; // Đoạn văn trùng

        public int StartPosition { get; set; } // Vị trí bắt đầu trong tài liệu gốc

        public int EndPosition { get; set; } // Vị trí kết thúc

        [Column(TypeName = "decimal(5,2)")]
        public decimal SimilarityScore { get; set; } // Điểm tương đồng của đoạn này (%)

        // Navigation Properties
        [ForeignKey("PlagiarismCheckId")]
        public virtual PlagiarismCheck PlagiarismCheck { get; set; } = null!;

        [ForeignKey("MatchedDocumentId")]
        public virtual Document MatchedDocument { get; set; } = null!;
    }
}
