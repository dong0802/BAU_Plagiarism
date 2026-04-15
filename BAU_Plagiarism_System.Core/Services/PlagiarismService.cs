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
        private readonly IBackgroundJobClient _backgroundJobs;

        public PlagiarismService(
            BAUDbContext context, 
            TextProcessor textProcessor, 
            SimilarityChecker similarityChecker,
            IBackgroundJobClient backgroundJobs)
        {
            _context = context;
            _textProcessor = textProcessor;
            _similarityChecker = similarityChecker;
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

            // Xác thực người dùng thực hiện kiểm tra
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("Không tìm thấy người dùng");

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
                Status = "Processing"
            };
        }

        /// <summary>
        /// Công việc chạy ngầm để xử lý đạo văn và phát hiện AI
        /// </summary>
        [AutomaticRetry(Attempts = 2)]
        public async Task ProcessCheckAsync(int checkId)
        {
            Console.WriteLine($"[PLAGIARISM-BG] ========== START ProcessCheckAsync (CheckId: {checkId}) ==========");
            
            var check = await _context.PlagiarismChecks
                .Include(p => p.SourceDocument)
                .FirstOrDefaultAsync(p => p.Id == checkId);

            if (check == null)
            {
                Console.WriteLine($"[PLAGIARISM-BG] Check {checkId} not found. Aborting.");
                return;
            }

            Console.WriteLine($"[PLAGIARISM-BG] Source doc: ID={check.SourceDocumentId}, ContentLength={check.SourceDocument?.Content?.Length ?? 0}");

            try
            {
                // TỔNG LỰC TỐI ƯU: Tách văn bản nguồn ra các đoạn (segments) CHỈ 1 LẦN duy nhất
                Console.WriteLine("[PLAGIARISM-BG] Step 0: Pre-splitting source document...");
                string cleanedSource = _textProcessor.CleanDocument(check.SourceDocument!.Content ?? string.Empty);
                var sourceSegments = _textProcessor.SplitIntoSmartSegments(cleanedSource);
                Console.WriteLine($"[PLAGIARISM-BG] Source split into {sourceSegments.Count} segments.");

                var analysis = new PlagiarismAnalysis { Segments = sourceSegments.Select(s => new HighlightedSegment { 
                    Text = s.RawText, 
                    IsExcluded = s.IsExcluded, 
                    ExclusionReason = s.ExclusionReason 
                }).ToList() };

                var sw = System.Diagnostics.Stopwatch.StartNew();
                
                int totalDocsProcess = await _context.Documents
                    .CountAsync(d => d.IsActive && d.IsPublic && d.Id != check.SourceDocumentId);
                
                Console.WriteLine($"[PLAGIARISM-BG] Total candidates for comparison: {totalDocsProcess}");

                int batchSize = 15; // Giảm xuống 15 để an toàn hơn nữa
                for (int i = 0; i < totalDocsProcess; i += batchSize)
                {
                    var batchDocs = await _context.Documents
                        .AsNoTracking()
                        .Where(d => d.IsActive && d.IsPublic && d.Id != check.SourceDocumentId)
                        .OrderBy(d => d.Id)
                        .Skip(i)
                        .Take(batchSize)
                        .ToListAsync();

                    Console.WriteLine($"[PLAGIARISM-BG] Comparing batch {i / batchSize + 1} with {batchDocs.Count} docs...");
                    
                    // Gọi hàm AnalyzeDetailed với danh sách segments đã có sẵn
                    _similarityChecker.CompareAgainstBatch(analysis, batchDocs);
                    
                    // Giải phóng bộ nhớ batch ngay lập tức
                    batchDocs.Clear();

                    // Cực kỳ quan trọng: Nhường CPU cho các tiến trình khác (như SQL Server) để chống lỗi Timeout khi tải file nén/văn bản
                    await Task.Delay(100);
                }

                // Bước 16: Tính toán tỷ lệ đạo văn tổng thể sau khi đã quét hết các batch
                // (Bước 15: ngưỡng 1% được xử lý bên trong CalculateOverallScore)
                Console.WriteLine("[PLAGIARISM-BG] Step 3: Calculating overall score (with 1% threshold)...");
                _similarityChecker.CalculateOverallScore(analysis);

                sw.Stop();
                var detailedResult = analysis;
                Console.WriteLine($"[PLAGIARISM-BG] Similarity analysis completed in {sw.ElapsedMilliseconds}ms. Final overall score: {detailedResult.OverallScore}");
                


                // 3. Lưu các phần trùng khớp
                Console.WriteLine("[PLAGIARISM-BG] Step 4: Saving matches...");
                var matches = new List<PlagiarismMatch>();
                // Chỉ lưu những đoạn trùng khớp thực sự (score > 0 = tìm thấy chuỗi từ trùng khớp)
                foreach (var seg in detailedResult.Segments.Where(s => s.Score > 0 && s.SourceId.HasValue))
                {
                    matches.Add(new PlagiarismMatch
                    {
                        PlagiarismCheckId = check.Id,
                        MatchedDocumentId = seg.SourceId!.Value,
                        MatchedText = seg.MatchedText ?? seg.Text,
                        StartPosition = seg.StartPosition,
                        EndPosition = seg.EndPosition,
                        SimilarityScore = (decimal)seg.Score
                    });
                }

                Console.WriteLine($"[PLAGIARISM-BG] Found {matches.Count} matches above threshold.");

                if (matches.Any())
                {
                    _context.PlagiarismMatches.AddRange(matches);
                }

                // Cập nhật kết quả kiểm tra
                check.OverallSimilarityPercentage = (decimal)detailedResult.OverallScore;
                check.TotalMatchedDocuments = matches.Select(m => m.MatchedDocumentId).Distinct().Count();
                check.Status = "Completed";

                Console.WriteLine("[PLAGIARISM-BG] Step 5: Saving to DB...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"[PLAGIARISM-BG] ========== COMPLETED CheckId: {checkId} ==========");
            }
            catch (OutOfMemoryException oomEx)
            {
                Console.WriteLine($"[PLAGIARISM-BG] OUT OF MEMORY for check {checkId}: {oomEx.Message}");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                check.Status = "Failed";
                check.Notes = $"Lỗi: Hết bộ nhớ khi xử lý tài liệu. Tài liệu quá lớn hoặc có quá nhiều dữ liệu so sánh.";
                try { await _context.SaveChangesAsync(); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLAGIARISM-BG] ERROR for check {checkId}: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"[PLAGIARISM-BG] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[PLAGIARISM-BG] Inner: {ex.InnerException.Message}");
                check.Status = "Failed";
                check.Notes = $"Lỗi xử lý ngầm: {ex.Message}";
                try { await _context.SaveChangesAsync(); } catch { }
            }
        }

        /// <summary>
        /// Lấy lịch sử kiểm tra đạo văn
        /// </summary>
        public async Task<List<PlagiarismCheckDto>> GetPlagiarismHistoryAsync(int? userId = null, int? documentId = null, int? limit = null)
        {
            // SỬ DỤNG SELECT PROJECTION thay vì Include để KHÔNG tải cột Content nặng
            var query = _context.PlagiarismChecks
                .AsNoTracking()
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);

            if (documentId.HasValue)
                query = query.Where(p => p.SourceDocumentId == documentId.Value);

            query = query.OrderByDescending(p => p.CheckDate);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            return await query.Select(p => new PlagiarismCheckDto
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
                Notes = p.Notes != null && p.Notes.Length > 200 ? p.Notes.Substring(0, 200) + "..." : p.Notes,
                Matches = new List<PlagiarismMatchDto>()
            }).ToListAsync();
        }

        /// <summary>
        /// Lấy chi tiết kết quả kiểm tra
        /// </summary>
        public async Task<PlagiarismCheckDto?> GetCheckDetailAsync(int checkId)
        {
            // Tải thông tin cơ bản trước
            var check = await _context.PlagiarismChecks
                .AsNoTracking()
                .Include(p => p.SourceDocument)
                .Include(p => p.User)
                .Include(p => p.Matches)
                    .ThenInclude(m => m.MatchedDocument)
                .FirstOrDefaultAsync(p => p.Id == checkId);

            if (check == null) return null;

            if (check.Status != "Completed")
            {
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
                    Notes = check.Notes
                };
            }

            // Đảm bảo không tải toàn bộ database khi lấy chi tiết
            var matchedDocs = check.Matches
                .Where(m => m.MatchedDocument != null)
                .Select(m => m.MatchedDocument!)
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

                Matches = check.Matches.Select(m => new PlagiarismMatchDto
                {
                    Id = m.Id,
                    MatchedDocumentId = m.MatchedDocumentId,
                    MatchedDocumentTitle = m.MatchedDocument.Title,
                    MatchedText = m.MatchedText,
                    StartPosition = m.StartPosition,
                    EndPosition = m.EndPosition,
                    SimilarityScore = m.SimilarityScore,
                    FullContent = m.MatchedDocument.Content, // Gửi toàn bộ nội dung tài liệu để so sánh
                    Author = m.MatchedDocument.User?.Username,
                    AuthorName = m.MatchedDocument.User?.FullName
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
            // SỬ DỤNG SELECT PROJECTION thay vì Include để KHÔNG tải cột Content nặng
            return await _context.PlagiarismChecks
                .AsNoTracking()
                .Where(p => p.Status == "Completed" && p.OverallSimilarityPercentage >= threshold)
                .OrderByDescending(p => p.OverallSimilarityPercentage)
                .ThenByDescending(p => p.CheckDate)
                .Take(limit)
                .Select(p => new PlagiarismCheckDto
                {
                    Id = p.Id,
                    SourceDocumentId = p.SourceDocumentId,
                    SourceDocumentTitle = p.SourceDocument.Title, // Chỉ lấy Title, KHÔNG lấy Content
                    UserId = p.UserId,
                    UserName = p.User.FullName,
                    CheckDate = p.CheckDate,
                    OverallSimilarityPercentage = p.OverallSimilarityPercentage,
                    Status = p.Status,
                    TotalMatchedDocuments = p.TotalMatchedDocuments,
                    Notes = p.Notes,
                    Matches = new List<PlagiarismMatchDto>()
                }).ToListAsync();
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

        /// <summary>
        /// So sánh trực tiếp 2 tài liệu (1vs1)
        /// </summary>
        public async Task<PlagiarismCheckDto> Compare1v1Async(int doc1Id, int doc2Id)
        {
            var doc1 = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == doc1Id);
            var doc2 = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == doc2Id);

            if (doc1 == null || doc2 == null)
                throw new Exception("Không tìm thấy tài liệu để so sánh");

            // Sử dụng SimilarityChecker để so sánh chi tiết
            var analysis = _similarityChecker.AnalyzeDetailed(doc1.Content ?? "", new List<Document> { doc2 });

            return new PlagiarismCheckDto
            {
                Id = 0, // Không lưu vào DB nên ID = 0
                SourceDocumentId = doc1Id,
                SourceDocumentTitle = doc1.Title,
                UserId = doc1.UserId,
                UserName = doc1.User?.FullName ?? "N/A",
                CheckDate = DateTime.Now,
                OverallSimilarityPercentage = (decimal)analysis.OverallScore,
                Status = "Completed",
                TotalMatchedDocuments = analysis.OverallScore > 0 ? 1 : 0,
                DetailedAnalysis = new DetailedAnalysisDto
                {
                    OverallScore = analysis.OverallScore,
                    Segments = analysis.Segments.Select(s => new SegmentDto
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
                Matches = analysis.Segments
                    .Where(s => s.Score > 0)
                    .Select(s => new PlagiarismMatchDto
                    {
                        MatchedDocumentId = doc2Id,
                        MatchedDocumentTitle = doc2.Title,
                        MatchedText = s.MatchedText ?? s.Text,
                        SimilarityScore = (decimal)s.Score,
                        StartPosition = s.StartPosition,
                        EndPosition = s.EndPosition,
                        Author = doc2.User?.Username,
                        AuthorName = doc2.User?.FullName,
                        FullContent = doc2.Content
                    }).ToList()
            };
        }
    }
}
