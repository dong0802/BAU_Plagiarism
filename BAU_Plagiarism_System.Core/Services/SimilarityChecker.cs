using System;
using System.Collections.Generic;
using System.Linq;

namespace BAU_Plagiarism_System.Core.Services
{
    public class HighlightedSegment
    {
        public string Text { get; set; } = "";
        public string? MatchedText { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public double Score { get; set; }
        public string? Source { get; set; }
        public int? SourceId { get; set; } // ID of the matching document
        public bool IsExcluded { get; set; }
        public string? ExclusionReason { get; set; }
    }

    public class PlagiarismAnalysis
    {
        public double OverallScore { get; set; }
        public List<HighlightedSegment> Segments { get; set; } = new();
    }

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
            var union = set1.Count + set2.Count - intersection;

            return (double)intersection / union * 100;
        }

        public decimal CalculateSimilarity(string text1, string text2)
        {
            var similarity = CalculateJaccardSimilarity(text1, text2, 3);
            return (decimal)(similarity / 100.0);
        }

        /// <summary>
        /// Giới hạn kích thước nội dung so sánh để tránh tràn bộ nhớ
        /// </summary>
        private const int MAX_CONTENT_LENGTH = 50000; // 50K ký tự tối đa mỗi tài liệu

        public PlagiarismAnalysis AnalyzeDetailed(string newText, List<BAU_Plagiarism_System.Data.Models.Document> database)
        {
            if (string.IsNullOrWhiteSpace(newText))
                return new PlagiarismAnalysis();

            string cleanedNewText = _processor.CleanDocument(newText);
            var sourceSegments = _processor.SplitIntoSmartSegments(cleanedNewText);
            
            var analysis = new PlagiarismAnalysis 
            { 
                Segments = sourceSegments.Select(s => new HighlightedSegment 
                { 
                    Text = s.RawText,
                    IsExcluded = s.IsExcluded,
                    ExclusionReason = s.ExclusionReason
                }).ToList() 
            };

            CompareAgainstBatch(analysis, database);

            // Re-calculate overall score after one batch (though usually called for multiple batches)
            CalculateOverallScore(analysis);

            return analysis;
        }

        public void CompareAgainstBatch(PlagiarismAnalysis analysis, List<BAU_Plagiarism_System.Data.Models.Document> database)
        {
            if (database == null || !database.Any()) return;

            // 1. Chuẩn bị dữ liệu phẳng (Flat Data) - Sử dụng HashSet<int> để cực kỳ tiết kiệm bộ nhớ
            var cleanDb = database
                .Where(d => !string.IsNullOrEmpty(d.Content))
                .Select(d => {
                    var content = d.Content.Length > MAX_CONTENT_LENGTH 
                        ? d.Content.Substring(0, MAX_CONTENT_LENGTH) 
                        : d.Content;
                    var cleanContent = _processor.NormalizeText(_processor.CleanDocument(content));
                    var nGrams = _processor.GenerateHashedNGrams(cleanContent, 4);
                    return new {
                        Id = d.Id,
                        Title = d.Title,
                        CleanContent = cleanContent,
                        NGrams = nGrams
                    };
                }).ToList();

            // 2. Chuẩn bị N-grams cho các segment nguồn (Sử dụng HashSet<int>)
            var preparedSourceSegments = new List<(HighlightedSegment info, HashSet<int> grams, string cleanText)>();
            foreach (var seg in analysis.Segments)
            {
                if (seg.IsExcluded || string.IsNullOrWhiteSpace(seg.Text)) continue;
                
                var cleanText = _processor.NormalizeText(seg.Text);
                var segmentNGrams = _processor.GenerateHashedNGrams(cleanText, 4);
                preparedSourceSegments.Add((seg, segmentNGrams, cleanText));
            }

            // 3. So sánh hiệu năng cao
            foreach (var source in preparedSourceSegments)
            {
                double bestScore = source.info.Score;
                string? bestSource = source.info.Source;
                int? bestSourceId = source.info.SourceId;
                string? bestMatchedText = source.info.MatchedText;

                foreach (var dbDoc in cleanDb)
                {
                    // Case 1: Exact match (Sử dụng bản đã chuẩn hóa để bỏ qua dấu và ký tự đặc biệt)
                    if (source.cleanText.Length > 20 && dbDoc.CleanContent.Contains(source.cleanText))
                    {
                        if (100 > bestScore)
                        {
                            bestScore = 100;
                            bestSource = dbDoc.Title;
                            bestSourceId = dbDoc.Id;
                            bestMatchedText = source.info.Text;
                        }
                        continue;
                    }

                    // Case 2: Fuzzy match using Containment similarity (Overlap Coefficient)
                    // Jaccard similarity (intersection/union) không phù hợp khi so sánh một đoạn văn ngắn với một tài liệu lớn
                    if (source.grams.Count > 0 && dbDoc.NGrams.Count > 0)
                    {
                        // Đếm số lượng N-grams trùng lặp
                        int intersection = 0;
                        foreach (var gram in source.grams)
                        {
                            if (dbDoc.NGrams.Contains(gram)) intersection++;
                        }

                        // Score = (số lượng trùng lặp / tổng số n-grams của đoạn nguồn) * 100
                        double score = (double)intersection / source.grams.Count * 100;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestSource = dbDoc.Title;
                            bestSourceId = dbDoc.Id;
                            bestMatchedText = source.info.Text;
                        }
                    }
                }

                source.info.Score = Math.Round(bestScore, 2);
                source.info.Source = bestSource;
                source.info.SourceId = bestSourceId;
                source.info.MatchedText = bestMatchedText;
            }
            
            // Dọn dẹp bộ nhớ ngay lập tức
            cleanDb.Clear();
            preparedSourceSegments.Clear();
        }

        public void CalculateOverallScore(PlagiarismAnalysis analysis)
        {
            int totalWords = 0;
            int matchedWords = 0;

            foreach (var seg in analysis.Segments)
            {
                if (seg.IsExcluded || string.IsNullOrWhiteSpace(seg.Text)) continue;

                var words = seg.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                totalWords += words;

                if (seg.Score > 40)
                {
                    matchedWords += words;
                }
            }

            analysis.OverallScore = totalWords > 0 ? Math.Round((double)matchedWords / totalWords * 100, 2) : 0;
        }
    }
}
