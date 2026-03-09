using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BAU_Plagiarism_System.Core.Services
{
    public class TextProcessor
    {
        private static readonly string[] BibliographyKeywords = { 
            "tài liệu tham khảo", "danh mục tài liệu", "references", "bibliography", "references list", "phụ lục" 
        };

        private static readonly string[] CommonPhrases = {
            "học viện ngân hàng", "ngân hàng nhà nước", "kinh tế tài chính", 
            "theo quy định của pháp luật", "trong bối cảnh hiện nay",
            "mục tiêu của nghiên cứu", "kết quả nghiên cứu cho thấy",
            "trên cơ sở đó", "có thể thấy rằng", "hệ thống ngân hàng thương mại",
            "khoa tài chính", "bài tập lớn", "nhóm 2", "giảng viên hướng dẫn",
            "trí tuệ nhân tạo", "trong nông nghiệp", "đối với sự phát triển"
        };

        public string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            
            // Sử dụng StringBuilder thay vì Regex để tránh StackOverflow trên chuỗi cực lớn (đặc biệt khi chạy F5 Debug)
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
                    // Thay thế mọi ký tự đặc biệt/khoảng trắng bằng một khoảng trắng duy nhất
                    if (!lastWasSpace)
                    {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                }
            }

            // Xóa khoảng trắng cuối cùng nếu có
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                sb.Length--;

            return sb.ToString();
        }

        /// <summary>
        /// Xử lý văn bản (bí danh cho NormalizeText)
        /// Được sử dụng bởi PlagiarismService
        /// </summary>
        public string Process(string text)
        {
            return NormalizeText(text);
        }

        public string CleanDocument(string text, bool excludeBibliography = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            if (excludeBibliography)
            {
                // TỐI ƯU: Chỉ tìm trong 30% cuối tài liệu để tránh scan toàn bộ và tránh ToLower() cả chuỗi lớn
                int searchStart = (int)(text.Length * 0.7);
                string endPart = text.Substring(searchStart).ToLower();
                
                int bestIndexInPart = -1;
                foreach (var keyword in BibliographyKeywords)
                {
                    int index = endPart.LastIndexOf(keyword);
                    if (index > bestIndexInPart)
                    {
                        bestIndexInPart = index;
                    }
                }

                if (bestIndexInPart != -1)
                {
                    return text.Substring(0, searchStart + bestIndexInPart);
                }
            }

            return text;
        }

        private const int MAX_SEGMENT_CHARS = 3000; // Giới hạn độ dài đoạn văn để an toàn

        public List<TextSegment> SplitIntoSmartSegments(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<TextSegment>();

            var segments = new List<TextSegment>();
            var delimiters = new[] { '.', '!', '?', '\n', '\r' };
            
            int lastPos = 0;
            for (int i = 0; i < text.Length; i++)
            {
                bool isDelimiter = delimiters.Contains(text[i]);
                bool isForceSplit = (i - lastPos) > MAX_SEGMENT_CHARS;
                bool isEnd = i == text.Length - 1;

                // Tìm ranh giới đoạn văn/câu hoặc buộc cắt nếu quá dài
                if (isDelimiter || isForceSplit || isEnd)
                {
                    int length = i - lastPos + 1;
                    string raw = text.Substring(lastPos, length);
                    string clean = NormalizeText(raw);
                    
                    var segment = new TextSegment { RawText = raw, CleanText = clean };

                    if (string.IsNullOrWhiteSpace(clean))
                    {
                        segment.IsNoise = true;
                    }
                    else if (clean.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 3)
                    {
                        segment.IsNoise = true;
                        segment.ExclusionReason = "Đoạn văn quá ngắn";
                    }
                    else
                    {
                        // Kiểm tra cụm từ thông dụng
                        foreach (var phrase in CommonPhrases)
                        {
                            if (clean.Contains(phrase))
                            {
                                segment.IsCommonPhrase = true;
                                segment.IsExcluded = true;
                                segment.ExclusionReason = "Cụm từ thông dụng";
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

        public HashSet<int> GenerateHashedNGrams(string text, int n)
        {
            if (string.IsNullOrEmpty(text)) return new HashSet<int>();
            
            var words = Tokenize(NormalizeText(text));
            var nGrams = new HashSet<int>();

            if (words.Count < n) return nGrams;

            // Sử dụng bộ đệm để tránh tạo chuỗi trung gian
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= words.Count - n; i++)
            {
                sb.Clear();
                for (int j = 0; j < n; j++)
                {
                    if (j > 0) sb.Append(" ");
                    sb.Append(words[i + j]);
                }
                // Sử dụng HashCode của chuỗi kết hợp
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
        public bool IsExcluded { get; set; } = false;
        public string? ExclusionReason { get; set; }
    }
}
