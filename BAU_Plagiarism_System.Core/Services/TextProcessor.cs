using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BAU_Plagiarism_System.Core.Services
{
    public class TextProcessor
    {
        // ═══════════════════════════════════════════════════════════════
        // Bước 3 & 4: Phân tích cấu trúc tài liệu - Loại bỏ vùng không kiểm tra
        // Nhận diện: Nội dung chính | Tài liệu tham khảo | Phụ lục
        // ═══════════════════════════════════════════════════════════════
        private static readonly string[] BibliographyKeywords = {
            "tài liệu tham khảo",
            "danh mục tài liệu tham khảo",
            "danh mục tài liệu",
            "references",
            "bibliography",
            "references list"
        };

        private static readonly string[] AppendixKeywords = {
            "phụ lục",
            "appendix",
            "phụ chú",
            "phần phụ lục"
        };

        // ═══════════════════════════════════════════════════════════════
        // Bước 14: Lọc nhiễu - Cụm từ phổ biến (common phrases)
        // ═══════════════════════════════════════════════════════════════
        private static readonly string[] CommonPhrases = {
            "học viện ngân hàng", "ngân hàng nhà nước", "kinh tế tài chính",
            "theo quy định của pháp luật", "trong bối cảnh hiện nay",
            "mục tiêu của nghiên cứu", "kết quả nghiên cứu cho thấy",
            "trên cơ sở đó", "có thể thấy rằng", "hệ thống ngân hàng thương mại",
            "khoa tài chính", "bài tập lớn", "nhóm 2", "giảng viên hướng dẫn",
        };

        // ═══════════════════════════════════════════════════════════════
        // Bước 7: Xử lý ngôn ngữ - Stopwords tiếng Việt
        // Loại bỏ các từ dừng (từ không mang nghĩa) trước khi so sánh
        // ═══════════════════════════════════════════════════════════════
        private static readonly HashSet<string> VietnameseStopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            // Đại từ
            "tôi", "tao", "mày", "nó", "họ", "chúng", "mình", "ta", "chúng tôi", "chúng ta",
            "anh", "chị", "em", "ông", "bà", "cô", "thầy",
            // Giới từ & liên từ
            "và", "hoặc", "hay", "nhưng", "mà", "thì", "vì", "nên", "nếu", "thì",
            "với", "của", "trong", "ngoài", "trên", "dưới", "bên", "giữa",
            "từ", "đến", "về", "cho", "vào", "ra", "lên", "xuống",
            "tại", "ở", "qua", "theo", "đối", "với", "bởi",
            // Trạng từ & từ chỉ mức độ
            "rất", "khá", "hơn", "nhất", "quá", "thêm", "vẫn", "đã", "đang", "sẽ",
            "cũng", "còn", "mới", "lại", "cứ", "nữa", "thôi", "nhé", "à", "ạ",
            "được", "bị", "có", "không", "chưa", "đã", "sẽ", "phải", "cần",
            // Từ chỉ số lượng & thời gian
            "các", "những", "mỗi", "một", "hai", "ba", "nhiều", "ít",
            "đây", "đó", "kia", "này", "nào", "gì", "ai", "sao",
            "khi", "lúc", "hôm", "nay", "sau", "trước", "ngày", "tháng", "năm",
            // Từ nối câu
            "ngoài ra", "bên cạnh đó", "do đó", "vì vậy", "tuy nhiên", "hơn nữa",
            "thứ nhất", "thứ hai", "thứ ba", "đầu tiên", "cuối cùng",
            // Từ thường gặp trong văn bản học thuật
            "là", "như", "để", "mà", "khi", "nếu", "vì", "tuy",
            "cả", "toàn", "hết", "tất", "đều", "chỉ", "thậm chí",
        };

        // ═══════════════════════════════════════════════════════════════
        // Bước 5: Phát hiện và loại bỏ trích dẫn APA
        // Các dạng: (Nguyễn Văn A, 2020) | Nguyễn Văn A (2020) | "..." (Smith, 2019)
        // ═══════════════════════════════════════════════════════════════

        // Regex nhận diện citation dạng (Tên, năm) hoặc (Tên et al., năm)
        private static readonly Regex ApaCitationInline = new Regex(
            @"\([^()]{2,60},\s*\d{4}[a-z]?\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex nhận diện citation dạng Tên (năm) đứng đầu câu
        private static readonly Regex ApaCitationNarrative = new Regex(
            @"\b[A-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠ][a-zA-ZÀ-ỹ\s]{1,40}\s*\(\d{4}[a-z]?\)",
            RegexOptions.Compiled);

        // Regex nhận diện trích dẫn nguyên văn (toàn bộ câu có dấu ngoặc kép + citation)
        private static readonly Regex ApaQuotedCitation = new Regex(
            @"""[^""]{10,500}""\s*\([^()]{2,60},\s*\d{4}[a-z]?\)",
            RegexOptions.Compiled);

        // ═══════════════════════════════════════════════════════════════
        // Bước 6: Chuẩn hóa văn bản
        // Chuyển chữ thường, loại ký tự đặc biệt, chuẩn hóa khoảng trắng
        // ═══════════════════════════════════════════════════════════════
        public string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Bước 6a: Loại bỏ APA citations trước khi normalize
            text = RemoveApaCitations(text);

            // Bước 6b & 6c: Chuyển về chữ thường + loại ký tự đặc biệt + chuẩn hóa khoảng trắng
            var sb = new StringBuilder(text.Length);
            bool lastWasSpace = true;

            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                    lastWasSpace = false;
                }
                else
                {
                    if (!lastWasSpace)
                    {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                }
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                sb.Length--;

            return sb.ToString();
        }

        /// <summary>
        /// Bước 7: Xử lý NLP - chuẩn hóa từ và loại bỏ stopwords
        /// Dùng cho việc so sánh nội dung (không phải hiển thị)
        /// </summary>
        public string NormalizeAndRemoveStopwords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var normalized = NormalizeText(text);
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Loại bỏ stopwords - chỉ giữ từ có ý nghĩa
            var filtered = words.Where(w => !VietnameseStopwords.Contains(w) && w.Length > 1);
            return string.Join(" ", filtered);
        }

        /// <summary>
        /// Bước 5: Loại bỏ trích dẫn APA khỏi văn bản
        /// </summary>
        public string RemoveApaCitations(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            // Bước 5a: Xóa toàn bộ câu chứa trích dẫn nguyên văn (quoted citation)
            text = ApaQuotedCitation.Replace(text, " ");

            // Bước 5b: Xóa citation nội tuyến dạng (Tên, năm)
            text = ApaCitationInline.Replace(text, " ");

            // Bước 5c: Xóa citation narrative dạng Tên (năm)
            text = ApaCitationNarrative.Replace(text, " ");

            return text;
        }

        /// <summary>
        /// Xử lý văn bản (bí danh cho NormalizeText - giữ tương thích)
        /// </summary>
        public string Process(string text) => NormalizeText(text);

        // ═══════════════════════════════════════════════════════════════
        // Bước 3 & 4: Làm sạch tài liệu - loại bỏ phần không kiểm tra
        // (Tài liệu tham khảo, Phụ lục)
        // ═══════════════════════════════════════════════════════════════
        public string CleanDocument(string text, bool excludeBibliography = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            if (!excludeBibliography) return text;

            // Tìm trong 40% cuối tài liệu (tăng từ 30% để bắt được phụ lục)
            int searchStart = (int)(text.Length * 0.6);
            string endPart = text.Substring(searchStart).ToLower();

            int bestIndexInPart = -1;

            // Kiểm tra Tài liệu tham khảo
            foreach (var keyword in BibliographyKeywords)
            {
                int index = endPart.LastIndexOf(keyword);
                if (index > bestIndexInPart)
                    bestIndexInPart = index;
            }

            // Kiểm tra Phụ lục (nếu xuất hiện trước tài liệu tham khảo thì ưu tiên cắt sớm hơn)
            foreach (var keyword in AppendixKeywords)
            {
                int index = endPart.LastIndexOf(keyword);
                if (index != -1 && (bestIndexInPart == -1 || index < bestIndexInPart))
                    bestIndexInPart = index;
            }

            if (bestIndexInPart != -1)
                return text.Substring(0, searchStart + bestIndexInPart);

            return text;
        }

        private const int MAX_SEGMENT_CHARS = 3000;

        // ═══════════════════════════════════════════════════════════════
        // Bước 8: Phân đoạn văn bản
        // Tách theo câu/đoạn dựa vào dấu chấm và xuống dòng
        // Bước 14: Lọc đoạn quá ngắn và cụm từ phổ biến
        // ═══════════════════════════════════════════════════════════════
        public List<TextSegment> SplitIntoSmartSegments(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<TextSegment>();

            var segments = new List<TextSegment>();
            var delimiters = new[] { '.', '!', '?', '\n', '\r' };

            // Bước 3: Tìm vị trí bắt đầu của tài liệu tham khảo hoặc phụ lục
            int bibStart = -1;
            int searchStart = (int)(text.Length * 0.6);
            if (text.Length > searchStart)
            {
                string endPart = text.Substring(searchStart).ToLower();

                // Kiểm tra tài liệu tham khảo
                foreach (var keyword in BibliographyKeywords)
                {
                    int index = endPart.LastIndexOf(keyword);
                    if (index != -1)
                    {
                        int absoluteIndex = searchStart + index;
                        if (bibStart == -1 || absoluteIndex < bibStart)
                            bibStart = absoluteIndex;
                    }
                }

                // Bước 4: Kiểm tra phụ lục - cũng loại bỏ khỏi so sánh
                foreach (var keyword in AppendixKeywords)
                {
                    int index = endPart.LastIndexOf(keyword);
                    if (index != -1)
                    {
                        int absoluteIndex = searchStart + index;
                        if (bibStart == -1 || absoluteIndex < bibStart)
                            bibStart = absoluteIndex;
                    }
                }
            }

            int lastPos = 0;
            for (int i = 0; i < text.Length; i++)
            {
                bool isDelimiter = delimiters.Contains(text[i]);
                bool isForceSplit = (i - lastPos) > MAX_SEGMENT_CHARS;
                bool isEnd = i == text.Length - 1;

                if (isDelimiter || isForceSplit || isEnd)
                {
                    int length = i - lastPos + 1;
                    string raw = text.Substring(lastPos, length);

                    // Bước 5: Loại bỏ APA citations khỏi raw text trước khi xử lý
                    string rawWithoutCitation = RemoveApaCitations(raw);
                    string clean = NormalizeText(rawWithoutCitation);

                    var segment = new TextSegment { RawText = raw, CleanText = clean };

                    // Bước 4: Đánh dấu đoạn thuộc phần không kiểm tra (tài liệu tham khảo/phụ lục)
                    if (bibStart != -1 && lastPos >= bibStart)
                    {
                        segment.IsBibliography = true;
                        segment.IsExcluded = true;
                        segment.ExclusionReason = "Loại trừ Mục lục Tham khảo / Phụ lục";
                    }

                    // Bước 14a: Loại đoạn quá ngắn (nhiễu)
                    if (string.IsNullOrWhiteSpace(clean))
                    {
                        segment.IsNoise = true;
                    }
                    else if (clean.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 5)
                    {
                        // Nâng ngưỡng từ 3 lên 5 để lọc nhiễu tốt hơn (Bước 14)
                        segment.IsNoise = true;
                        if (string.IsNullOrEmpty(segment.ExclusionReason))
                            segment.ExclusionReason = "Đoạn văn quá ngắn (< 5 từ)";
                    }
                    else if (!segment.IsBibliography)
                    {
                        // Kiểm tra trích dẫn nguyên văn (Quotes)
                        string trimmed = raw.Trim();
                        bool isQuote = (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) ||
                                        (trimmed.StartsWith("\u201c") && trimmed.EndsWith("\u201d")) ||
                                        (trimmed.StartsWith("«") && trimmed.EndsWith("»")) ||
                                        (trimmed.Count(c => c == '\"') >= 2 && trimmed.Length > 20);

                        if (isQuote)
                        {
                            segment.IsQuote = true;
                            segment.IsExcluded = true;
                            segment.ExclusionReason = "Trích dẫn nguyên văn (Quote)";
                        }

                        // Bước 14b: Loại cụm từ phổ biến
                        foreach (var phrase in CommonPhrases)
                        {
                            if (clean.Contains(phrase))
                            {
                                segment.IsCommonPhrase = true;
                                segment.IsExcluded = true;
                                segment.ExclusionReason = $"Cụm từ phổ biến: '{phrase}'";
                                break;
                            }
                        }
                    }

                    segments.Add(segment);
                    lastPos = i + 1;
                }
            }

            return segments;
        }

        public List<string> Tokenize(string text)
        {
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        // ═══════════════════════════════════════════════════════════════
        // Bước 9: Tạo N-gram (cửa sổ trượt)
        // ═══════════════════════════════════════════════════════════════
        public HashSet<int> GenerateHashedNGrams(string text, int n)
        {
            if (string.IsNullOrEmpty(text)) return new HashSet<int>();

            var words = Tokenize(NormalizeText(text));
            var nGrams = new HashSet<int>();

            if (words.Count < n) return nGrams;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= words.Count - n; i++)
            {
                sb.Clear();
                for (int j = 0; j < n; j++)
                {
                    if (j > 0) sb.Append(" ");
                    sb.Append(words[i + j]);
                }
                nGrams.Add(sb.ToString().GetHashCode());
            }

            return nGrams;
        }

        public HashSet<string> GenerateNGrams(string text, int n)
        {
            if (string.IsNullOrEmpty(text)) return new HashSet<string>();

            var words = Tokenize(NormalizeText(text));
            var nGrams = new HashSet<string>();

            if (words.Count < n) return nGrams;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= words.Count - n; i++)
            {
                if (n == 1)
                {
                    nGrams.Add(words[i]);
                }
                else
                {
                    sb.Clear();
                    for (int j = 0; j < n; j++)
                    {
                        if (j > 0) sb.Append(" ");
                        sb.Append(words[i + j]);
                    }
                    nGrams.Add(sb.ToString());
                }
            }

            return nGrams;
        }
    }

    public class TextSegment
    {
        public string RawText { get; set; } = "";
        public string CleanText { get; set; } = "";
        public bool IsNoise { get; set; } = false;
        public bool IsCommonPhrase { get; set; } = false;
        public bool IsBibliography { get; set; } = false;
        public bool IsQuote { get; set; } = false;
        public bool IsExcluded { get; set; } = false;
        public string? ExclusionReason { get; set; }
    }
}
