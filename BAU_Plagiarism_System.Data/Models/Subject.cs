using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BAU_Plagiarism_System.Data.Models
{
    /// <summary>
    /// Môn học - Subject (Quản trị rủi ro, Ngân hàng thương mại, v.v.)
    /// </summary>
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // Mã môn học: "NH101", "TC201"

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty; // Tên môn học

        [StringLength(1000)]
        public string? Description { get; set; }

        public int Credits { get; set; } = 3; // Số tín chỉ

        [Required]
        public int DepartmentId { get; set; } // Thuộc bộ môn nào

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;

        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
