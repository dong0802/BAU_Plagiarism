using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

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
        public int? SourceId { get; set; }
        public bool IsBibliography { get; set; }
        public bool IsQuote { get; set; }
        public bool IsExcluded { get; set; }
        public string? ExclusionReason { get; set; }
    }

    public class PlagiarismAnalysis
    {
        public double OverallScore { get; set; }
        public List<HighlightedSegment> Segments { get; set; } = new();
    }

    /// <summary>
    /// Lưu thông tin một tài liệu DB đã được index sẵn (giữ tương thích).
    /// </summary>
    public class IndexedDocument
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public Dictionary<uint, List<int>> InvertedIndex { get; set; } = new();
        public List<uint[]> SegmentGrams { get; set; } = new();
        public List<string> SegmentTexts { get; set; } = new();
        public string FullCleanText { get; set; } = "";

        // === Turnitin-style: word-level index ===
        /// <summary>Mảng các từ đã normalize của toàn bộ tài liệu</summary>
        public string[] Words { get; set; } = Array.Empty<string>();
        /// <summary>RawText tương ứng với mỗi từ (để trả về matchedText gốc)</summary>
        public string[] RawWords { get; set; } = Array.Empty<string>();
        /// <summary>Hash lookup: hash(word_sequence) → list of starting positions</summary>
        public Dictionary<ulong, List<int>> WordSequenceIndex { get; set; } = new();
    }

    public class SimilarityChecker
    {
        private readonly TextProcessor _processor;

        // ═══ TURNITIN-STYLE CONSTANTS ═══
        /// <summary>
        /// Số từ liên tiếp tối thiểu phải trùng khớp để tính là đạo văn.
        /// Turnitin mặc định dùng khoảng 8 từ. Ta dùng 5 vì tiếng Việt ngắn hơn.
        /// </summary>
        private const int MIN_MATCH_WORDS = 5;
        
        /// <summary>
        /// Sau khi tìm thấy chuỗi trùng khớp MIN_MATCH_WORDS từ, 
        /// hệ thống sẽ mở rộng sang các từ tiếp theo nếu chúng cũng khớp.
        /// </summary>
        private const int NGRAM_SIZE = 3; // Giữ cho legacy API

        public SimilarityChecker()
        {
            _processor = new TextProcessor();
        }

        // ─── Legacy API (giữ để tương thích) ───────────────────────────────────

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
            => (decimal)(CalculateJaccardSimilarity(text1, text2, 3) / 100.0);

        // ─── Build Index (Turnitin-style) ───────────────────────────────────────

        /// <summary>
        /// Xây dựng index cho tài liệu DB. Ngoài Inverted Index cũ, 
        /// thêm word-level index cho exact string matching kiểu Turnitin.
        /// </summary>
        public IndexedDocument BuildIndex(BAU_Plagiarism_System.Data.Models.Document doc)
        {
            var indexed = new IndexedDocument { Id = doc.Id, Title = doc.Title };

            if (string.IsNullOrWhiteSpace(doc.Content))
                return indexed;

            var cleanDoc = _processor.CleanDocument(doc.Content);
            indexed.FullCleanText = _processor.NormalizeText(cleanDoc);

            // ═══ TURNITIN-STYLE: Word-level index ═══
            // Tách toàn bộ tài liệu thành mảng từ
            var normalizedFull = _processor.NormalizeText(doc.Content ?? "");
            indexed.Words = normalizedFull.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Lưu raw words tương ứng (để trả lại text gốc cho frontend)
            var rawWords = ExtractRawWords(doc.Content ?? "");
            indexed.RawWords = rawWords;

            // Build sequence hash index: với mỗi vị trí i, hash MIN_MATCH_WORDS từ liên tiếp
            indexed.WordSequenceIndex = new Dictionary<ulong, List<int>>();
            if (indexed.Words.Length >= MIN_MATCH_WORDS)
            {
                for (int i = 0; i <= indexed.Words.Length - MIN_MATCH_WORDS; i++)
                {
                    ulong hash = HashWordSequence(indexed.Words, i, MIN_MATCH_WORDS);
                    if (!indexed.WordSequenceIndex.TryGetValue(hash, out var positions))
                    {
                        positions = new List<int>();
                        indexed.WordSequenceIndex[hash] = positions;
                    }
                    positions.Add(i);
                }
            }

            // Giữ segment-level index cho tương thích
            var segments = _processor.SplitIntoSmartSegments(cleanDoc)
                .Where(s => !s.IsNoise && !s.IsExcluded)
                .ToList();

            for (int segIdx = 0; segIdx < segments.Count; segIdx++)
            {
                var seg = segments[segIdx];
                var grams = ComputeGrams(seg.CleanText);
                indexed.SegmentGrams.Add(grams);
                indexed.SegmentTexts.Add(seg.RawText);

                foreach (var gram in grams)
                {
                    if (!indexed.InvertedIndex.TryGetValue(gram, out var postingList))
                    {
                        postingList = new List<int>();
                        indexed.InvertedIndex[gram] = postingList;
                    }
                    postingList.Add(segIdx);
                }
            }

            return indexed;
        }

        // ─── Core: Turnitin-style exact string matching ─────────────────────────

        /// <summary>
        /// So sánh bài nộp với database - KIỂU TURNITIN.
        /// Tìm chuỗi từ liên tiếp (≥ MIN_MATCH_WORDS từ) trùng khớp chính xác.
        /// </summary>
        public void CompareAgainstIndexedBatch(PlagiarismAnalysis analysis, List<IndexedDocument> indexedDocs)
        {
            if (indexedDocs == null || indexedDocs.Count == 0) return;

            foreach (var seg in analysis.Segments)
            {
                if (seg.IsExcluded || string.IsNullOrWhiteSpace(seg.Text)) continue;

                // Normalize segment text thành mảng từ
                var segNormalized = _processor.NormalizeText(seg.Text);
                var segWords = segNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (segWords.Length < MIN_MATCH_WORDS) continue;

                double bestScore = seg.Score;
                string? bestSource = seg.Source;
                int? bestSourceId = seg.SourceId;
                string? bestMatchedText = seg.MatchedText;

                foreach (var dbDoc in indexedDocs)
                {
                    if (dbDoc.Words.Length < MIN_MATCH_WORDS) continue;

                    // Tìm chuỗi trùng khớp dài nhất giữa segment và tài liệu DB
                    var matchResult = FindLongestExactMatch(segWords, dbDoc);

                    if (matchResult.matchedWordCount > 0)
                    {
                        // Score = % số từ trong segment được tìm thấy trùng khớp
                        double score = (double)matchResult.matchedWordCount / segWords.Length * 100;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestSource = dbDoc.Title;
                            bestSourceId = dbDoc.Id;
                            bestMatchedText = matchResult.matchedText;
                        }
                    }
                }

                seg.Score = Math.Round(bestScore, 2);
                seg.Source = bestSource;
                seg.SourceId = bestSourceId;
                seg.MatchedText = bestMatchedText;
            }
        }

        /// <summary>
        /// Tìm chuỗi từ trùng khớp chính xác giữa segment source và tài liệu DB.
        /// Trả về tổng số từ trùng khớp và text tương ứng từ tài liệu DB.
        /// 
        /// Thuật toán:
        /// 1. Với mỗi vị trí i trong segWords, hash MIN_MATCH_WORDS từ liên tiếp
        /// 2. Tra cứu hash trong WordSequenceIndex của dbDoc
        /// 3. Nếu tìm thấy → mở rộng match sang hai bên để lấy chuỗi dài nhất
        /// 4. Đánh dấu các từ đã match để không đếm trùng
        /// </summary>
        private (int matchedWordCount, string matchedText) FindLongestExactMatch(
            string[] segWords, IndexedDocument dbDoc)
        {
            bool[] matched = new bool[segWords.Length];
            int totalMatched = 0;
            int bestDbStart = -1;
            int bestDbEnd = -1;

            for (int i = 0; i <= segWords.Length - MIN_MATCH_WORDS; i++)
            {
                if (matched[i]) continue; // Đã match rồi, bỏ qua

                ulong hash = HashWordSequence(segWords, i, MIN_MATCH_WORDS);

                if (!dbDoc.WordSequenceIndex.TryGetValue(hash, out var dbPositions))
                    continue;

                // Tìm thấy hash trùng → verify từng vị trí trong DB
                foreach (var dbStart in dbPositions)
                {
                    // Verify: kiểm tra MIN_MATCH_WORDS từ có thực sự khớp không (tránh hash collision)
                    bool verified = true;
                    for (int k = 0; k < MIN_MATCH_WORDS; k++)
                    {
                        if (segWords[i + k] != dbDoc.Words[dbStart + k])
                        {
                            verified = false;
                            break;
                        }
                    }
                    if (!verified) continue;

                    // ✅ Đã xác nhận trùng khớp! Bây giờ MỞ RỘNG match ra hai bên
                    int matchStart = i;
                    int matchEnd = i + MIN_MATCH_WORDS - 1;
                    int dbMatchStart = dbStart;
                    int dbMatchEnd = dbStart + MIN_MATCH_WORDS - 1;

                    // Mở rộng sang phải
                    while (matchEnd + 1 < segWords.Length &&
                           dbMatchEnd + 1 < dbDoc.Words.Length &&
                           segWords[matchEnd + 1] == dbDoc.Words[dbMatchEnd + 1])
                    {
                        matchEnd++;
                        dbMatchEnd++;
                    }

                    // Mở rộng sang trái
                    while (matchStart > 0 &&
                           dbMatchStart > 0 &&
                           !matched[matchStart - 1] &&
                           segWords[matchStart - 1] == dbDoc.Words[dbMatchStart - 1])
                    {
                        matchStart--;
                        dbMatchStart--;
                    }

                    // Đánh dấu các từ đã match
                    for (int m = matchStart; m <= matchEnd; m++)
                    {
                        if (!matched[m])
                        {
                            matched[m] = true;
                            totalMatched++;
                        }
                    }

                    // Ghi nhớ vị trí match dài nhất trong DB (để trả về matchedText)
                    int thisLength = dbMatchEnd - dbMatchStart + 1;
                    int bestLength = bestDbEnd - bestDbStart + 1;
                    if (bestDbStart == -1 || thisLength > bestLength)
                    {
                        bestDbStart = dbMatchStart;
                        bestDbEnd = dbMatchEnd;
                    }

                    break; // Đã match tại vị trí này, không cần thử vị trí DB khác
                }
            }

            if (totalMatched == 0 || bestDbStart == -1)
                return (0, "");

            // Lấy text gốc từ DB tương ứng với đoạn match dài nhất
            string matchedText;
            if (dbDoc.RawWords.Length > bestDbEnd)
            {
                var rawSlice = dbDoc.RawWords.Skip(bestDbStart).Take(bestDbEnd - bestDbStart + 1);
                matchedText = string.Join(" ", rawSlice);
            }
            else
            {
                matchedText = string.Join(" ", dbDoc.Words.Skip(bestDbStart).Take(bestDbEnd - bestDbStart + 1));
            }

            return (totalMatched, matchedText);
        }

        // ─── CompareAgainstBatch (legacy wrapper) ───────────────────────────────

        public void CompareAgainstBatch(PlagiarismAnalysis analysis, List<BAU_Plagiarism_System.Data.Models.Document> database)
        {
            if (database == null || !database.Any()) return;

            var indexed = database
                .Where(d => !string.IsNullOrEmpty(d.Content))
                .Select(d => BuildIndex(d))
                .ToList();

            CompareAgainstIndexedBatch(analysis, indexed);
        }

        public PlagiarismAnalysis AnalyzeDetailed(string newText, List<BAU_Plagiarism_System.Data.Models.Document> database)
        {
            if (string.IsNullOrWhiteSpace(newText))
                return new PlagiarismAnalysis();

            // Lấy văn bản bài nộp (giữ nguyên tất cả, việc lọc sẽ dựa trên thuộc tính segment)
            // excludeBibliography: false để không bị truncate mất phần cuối
            string fullNewText = _processor.CleanDocument(newText, excludeBibliography: false); 
            var sourceSegments = _processor.SplitIntoSmartSegments(fullNewText);

            var analysis = new PlagiarismAnalysis
            {
                Segments = sourceSegments.Select(s => new HighlightedSegment
                {
                    Text = s.RawText,
                    IsExcluded = s.IsExcluded,
                    ExclusionReason = s.ExclusionReason,
                    IsBibliography = s.IsBibliography,
                    IsQuote = s.IsQuote
                }).ToList()
            };

            CompareAgainstBatch(analysis, database);
            CalculateOverallScore(analysis);
            return analysis;
        }

        /// <summary>
        /// Tính điểm tổng thể kiểu Turnitin:
        /// Score = (tổng từ trùng khớp / tổng từ bài nộp) × 100
        /// 
        /// Một segment được tính là "trùng khớp" nếu có score > 0 
        /// (tức là đã tìm thấy ≥ MIN_MATCH_WORDS từ liên tiếp giống nhau).
        /// </summary>
        public void CalculateOverallScore(PlagiarismAnalysis analysis)
        {
            int totalWords = 0;
            int totalMatchedWords = 0;

            foreach (var seg in analysis.Segments)
            {
                if (seg.IsExcluded || string.IsNullOrWhiteSpace(seg.Text)) continue;

                var words = seg.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                totalWords += words;

                // Turnitin-style: nếu segment có score > 0 (đã tìm thấy chuỗi trùng),
                // tính số từ thực sự trùng = words × score / 100
                if (seg.Score > 0 && seg.SourceId.HasValue)
                {
                    int matchedInSeg = (int)Math.Round(words * seg.Score / 100.0);
                    totalMatchedWords += matchedInSeg;
                }
            }

            analysis.OverallScore = totalWords > 0
                ? Math.Round((double)totalMatchedWords / totalWords * 100, 2)
                : 0;
        }

        // ─── Private helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Trích xuất các từ "thô" (raw) từ text gốc, giữ nguyên dấu tiếng Việt.
        /// Dùng để trả về matchedText gốc cho frontend hiển thị.
        /// </summary>
        private string[] ExtractRawWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            
            var words = new List<string>();
            var sb = new StringBuilder();
            
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '\'')
                {
                    sb.Append(c);
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        words.Add(sb.ToString());
                        sb.Clear();
                    }
                }
            }
            if (sb.Length > 0) words.Add(sb.ToString());
            
            return words.ToArray();
        }

        /// <summary>
        /// Hash một chuỗi n từ liên tiếp bắt đầu từ vị trí start.
        /// Dùng FNV-1a 64-bit để giảm collision.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong HashWordSequence(string[] words, int start, int count)
        {
            ulong hash = 14695981039346656037UL; // FNV-1a 64-bit offset
            const ulong prime = 1099511628211UL;

            for (int i = start; i < start + count; i++)
            {
                if (i > start)
                {
                    hash ^= (byte)' ';
                    hash *= prime;
                }
                foreach (char c in words[i])
                {
                    hash ^= (byte)c;
                    hash *= prime;
                }
            }
            return hash;
        }

        // Giữ ComputeGrams cho legacy compatibility
        private uint[] ComputeGrams(string normalizedText)
        {
            if (string.IsNullOrEmpty(normalizedText)) return Array.Empty<uint>();

            var words = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < NGRAM_SIZE) return Array.Empty<uint>();

            var gramSet = new HashSet<uint>(words.Length);
            for (int i = 0; i <= words.Length - NGRAM_SIZE; i++)
            {
                uint h = FNV_OFFSET;
                for (int j = 0; j < NGRAM_SIZE; j++)
                {
                    if (j > 0) h = FnvStep(h, (byte)' ');
                    foreach (char c in words[i + j])
                        h = FnvStep(h, (byte)c);
                }
                gramSet.Add(h);
            }

            return gramSet.ToArray();
        }

        private const uint FNV_OFFSET = 2166136261u;
        private const uint FNV_PRIME  = 16777619u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FnvStep(uint hash, byte b)
            => (hash ^ b) * FNV_PRIME;
    }
}
