using BAU_Plagiarism_System.Core.DTOs;
using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BAU_Plagiarism_System.Core.Services
{
    /// <summary>
    /// Service kiểm tra đạo văn - NLP Engine
    /// </summary>
    public class PlagiarismService
    {
        private readonly BAUDbContext _context;
        private readonly TextProcessor _textProcessor;
        private readonly SimilarityChecker _similarityChecker;

        public PlagiarismService(BAUDbContext context, TextProcessor textProcessor, SimilarityChecker similarityChecker)
        {
            _context = context;
            _textProcessor = textProcessor;
            _similarityChecker = similarityChecker;
        }

        /// <summary>
        /// Kiểm tra đạo văn cho một tài liệu
        /// </summary>
        public async Task<PlagiarismCheckResultDto> CheckPlagiarismAsync(int userId, CreatePlagiarismCheckDto dto)
        {
            // Get source document
            var sourceDoc = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == dto.SourceDocumentId && d.IsActive);

            if (sourceDoc == null)
                throw new Exception("Source document not found");

            // Create plagiarism check record
            var plagiarismCheck = new PlagiarismCheck
            {
                SourceDocumentId = dto.SourceDocumentId,
                UserId = userId,
                CheckDate = DateTime.Now,
                Status = "Processing"
            };

            _context.PlagiarismChecks.Add(plagiarismCheck);
            await _context.SaveChangesAsync();

            try
            {
                // Preprocess source text
                var processedSource = _textProcessor.Process(sourceDoc.Content);
                // var processedSource = _textProcessor.Process(sourceDoc.Content); // This is no longer needed directly here

                // Get all public documents for comparison (excluding the source itself)
                var compareDocs = await _context.Documents
                    .Where(d => d.IsActive && d.IsPublic && d.Id != dto.SourceDocumentId)
                    .ToListAsync();

                // Detailed Segment Analysis
                var detailedResult = _similarityChecker.AnalyzeDetailed(sourceDoc.Content, compareDocs);
                
                var detailedDto = new DetailedAnalysisDto
                {
                    OverallScore = detailedResult.OverallScore,
                    Segments = detailedResult.Segments.Select(s => new SegmentDto
                    {
                        Text = s.Text,
                        MatchedText = s.MatchedText,
                        StartPosition = s.StartPosition,
                        EndPosition = s.EndPosition,
                        Score = s.Score,
                        Source = s.Source,
                        IsExcluded = s.IsExcluded,
                        ExclusionReason = s.ExclusionReason
                    }).ToList()
                };

                var matches = new List<PlagiarismMatch>();
                var matchDetails = new List<MatchDetailDto>();

                // Add to match details (Optional: can keep existing sentences logic or just use segments)
                foreach(var seg in detailedResult.Segments.Where(s => s.Score > 20 && !string.IsNullOrEmpty(s.Source)))
                {
                    // Find the matched document by title (Source in DetailedAnalysisDto is the document title)
                    var matchedDocument = compareDocs.FirstOrDefault(d => d.Title == seg.Source);
                    string authorName = "Unknown";
                    if (matchedDocument != null)
                    {
                        var matchedAuthor = await _context.Users.FindAsync(matchedDocument.UserId);
                        authorName = matchedAuthor?.FullName ?? "Unknown";
                    }

                    var existingDetail = matchDetails.FirstOrDefault(m => m.MatchedDocumentTitle == seg.Source);
                    if (existingDetail != null)
                    {
                        existingDetail.TextMatches.Add(new TextMatchDto {
                            OriginalText = seg.Text,
                            MatchedText = seg.MatchedText, // Assuming DetailedAnalysisDto.Segment has MatchedText
                            Score = (decimal)seg.Score
                        });
                    }
                    else 
                    {
                        matchDetails.Add(new MatchDetailDto {
                            MatchedDocumentId = matchedDocument?.Id ?? 0, // Add MatchedDocumentId
                            MatchedDocumentTitle = seg.Source,
                            Author = authorName,
                            SimilarityPercentage = (decimal)seg.Score, // This is just for the first segment
                            TextMatches = new List<TextMatchDto> { new TextMatchDto { OriginalText = seg.Text, MatchedText = seg.MatchedText, Score = (decimal)seg.Score } }
                        });
                    }

                    // Create PlagiarismMatch records for database
                    if (matchedDocument != null)
                    {
                        matches.Add(new PlagiarismMatch
                        {
                            PlagiarismCheckId = plagiarismCheck.Id,
                            MatchedDocumentId = matchedDocument.Id,
                            MatchedText = seg.MatchedText, // Assuming DetailedAnalysisDto.Segment has MatchedText
                            StartPosition = seg.StartPosition, // Assuming DetailedAnalysisDto.Segment has StartPosition
                            EndPosition = seg.EndPosition, // Assuming DetailedAnalysisDto.Segment has EndPosition
                            SimilarityScore = (decimal)seg.Score
                        });
                    }
                }

                // Recalculate match details similarity
                foreach(var detail in matchDetails)
                {
                    detail.SimilarityPercentage = detail.TextMatches.Any() ? detail.TextMatches.Max(tm => tm.Score) : 0;
                }

                // Save all matches
                if (matches.Any())
                {
                    _context.PlagiarismMatches.AddRange(matches);
                }

                // Calculate overall similarity from detailed analysis
                var overallSimilarity = (decimal)detailedDto.OverallScore;

                // Update plagiarism check
                plagiarismCheck.OverallSimilarityPercentage = overallSimilarity;
                plagiarismCheck.TotalMatchedDocuments = matchDetails.Count;
                plagiarismCheck.Status = "Completed";
                plagiarismCheck.Notes = dto.Notes;

                await _context.SaveChangesAsync();

                return new PlagiarismCheckResultDto
                {
                    CheckId = plagiarismCheck.Id,
                    OverallSimilarityPercentage = overallSimilarity,
                    TotalMatchedDocuments = matchDetails.Count,
                    MatchDetails = matchDetails.OrderByDescending(m => m.SimilarityPercentage).ToList(),
                    DetailedAnalysis = detailedDto,
                    Status = "Completed",
                    CheckDate = plagiarismCheck.CheckDate
                };
            }
            catch (Exception ex)
            {
                plagiarismCheck.Status = "Failed";
                plagiarismCheck.Notes = $"Error: {ex.Message}";
                await _context.SaveChangesAsync();
                throw;
            }
        }

        /// <summary>
        /// Lấy lịch sử kiểm tra đạo văn
        /// </summary>
        public async Task<List<PlagiarismCheckDto>> GetPlagiarismHistoryAsync(int? userId = null, int? documentId = null, int? limit = null)
        {
            var query = _context.PlagiarismChecks
                .Include(p => p.SourceDocument)
                .Include(p => p.User)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);

            if (documentId.HasValue)
                query = query.Where(p => p.SourceDocumentId == documentId.Value);

            query = query.OrderByDescending(p => p.CheckDate);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            var checks = await query.ToListAsync();

            return checks.Select(p => new PlagiarismCheckDto
            {
                Id = p.Id,
                SourceDocumentId = p.SourceDocumentId,
                SourceDocumentTitle = p.SourceDocument.Title,
                UserId = p.UserId,
                UserName = p.User.FullName,
                CheckDate = p.CheckDate,
                OverallSimilarityPercentage = p.OverallSimilarityPercentage,
                Status = p.Status,
                TotalMatchedDocuments = p.TotalMatchedDocuments,
                Notes = p.Notes,
                Matches = new List<PlagiarismMatchDto>() // Don't fetch matches for list view
            }).ToList();
        }

        /// <summary>
        /// Lấy chi tiết kết quả kiểm tra
        /// </summary>
        public async Task<PlagiarismCheckDto?> GetCheckDetailAsync(int checkId)
        {
            var check = await _context.PlagiarismChecks
                .Include(p => p.SourceDocument)
                .Include(p => p.User)
                .Include(p => p.Matches)
                    .ThenInclude(m => m.MatchedDocument)
                .FirstOrDefaultAsync(p => p.Id == checkId);

            if (check == null) return null;

            return new PlagiarismCheckDto
            {
                Id = check.Id,
                SourceDocumentId = check.SourceDocumentId,
                SourceDocumentTitle = check.SourceDocument.Title,
                UserId = check.UserId,
                UserName = check.User.FullName,
                CheckDate = check.CheckDate,
                OverallSimilarityPercentage = check.OverallSimilarityPercentage,
                Status = check.Status,
                TotalMatchedDocuments = check.TotalMatchedDocuments,
                Notes = check.Notes,
                Matches = check.Matches.Select(m => new PlagiarismMatchDto
                {
                    Id = m.Id,
                    MatchedDocumentId = m.MatchedDocumentId,
                    MatchedDocumentTitle = m.MatchedDocument.Title,
                    MatchedText = m.MatchedText,
                    StartPosition = m.StartPosition,
                    EndPosition = m.EndPosition,
                    SimilarityScore = m.SimilarityScore
                }).ToList()
            };
        }

        /// <summary>
        /// Lấy thống kê đạo văn
        /// </summary>
        public async Task<PlagiarismStatisticsDto> GetStatisticsAsync(int? subjectId = null, int? userId = null)
        {
            var baseQuery = _context.PlagiarismChecks.Where(p => p.Status == "Completed");
            
            if (userId.HasValue)
                baseQuery = baseQuery.Where(p => p.UserId == userId.Value);

            if (subjectId.HasValue)
                baseQuery = baseQuery.Where(p => p.SourceDocument.SubjectId == subjectId.Value);

            // 1. Get summary stats in one go
            var statsSummary = await baseQuery
                .GroupBy(x => 1)
                .Select(g => new 
                {
                    TotalChecks = g.Count(),
                    AverageSimilarity = g.Average(c => c.OverallSimilarityPercentage),
                    HighRiskCount = g.Count(c => c.OverallSimilarityPercentage > 30),
                    MediumRiskCount = g.Count(c => c.OverallSimilarityPercentage >= 15 && c.OverallSimilarityPercentage <= 30),
                    LowRiskCount = g.Count(c => c.OverallSimilarityPercentage < 15)
                })
                .FirstOrDefaultAsync();

            var totalChecks = statsSummary?.TotalChecks ?? 0;
            var avgSimilarity = statsSummary?.AverageSimilarity ?? 0;
            var highRisk = statsSummary?.HighRiskCount ?? 0;
            var mediumRisk = statsSummary?.MediumRiskCount ?? 0;
            var lowRisk = statsSummary?.LowRiskCount ?? 0;

            var totalDocs = await _context.Documents.CountAsync(d => d.IsActive);
            var totalUsers = await _context.Users.CountAsync();

            // 2. Subject statistics
            var subjectStats = await baseQuery
                .Where(c => c.SourceDocument.SubjectId.HasValue)
                .GroupBy(c => new { c.SourceDocument.SubjectId, c.SourceDocument.Subject!.Name })
                .Select(g => new SubjectStatDto
                {
                    SubjectId = g.Key.SubjectId!.Value,
                    SubjectName = g.Key.Name,
                    DocumentCount = g.Count(),
                    AverageSimilarity = g.Average(c => c.OverallSimilarityPercentage)
                })
                .OrderByDescending(s => s.AverageSimilarity)
                .ToListAsync();

            return new PlagiarismStatisticsDto
            {
                TotalChecks = totalChecks,
                TotalDocuments = totalDocs,
                TotalUsers = totalUsers,
                AverageSimilarity = avgSimilarity,
                HighRiskCount = highRisk,
                MediumRiskCount = mediumRisk,
                LowRiskCount = lowRisk,
                SubjectStats = subjectStats
            };
        }

        /// <summary>
        /// Tìm các đoạn văn trùng lặp
        /// </summary>
        private List<TextMatchDto> FindMatchingSegments(string sourceText, string compareText, decimal overallSimilarity)
        {
            var matches = new List<TextMatchDto>();

            // Split into sentences
            var sourceSentences = SplitIntoSentences(sourceText);
            var compareSentences = SplitIntoSentences(compareText);

            int sourcePos = 0;
            foreach (var sourceSentence in sourceSentences)
            {
                var processedSource = _textProcessor.Process(sourceSentence);

                foreach (var compareSentence in compareSentences)
                {
                    var processedCompare = _textProcessor.Process(compareSentence);
                    var similarity = _similarityChecker.CalculateSimilarity(processedSource, processedCompare);

                    if (similarity > 0.5m) // High similarity threshold for sentence matching
                    {
                        matches.Add(new TextMatchDto
                        {
                            OriginalText = sourceSentence,
                            MatchedText = compareSentence,
                            StartPosition = sourcePos,
                            EndPosition = sourcePos + sourceSentence.Length,
                            Score = similarity * 100
                        });
                        break; // Only match once per source sentence
                    }
                }

                sourcePos += sourceSentence.Length + 1;
            }

            return matches;
        }

        private List<string> SplitIntoSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // Simple sentence splitting (can be improved with NLP)
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 20) // Ignore very short sentences
                .ToList();

            return sentences;
        }
    }
}
