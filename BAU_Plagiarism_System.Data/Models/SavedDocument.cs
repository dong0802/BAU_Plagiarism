using System;
using System.ComponentModel.DataAnnotations;

namespace BAU_Plagiarism_System.Data.Models
{
    public class SavedDocument
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Title { get; set; }
        
        [Required]
        public string Author { get; set; }
        
        [Required]
        public string Content { get; set; } // Raw text for comparison
        
        public DateTime UploadDate { get; set; }
        
        public string StudentId { get; set; }
        public string Department { get; set; } = "Banking Academy";
    }
}
