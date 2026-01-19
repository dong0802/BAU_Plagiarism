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
        /// Calculate similarity between two texts (returns decimal 0-1)
        /// Used by PlagiarismService
        /// </summary>
        public decimal CalculateSimilarity(string text1, string text2)
        {
            var similarity = CalculateJaccardSimilarity(text1, text2, 3);
            return (decimal)(similarity / 100.0); // Convert to 0-1 range
        }

        public PlagiarismAnalysis AnalyzeDetailed(string newText, List<BAU_Plagiarism_System.Data.Models.Document> database)
        {
            // 1. Clean the document (Remove Bibliographies like Turnitin)
            string cleanedNewText = _processor.CleanDocument(newText);
            var segments = _processor.SplitIntoSmartSegments(cleanedNewText);
            
            var analysis = new PlagiarismAnalysis();
            
            // Prepare database documents (Clean them too)
            var cleanDb = database.Select(d => new {
                Doc = d,
                CleanContent = _processor.NormalizeText(_processor.CleanDocument(d.Content))
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

                totalWords += _processor.Tokenize(seg.CleanText).Count;

                var segmentAnalysis = new HighlightedSegment { Text = seg.RawText };
                double bestMatchScore = 0;
                string? bestSource = null;

                foreach (var dbDoc in cleanDb)
                {
                    // Check if the segment clean text is contained or highly similar
                    double score = CalculateJaccardSimilarity(seg.CleanText, dbDoc.CleanContent, 2);
                    
                    // Turnitin Logic: If a phrase of >7 words matches exactly, it's 100%
                    if (dbDoc.CleanContent.Contains(seg.CleanText) && seg.CleanText.Split(' ').Length >= 5)
                    {
                        score = 100;
                    }

                    if (score > bestMatchScore)
                    {
                        bestMatchScore = score;
                        bestSource = dbDoc.Doc.Title;
                        segmentAnalysis.MatchedText = seg.CleanText; // Simplified for now
                    }
                }

                segmentAnalysis.Score = Math.Round(bestMatchScore, 2);
                segmentAnalysis.Source = bestSource;
                segmentAnalysis.StartPosition = 0; // In a real app, track cursor position in raw text
                segmentAnalysis.EndPosition = seg.RawText.Length;
               
                if (bestMatchScore > 20) // Only count as matched words if similarity is significant
                {
                    matchedWords += _processor.Tokenize(seg.CleanText).Count;
                }

                analysis.Segments.Add(segmentAnalysis);
            }

            // Overall score calculation (Matched words / Total words)
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
