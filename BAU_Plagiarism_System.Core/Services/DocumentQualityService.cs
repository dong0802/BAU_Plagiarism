using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BAU_Plagiarism_System.Core.DTOs;

namespace BAU_Plagiarism_System.Core.Services
{
    /// <summary>
    /// Dịch vụ phân tích chất lượng tài liệu và cung cấp phản hồi chấm điểm tự động
    /// </summary>
    public class DocumentQualityService
    {
        private readonly TextProcessor _textProcessor;

        public DocumentQualityService(TextProcessor textProcessor)
        {
            _textProcessor = textProcessor;
        }

        public DocumentQualityAnalysisDto AnalyzeDocument(string content, string title = "")
        {
            var analysis = new DocumentQualityAnalysisDto();
            
            // 1. Phân tích định dạng
            analysis.FormatAnalysis = AnalyzeFormat(content, title);
            
            // 2. Phân tích chất lượng nội dung
            analysis.ContentQuality = AnalyzeContentQuality(content);
            
            // 3. Xác định các vấn đề
            analysis.Issues = IdentifyIssues(content, analysis.FormatAnalysis, analysis.ContentQuality);
            
            // 4. Tạo các gợi ý cải thiện
            analysis.Suggestions = GenerateSuggestions(analysis.Issues, analysis.FormatAnalysis, analysis.ContentQuality);
            
            // 5. Tính toán điểm tổng quan
            analysis.OverallQualityScore = CalculateOverallScore(analysis.FormatAnalysis, analysis.ContentQuality);
            analysis.QualityLevel = GetQualityLevel(analysis.OverallQualityScore);
            
            return analysis;
        }

        private FormatAnalysisDto AnalyzeFormat(string content, string title)
        {
            var format = new FormatAnalysisDto();
            
            // Các chỉ số cơ bản
            var sentences = SplitIntoSentences(content);
            var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var words = _textProcessor.Tokenize(content);
            
            format.SentenceCount = sentences.Count;
            format.ParagraphCount = paragraphs.Length;
            format.WordCount = words.Count;
            
            // Trung bình
            format.AverageSentenceLength = format.SentenceCount > 0 ? (double)format.WordCount / format.SentenceCount : 0;
            format.AverageParagraphLength = format.ParagraphCount > 0 ? (double)format.WordCount / format.ParagraphCount : 0;
            
            // Phát hiện cấu trúc
            format.HasTitle = !string.IsNullOrWhiteSpace(title);
            format.HasIntroduction = DetectIntroduction(content);
            format.HasConclusion = DetectConclusion(content);
            format.HasReferences = DetectReferences(content);
            
            // Tính toán điểm định dạng
            int score = 0;
            if (format.HasTitle) score += 15;
            if (format.HasIntroduction) score += 20;
            if (format.HasConclusion) score += 20;
            if (format.HasReferences) score += 15;
            if (format.ParagraphCount >= 3) score += 10;
            if (format.WordCount >= 500) score += 10;
            if (format.AverageSentenceLength >= 15 && format.AverageSentenceLength <= 25) score += 10;
            
            format.FormatScore = score;
            format.HasProperStructure = score >= 60;
            
            return format;
        }

        private ContentQualityDto AnalyzeContentQuality(string content)
        {
            var quality = new ContentQualityDto();
            
            var words = _textProcessor.Tokenize(content);
            var uniqueWords = words.Distinct().ToList();
            
            quality.TotalWords = words.Count;
            quality.UniqueWords = uniqueWords.Count;
            quality.LexicalDiversity = quality.TotalWords > 0 ? (double)quality.UniqueWords / quality.TotalWords : 0;
            
            // Độ phong phú vốn từ (0-100)
            quality.VocabularyRichness = Math.Min(100, quality.LexicalDiversity * 200);
            
            // Điểm mức độ dễ đọc (Flesch Reading Ease đơn giản hóa)
            quality.ReadabilityScore = CalculateReadabilityScore(content);
            
            // Điểm tính mạch lạc (dựa trên từ nối và luồng logic)
            quality.CoherenceScore = CalculateCoherenceScore(content);
            
            // Trích xuất các cụm từ khóa và thuật ngữ học thuật
            quality.KeyPhrases = ExtractKeyPhrases(content);
            quality.AcademicTerms = ExtractAcademicTerms(content);
            
            // Tính toán điểm nội dung
            quality.ContentScore = (int)((quality.ReadabilityScore + quality.CoherenceScore + quality.VocabularyRichness) / 3);
            
            return quality;
        }

        private List<QualityIssueDto> IdentifyIssues(string content, FormatAnalysisDto format, ContentQualityDto contentQuality)
        {
            var issues = new List<QualityIssueDto>();
            
            // Vấn đề về định dạng
            if (!format.HasTitle)
            {
                issues.Add(new QualityIssueDto
                {
                    IssueType = "Định dạng",
                    Severity = "High",
                    Description = "Tài liệu thiếu tiêu đề",
                    Suggestion = "Thêm tiêu đề rõ ràng cho bài viết"
                });
            }
            
            if (!format.HasIntroduction)
            {
                issues.Add(new QualityIssueDto
                {
                    IssueType = "Cấu trúc",
                    Severity = "High",
                    Description = "Thiếu phần mở bài",
                    Suggestion = "Thêm đoạn mở bài giới thiệu chủ đề và mục đích nghiên cứu"
                });
            }
            
            if (!format.HasConclusion)
            {
                issues.Add(new QualityIssueDto
                {
                    IssueType = "Cấu trúc",
                    Severity = "High",
                    Description = "Thiếu phần kết luận",
                    Suggestion = "Thêm đoạn kết luận tóm tắt nội dung và đưa ra nhận định"
                });
            }
            
            if (!format.HasReferences)
            {
                issues.Add(new QualityIssueDto
                {
                    IssueType = "Định dạng",
                    Severity = "Medium",
                    Description = "Thiếu danh mục tài liệu tham khảo",
                    Suggestion = "Bổ sung danh mục tài liệu tham khảo theo chuẩn APA hoặc Harvard"
                });
            }
            
            // Vấn đề về nội dung
            if (format.WordCount < 500)
            {
                issues.Add(new QualityIssueDto
                {
                    IssueType = "Nội dung",
                    Severity = "Medium",
                    Description = $"Bài viết quá ngắn ({format.WordCount} từ)",
                    Suggestion = "Mở rộng nội dung, bổ sung thêm phân tích và dẫn chứng"
                });
            }
            
            if (contentQuality.VocabularyRichness < 30)
            {
                issues.Add(new QualityIssueDto
                {
                    IssueType = "Nội dung",
                    Severity = "Medium",
                    Description = "Vốn từ vựng nghèo nàn, nhiều từ lặp lại",
                    Suggestion = "Sử dụng từ đồng nghĩa và đa dạng hóa cách diễn đạt"
                });
            }
            
            if (format.AverageSentenceLength < 10)
            {
                issues.Add(new QualityIssueDto
                {
                    IssueType = "Ngữ pháp",
                    Severity = "Low",
                    Description = "Câu văn quá ngắn, thiếu tính học thuật",
                    Suggestion = "Kết hợp các câu ngắn thành câu phức để tăng tính học thuật"
                });
            }
            
            if (format.AverageSentenceLength > 30)
            {
                issues.Add(new QualityIssueDto
                {
                    IssueType = "Ngữ pháp",
                    Severity = "Low",
                    Description = "Câu văn quá dài, khó đọc",
                    Suggestion = "Chia nhỏ các câu dài thành câu ngắn hơn để dễ hiểu"
                });
            }
            
            return issues;
        }

        private List<string> GenerateSuggestions(List<QualityIssueDto> issues, FormatAnalysisDto format, ContentQualityDto content)
        {
            var suggestions = new List<string>();
            
            // Gợi ý ưu tiên dựa trên các vấn đề
            var highSeverityIssues = issues.Where(i => i.Severity == "High").ToList();
            if (highSeverityIssues.Any())
            {
                suggestions.Add($"🔴 Ưu tiên: Khắc phục {highSeverityIssues.Count} vấn đề nghiêm trọng về cấu trúc");
            }
            
            // Gợi ý cụ thể
            if (format.WordCount < 1000)
            {
                suggestions.Add("📝 Mở rộng nội dung lên ít nhất 1000 từ để đạt chuẩn học thuật");
            }
            
            if (content.AcademicTerms.Count < 5)
            {
                suggestions.Add("📚 Sử dụng thêm thuật ngữ chuyên ngành để tăng tính học thuật");
            }
            
            if (content.CoherenceScore < 60)
            {
                suggestions.Add("🔗 Cải thiện tính mạch lạc bằng cách sử dụng từ nối (tuy nhiên, do đó, hơn nữa...)");
            }
            
            if (!format.HasReferences)
            {
                suggestions.Add("📖 Bổ sung ít nhất 5-10 tài liệu tham khảo uy tín");
            }
            
            return suggestions;
        }

        private double CalculateOverallScore(FormatAnalysisDto format, ContentQualityDto content)
        {
            // Trung bình có trọng số: Định dạng 40%, Nội dung 60%
            return (format.FormatScore * 0.4) + (content.ContentScore * 0.6);
        }

        private string GetQualityLevel(double score)
        {
            if (score >= 85) return "Rất tốt";
            if (score >= 70) return "Tốt";
            if (score >= 50) return "Trung bình";
            return "Kém";
        }

        // Các phương thức hỗ trợ
        private bool DetectIntroduction(string content)
        {
            var firstParagraph = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var introKeywords = new[] { "giới thiệu", "mở bài", "mở đầu", "trong bài viết này", "nghiên cứu này", "bài báo này" };
            return introKeywords.Any(k => firstParagraph.ToLower().Contains(k));
        }

        private bool DetectConclusion(string content)
        {
            var lastParagraph = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";
            var conclusionKeywords = new[] { "kết bài", "kết luận", "tóm lại", "như vậy", "qua đó", "tổng kết" };
            return conclusionKeywords.Any(k => lastParagraph.ToLower().Contains(k));
        }

        private bool DetectReferences(string content)
        {
            var refKeywords = new[] { "tài liệu tham khảo", "references", "bibliography", "nguồn:" };
            return refKeywords.Any(k => content.ToLower().Contains(k));
        }

        private double CalculateReadabilityScore(string content)
        {
            // Độ dễ đọc đơn giản hóa dựa trên độ dài câu và từ
            var sentences = SplitIntoSentences(content);
            if (sentences.Count == 0) return 0;
            
            var avgSentenceLength = content.Split(' ').Length / (double)sentences.Count;
            
            // Phạm vi lý tưởng: 15-25 từ mỗi câu
            if (avgSentenceLength >= 15 && avgSentenceLength <= 25)
                return 80;
            else if (avgSentenceLength >= 10 && avgSentenceLength <= 30)
                return 60;
            else
                return 40;
        }

        private double CalculateCoherenceScore(string content)
        {
            var transitionWords = new[] { "tuy nhiên", "do đó", "hơn nữa", "ngoài ra", "vì vậy", "mặt khác", "bên cạnh đó", "tóm lại" };
            int transitionCount = transitionWords.Sum(word => Regex.Matches(content.ToLower(), word).Count);
            
            var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            double transitionDensity = paragraphs.Length > 0 ? (double)transitionCount / paragraphs.Length : 0;
            
            return Math.Min(100, transitionDensity * 50);
        }

        private List<string> ExtractKeyPhrases(string content)
        {
            // Trích xuất cụm từ khóa đơn giản (có thể cải thiện bằng NLP)
            var words = _textProcessor.Tokenize(content);
            var wordFreq = words.GroupBy(w => w).OrderByDescending(g => g.Count()).Take(10);
            return wordFreq.Select(g => g.Key).ToList();
        }

        private List<string> ExtractAcademicTerms(string content)
        {
            var academicTerms = new[] { 
                "nghiên cứu", "phân tích", "đánh giá", "so sánh", "kết quả", 
                "phương pháp", "lý thuyết", "mô hình", "dữ liệu", "thống kê" 
            };
            
            return academicTerms.Where(term => content.ToLower().Contains(term)).ToList();
        }

        private List<string> SplitIntoSentences(string text)
        {
            return Regex.Split(text, @"(?<=[.!?])\s+")
                        .Where(s => s.Length > 10)
                        .ToList();
        }
    }
}
