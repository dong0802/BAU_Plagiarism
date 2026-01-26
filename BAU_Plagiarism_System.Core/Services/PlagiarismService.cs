using System.Text.Json;
using Hangfire;
using BAU_Plagiarism_System.Core.DTOs;
using BAU_Plagiarism_System.Data;
using BAU_Plagiarism_System.Data.Models;
using Microsoft.EntityFrameworkCore;

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
        private readonly AiDetectionService _aiDetectionService;
        private readonly IBackgroundJobClient _backgroundJobs;

        public PlagiarismService(
            BAUDbContext context, 
            TextProcessor textProcessor, 
            SimilarityChecker similarityChecker,
            AiDetectionService aiDetectionService,
            IBackgroundJobClient backgroundJobs)
        {
            _context = context;
            _textProcessor = textProcessor;
            _similarityChecker = similarityChecker;
            _aiDetectionService = aiDetectionService;
            _backgroundJobs = backgroundJobs;
        }

        /// <summary>
        /// Kiểm tra đạo văn cho một tài liệu
        /// </summary>
        public async Task<PlagiarismCheckResultDto> CheckPlagiarismAsync(int userId, CreatePlagiarismCheckDto dto)
        {
            // Lấy tài liệu nguồn
            var sourceDoc = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == dto.SourceDocumentId);

            if (sourceDoc == null)
                throw new Exception("Không tìm thấy tài liệu nguồn");

            // Kiểm tra giới hạn hàng ngày
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("Không tìm thấy người dùng");

            // Đặt lại bộ đếm nếu là ngày mới
            if (user.LastCheckResetDate == null || user.LastCheckResetDate.Value.Date < DateTime.Now.Date)
            {
                user.ChecksUsedToday = 0;
                user.LastCheckResetDate = DateTime.Now;
            }

            // Kiểm tra nếu sinh viên còn lượt kiểm tra
            if (user.Role == "Student" && user.ChecksUsedToday >= user.DailyCheckLimit)
            {
                throw new Exception($"Bạn đã hết lượt kiểm tra trong ngày hôm nay (Tối đa {user.DailyCheckLimit} lượt/ngày).");
            }

            // Tăng số lượt kiểm tra
            user.ChecksUsedToday++;
            await _context.SaveChangesAsync();

            // Tạo bản ghi kiểm tra đạo văn
            var plagiarismCheck = new PlagiarismCheck
            {
                SourceDocumentId = dto.SourceDocumentId,
                UserId = userId,
                CheckDate = DateTime.Now,
                Status = "Processing"
            };

            _context.PlagiarismChecks.Add(plagiarismCheck);
            await _context.SaveChangesAsync();

            // Đưa công việc vào hàng đợi xử lý ngầm
            _backgroundJobs.Enqueue<PlagiarismService>(x => x.ProcessCheckAsync(plagiarismCheck.Id));

            return new PlagiarismCheckResultDto
            {
                CheckId = plagiarismCheck.Id,
                Status = "Processing",
                RemainingChecksToday = user.DailyCheckLimit - user.ChecksUsedToday,
                DailyCheckLimit = user.DailyCheckLimit
            };
        }

        /// <summary>
        /// Công việc chạy ngầm để xử lý đạo văn và phát hiện AI
        /// </summary>
        [AutomaticRetry(Attempts = 2)]
        public async Task ProcessCheckAsync(int checkId)
        {
            var check = await _context.PlagiarismChecks
                .Include(p => p.SourceDocument)
                .FirstOrDefaultAsync(p => p.Id == checkId);

            if (check == null) return;

            try
            {
                // 1. Kiểm tra độ tương đồng (Đạo văn)
                var compareDocs = await _context.Documents
                    .Where(d => d.IsActive && d.IsPublic && d.Id != check.SourceDocumentId)
                    .ToListAsync();

                var detailedResult = _similarityChecker.AnalyzeDetailed(check.SourceDocument.Content, compareDocs);
                
                // 2. Phát hiện AI
                var aiResult = await _aiDetectionService.DetectAiAsync(check.SourceDocument.Content);

                // 3. Lưu các phần trùng khớp
                var matches = new List<PlagiarismMatch>();
                foreach (var seg in detailedResult.Segments.Where(s => s.Score > 20 && !string.IsNullOrEmpty(s.Source)))
                {
                    var matchedDoc = compareDocs.FirstOrDefault(d => d.Title == seg.Source);
                    if (matchedDoc != null)
                    {
                        matches.Add(new PlagiarismMatch
                        {
                            PlagiarismCheckId = check.Id,
                            MatchedDocumentId = matchedDoc.Id,
                            MatchedText = seg.MatchedText ?? seg.Text,
                            StartPosition = seg.StartPosition,
                            EndPosition = seg.EndPosition,
                            SimilarityScore = (decimal)seg.Score
                        });
                    }
                }

                if (matches.Any())
                {
                    _context.PlagiarismMatches.AddRange(matches);
                }

                // Cập nhật kết quả kiểm tra
                check.OverallSimilarityPercentage = (decimal)detailedResult.OverallScore;
                check.TotalMatchedDocuments = matches.Select(m => m.MatchedDocumentId).Distinct().Count();
                check.AiProbability = (decimal)aiResult.AiProbability;
                check.AiDetectionLevel = aiResult.DetectionLevel;
                check.AiDetectionJson = JsonSerializer.Serialize(aiResult);
                check.Status = "Completed";

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                check.Status = "Failed";
                check.Notes = $"Lỗi xử lý ngầm: {ex.Message}";
                await _context.SaveChangesAsync();
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
                Matches = new List<PlagiarismMatchDto>() // Không lấy các phần trùng khớp cho giao diện danh sách
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

            // Tái cấu trúc phân tích chi tiết bằng cách sử dụng các phần trùng khớp đã lưu để làm nổi bật trên giao diện
            var matchedDocs = check.Matches
                .Select(m => m.MatchedDocument)
                .Where(d => d != null)
                .DistinctBy(d => d.Id)
                .ToList();

            var detailedResult = _similarityChecker.AnalyzeDetailed(check.SourceDocument.Content, matchedDocs);

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
                DetailedAnalysis = new DetailedAnalysisDto
                {
                    OverallScore = (double)check.OverallSimilarityPercentage,
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
                },
                AiProbability = check.AiProbability,
                AiDetectionLevel = check.AiDetectionLevel,
                AiAnalysis = string.IsNullOrEmpty(check.AiDetectionJson) 
                    ? null 
                    : JsonSerializer.Deserialize<AiDetectionResultDto>(check.AiDetectionJson),
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

            // 1. Lấy thống kê tóm tắt trong một lần gọi
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
            var totalFaculties = await _context.Faculties.CountAsync();
            var totalSubjects = await _context.Subjects.CountAsync();
            var totalStudents = await _context.Users.CountAsync(u => u.Role == "Student");

            // 2. Thống kê theo môn học
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
                TotalFaculties = totalFaculties,
                TotalSubjects = totalSubjects,
                TotalStudents = totalStudents,
                AverageSimilarity = avgSimilarity,
                HighRiskCount = highRisk,
                MediumRiskCount = mediumRisk,
                LowRiskCount = lowRisk,
                SubjectStats = subjectStats
            };
        }

        /// <summary>
        /// Lấy danh sách kiểm tra có tỷ lệ đạo văn cao (Cảnh báo nóng)
        /// </summary>
        public async Task<List<PlagiarismCheckDto>> GetHighRiskChecksAsync(decimal threshold = 50.0m, int limit = 10)
        {
            var checks = await _context.PlagiarismChecks
                .Include(p => p.SourceDocument)
                .Include(p => p.User)
                .Where(p => p.Status == "Completed" && p.OverallSimilarityPercentage >= threshold)
                .OrderByDescending(p => p.OverallSimilarityPercentage)
                .ThenByDescending(p => p.CheckDate)
                .Take(limit)
                .ToListAsync();

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
                Matches = new List<PlagiarismMatchDto>()
            }).ToList();
        }


        /// <summary>
        /// Tìm các đoạn văn trùng lặp
        /// </summary>
        private List<TextMatchDto> FindMatchingSegments(string sourceText, string compareText, decimal overallSimilarity)
        {
            var matches = new List<TextMatchDto>();

            // Chia thành các câu
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

                    if (similarity > 0.5m) // Ngưỡng tương đồng cao cho việc đối soát câu
                    {
                        matches.Add(new TextMatchDto
                        {
                            OriginalText = sourceSentence,
                            MatchedText = compareSentence,
                            StartPosition = sourcePos,
                            EndPosition = sourcePos + sourceSentence.Length,
                            Score = similarity * 100
                        });
                        break; // Chỉ đối soát một lần cho mỗi câu nguồn
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

            // Chia câu đơn giản (có thể cải thiện bằng NLP)
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 20) // Bỏ qua các câu quá ngắn
                .ToList();

            return sentences;
        }
    }
}
