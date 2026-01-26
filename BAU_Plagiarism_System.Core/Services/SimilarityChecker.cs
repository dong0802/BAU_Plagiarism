using System;
using System.Collections.Generic;
using System.Linq;

namespace BAU_Plagiarism_System.Core.Services
{
    public class SimilarityChecker
    {
        private readonly TextProcessor _processor;

        public SimilarityChecker()
        {
            _processor = new TextProcessor();
        }

        public double CalculateJaccardSimilarity(string text1, string text2, int nGramSize = 3)
        {
            var set1 = _processor.GenerateNGrams(text1, nGramSize);
            var set2 = _processor.GenerateNGrams(text2, nGramSize);

            if (!set1.Any() || !set2.Any()) return 0;

            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();

            return (double)intersection / union * 100;
        }

        /// <summary>
        /// Tính toán độ tương đồng giữa hai văn bản (trả về giá trị thập phân 0-1)
        /// Được sử dụng bởi PlagiarismService
        /// </summary>
        public decimal CalculateSimilarity(string text1, string text2)
        {
            var similarity = CalculateJaccardSimilarity(text1, text2, 3);
            return (decimal)(similarity / 100.0); // Chuyển đổi sang phạm vi 0-1
        }

        public PlagiarismAnalysis AnalyzeDetailed(string newText, List<BAU_Plagiarism_System.Data.Models.Document> database)
        {
            // 1. Làm sạch tài liệu (Loại bỏ danh mục tham khảo giống như Turnitin)
            string cleanedNewText = _processor.CleanDocument(newText);
            var segments = _processor.SplitIntoSmartSegments(cleanedNewText);
            
            var analysis = new PlagiarismAnalysis();
            
            // Chuẩn bị các tài liệu trong cơ sở dữ liệu (Làm sạch và tính toán trước NGrams)
            var cleanDb = database.Select(d => {
                var cleanContent = _processor.NormalizeText(_processor.CleanDocument(d.Content));
                return new {
                    Doc = d,
                    CleanContent = cleanContent,
                    NGrams = _processor.GenerateNGrams(cleanContent, 2) // Tính toán trước NGrams cho toàn bộ tài liệu
                };
            }).ToList();

            int matchedWords = 0;
            int totalWords = 0;

            foreach (var seg in segments)
            {
                if (seg.IsNoise || seg.IsExcluded)
                {
                    analysis.Segments.Add(new HighlightedSegment { 
                        Text = seg.RawText, 
                        Score = 0, 
                        IsExcluded = seg.IsExcluded, 
                        ExclusionReason = seg.ExclusionReason 
                    });
                    continue;
                }

                var segmentCleanText = seg.CleanText;
                var segmentTokens = _processor.Tokenize(segmentCleanText);
                totalWords += segmentTokens.Count;

                var segmentNGrams = _processor.GenerateNGrams(segmentCleanText, 2);
                var segmentAnalysis = new HighlightedSegment { Text = seg.RawText };
                double bestMatchScore = 0;
                string? bestSource = null;

                foreach (var dbDoc in cleanDb)
                {
                    // Đường dẫn nhanh: Kiểm tra xem phân đoạn có được chứa chính xác hay không (Không phân biệt hoa thường do chuẩn hóa)
                    if (segmentTokens.Count >= 5 && dbDoc.CleanContent.Contains(segmentCleanText))
                    {
                        bestMatchScore = 100;
                        bestSource = dbDoc.Doc.Title;
                        segmentAnalysis.MatchedText = segmentCleanText;
                        break; // Đã tìm thấy khớp 100%, có thể dừng cho tài liệu này
                    }

                    // Nếu không, tính toán độ tương đồng Jaccard bằng cách sử dụng NGrams của tài liệu đã tính toán trước
                    if (segmentNGrams.Any() && dbDoc.NGrams.Any())
                    {
                        var intersection = segmentNGrams.Intersect(dbDoc.NGrams).Count();
                        var union = segmentNGrams.Count + dbDoc.NGrams.Count - intersection;
                        double score = (double)intersection / union * 100;

                        if (score > bestMatchScore)
                        {
                            bestMatchScore = score;
                            bestSource = dbDoc.Doc.Title;
                            segmentAnalysis.MatchedText = segmentCleanText; // Đơn giản hóa
                        }
                    }
                }

                segmentAnalysis.Score = Math.Round(bestMatchScore, 2);
                segmentAnalysis.Source = bestSource;
                segmentAnalysis.StartPosition = 0; 
                segmentAnalysis.EndPosition = seg.RawText.Length;
               
                if (bestMatchScore > 20) 
                {
                    matchedWords += segmentTokens.Count;
                }

                analysis.Segments.Add(segmentAnalysis);
            }

            // Tính toán tổng số điểm (Số từ khớp / Tổng số từ)
            analysis.OverallScore = totalWords > 0 ? Math.Round((double)matchedWords / totalWords * 100, 2) : 0;
            
            return analysis;
        }
    }

    public class PlagiarismAnalysis
    {
        public double OverallScore { get; set; }
        public List<HighlightedSegment> Segments { get; set; } = new();
    }

    public class HighlightedSegment
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
}
