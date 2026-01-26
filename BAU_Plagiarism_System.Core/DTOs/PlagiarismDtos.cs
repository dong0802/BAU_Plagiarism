namespace BAU_Plagiarism_System.Core.DTOs
{
    // ============= Plagiarism Check DTOs =============
    public class PlagiarismCheckDto
    {
        public int Id { get; set; }
        public int SourceDocumentId { get; set; }
        public string SourceDocumentTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CheckDate { get; set; }
        public decimal OverallSimilarityPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalMatchedDocuments { get; set; }
        public string? Notes { get; set; }
        public List<PlagiarismMatchDto> Matches { get; set; } = new();
        public DetailedAnalysisDto? DetailedAnalysis { get; set; }
        public decimal? AiProbability { get; set; }
        public string? AiDetectionLevel { get; set; }
        public AiDetectionResultDto? AiAnalysis { get; set; }
    }

    public class PlagiarismMatchDto
    {
        public int Id { get; set; }
        public int MatchedDocumentId { get; set; }
        public string MatchedDocumentTitle { get; set; } = string.Empty;
        public string MatchedText { get; set; } = string.Empty;
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public decimal SimilarityScore { get; set; }
    }

    public class CreatePlagiarismCheckDto
    {
        public int SourceDocumentId { get; set; }
        public string? Notes { get; set; }
    }

    public class PlagiarismCheckResultDto
    {
        public int CheckId { get; set; }
        public decimal OverallSimilarityPercentage { get; set; }
        public int TotalMatchedDocuments { get; set; }
        public List<MatchDetailDto> MatchDetails { get; set; } = new();
        public DetailedAnalysisDto? DetailedAnalysis { get; set; }
        public string Status { get; set; } = "Completed";
        public DateTime CheckDate { get; set; }
        public decimal? AiProbability { get; set; }
        public string? AiDetectionLevel { get; set; }
        public AiDetectionResultDto? AiAnalysis { get; set; }

        // Daily limits info
        public int RemainingChecksToday { get; set; }
        public int DailyCheckLimit { get; set; }
    }

    public class MatchDetailDto
    {
        public int MatchedDocumentId { get; set; }
        public string MatchedDocumentTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal SimilarityPercentage { get; set; }
        public List<TextMatchDto> TextMatches { get; set; } = new();
    }

    public class TextMatchDto
    {
        public string OriginalText { get; set; } = string.Empty;
        public string MatchedText { get; set; } = string.Empty;
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public decimal Score { get; set; }
    }

    public class DetailedAnalysisDto
    {
        public double OverallScore { get; set; }
        public List<SegmentDto> Segments { get; set; } = new();
    }

    public class SegmentDto
    {
        public string Text { get; set; } = "";
        public string? MatchedText { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public double Score { get; set; }
        public string? Source { get; set; }
        public bool IsExcluded { get; set; }
        public string? ExclusionReason { get; set; }
    }

    // ============= Statistics DTOs =============
    public class PlagiarismStatisticsDto
    {
        public int TotalChecks { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalUsers { get; set; }
        public int TotalFaculties { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalStudents { get; set; }
        public decimal AverageSimilarity { get; set; }
        public int HighRiskCount { get; set; } // > 30%
        public int MediumRiskCount { get; set; } // 15-30%
        public int LowRiskCount { get; set; } // < 15%
        public List<SubjectStatDto> SubjectStats { get; set; } = new();
    }

    public class SubjectStatDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int DocumentCount { get; set; }
        public decimal AverageSimilarity { get; set; }
    }
}
