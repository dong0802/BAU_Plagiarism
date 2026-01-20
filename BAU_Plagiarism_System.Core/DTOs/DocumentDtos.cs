namespace BAU_Plagiarism_System.Core.DTOs
{
    // ============= Document DTOs =============
    public class DocumentDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public string? Semester { get; set; }
        public int? Year { get; set; }
        public DateTime UploadDate { get; set; }
        public bool IsPublic { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateDocumentDto
    {
        public string Title { get; set; } = string.Empty;
        public string DocumentType { get; set; } = "Essay";
        public int? SubjectId { get; set; }
        public string? Semester { get; set; }
        public int? Year { get; set; }
        public bool IsPublic { get; set; } = false;
    }

    public class UpdateDocumentDto
    {
        public string Title { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
        public string? Semester { get; set; }
        public int? Year { get; set; }
        public bool IsPublic { get; set; }
        public bool IsActive { get; set; }
    }

    public class DocumentUploadDto
    {
        public string Title { get; set; } = string.Empty;
        public string DocumentType { get; set; } = "Essay";
        public int? SubjectId { get; set; }
        public string? Semester { get; set; }
        public int? Year { get; set; }
        public bool IsPublic { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }
}
