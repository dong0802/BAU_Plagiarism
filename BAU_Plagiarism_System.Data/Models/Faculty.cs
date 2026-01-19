using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BAU_Plagiarism_System.Data.Models
{
    /// <summary>
    /// Khoa - Faculty (Khoa Ngân hàng, Khoa Tài chính, v.v.)
    /// </summary>
    public class Faculty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty; // Mã khoa: "NH", "TC", "KT"

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty; // Tên khoa

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
