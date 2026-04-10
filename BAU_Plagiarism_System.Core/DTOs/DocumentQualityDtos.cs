using System;
using System.Collections.Generic;

namespace BAU_Plagiarism_System.Core.DTOs
{
    /// <summary>
    /// Các DTO phục vụ phân tích chất lượng tài liệu (Chấm điểm tự động)
    /// </summary>
    public class DocumentQualityAnalysisDto
    {
        public int DocumentId { get; set; }
        public double OverallQualityScore { get; set; } // 0-100
        public string QualityLevel { get; set; } = "Medium"; // Kém, Trung bình, Tốt, Rất tốt
        
        public FormatAnalysisDto FormatAnalysis { get; set; } = new();
        public ContentQualityDto ContentQuality { get; set; } = new();
        public List<QualityIssueDto> Issues { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        
        public DateTime AnalyzedDate { get; set; } = DateTime.Now;
    }

    public class FormatAnalysisDto
    {
        public bool HasProperStructure { get; set; }
        public int WordCount { get; set; }
        public int ParagraphCount { get; set; }
        public int SentenceCount { get; set; }
        
        public double AverageSentenceLength { get; set; }
        public double AverageParagraphLength { get; set; }
        
        // Các vấn đề định dạng
        public bool HasTitle { get; set; }
        public bool HasIntroduction { get; set; }
        public bool HasConclusion { get; set; }
        public bool HasReferences { get; set; }
        
        public int FormatScore { get; set; } // 0-100
    }

    public class ContentQualityDto
    {
        public double ReadabilityScore { get; set; } // 0-100
        public double CoherenceScore { get; set; } // 0-100
        public double VocabularyRichness { get; set; } // 0-100
        
        public int UniqueWords { get; set; }
        public int TotalWords { get; set; }
        public double LexicalDiversity { get; set; }
        
        public List<string> KeyPhrases { get; set; } = new();
        public List<string> AcademicTerms { get; set; } = new();
        
        public int ContentScore { get; set; } // 0-100
    }

    public class QualityIssueDto
    {
        public string IssueType { get; set; } = string.Empty; // Định dạng, Ngữ pháp, Cấu trúc, Nội dung
        public string Severity { get; set; } = "Medium"; // Low, Medium, High
        public string Description { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public int Position { get; set; }
    }

    public class WebSearchResultDto
    {
        public string Query { get; set; } = string.Empty;
        public List<WebSourceDto> Sources { get; set; } = new();
        public int TotalMatches { get; set; }
        public DateTime SearchDate { get; set; } = DateTime.Now;
    }

    public class WebSourceDto
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public string SourceType { get; set; } = "Web"; // Web, Wikipedia, Journal, News
    }
}
