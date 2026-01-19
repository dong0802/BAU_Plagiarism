using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BAU_Plagiarism_System.Data.Models
{
    /// <summary>
    /// Bộ môn - Department (Bộ môn Ngân hàng thương mại, Bộ môn Tài chính doanh nghiệp, v.v.)
    /// </summary>
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty; // Mã bộ môn

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty; // Tên bộ môn

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int FacultyId { get; set; } // Thuộc khoa nào

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        [ForeignKey("FacultyId")]
        public virtual Faculty Faculty { get; set; } = null!;

        public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
