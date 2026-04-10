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

        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "bị", "bởi", "cả", "các", "cái", "cần", "càng", "chỉ", "chiếc", "cho", "chứ", "chưa", "chuyện", 
            "có", "cứ", "của", "cùng", "cũng", "đã", "đang", "đây", "để", "đều", "điều", 
            "do", "đó", "được", "dưới", "gì", "khi", "không", "là", "lại", "lên", "lúc", "mà", "mỗi", 
            "này", "nên", "nếu", "ngay", "nhiều", "như", "nhưng", "những", "nơi", "nữa", "phải", "qua", "ra", 
            "rằng", "rất", "rồi", "sau", "sẽ", "so", "sự", "tại", "theo", "thì", "trên", "trước", "từ", "từng", 
            "và", "vẫn", "vào", "vậy", "vì", "việc", "với", "vừa"
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

        public bool IsStopWord(string word)
        {
            return StopWords.Contains(word.ToLowerInvariant());
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
            
            // Tìm vị trí bắt đầu của danh mục tham khảo
            int bibStart = -1;
            int searchStart = (int)(text.Length * 0.7);
            if (text.Length > searchStart)
            {
                string endPart = text.Substring(searchStart).ToLower();
                foreach (var keyword in BibliographyKeywords)
                {
                    int index = endPart.LastIndexOf(keyword);
                    if (index != -1)
                    {
                        int absoluteIndex = searchStart + index;
                        if (bibStart == -1 || absoluteIndex > bibStart)
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
                    string clean = NormalizeText(raw);
                    
                    var segment = new TextSegment { RawText = raw, CleanText = clean };

                    // Kiểm tra xem đoạn này có thuộc phần bibliography không
                    if (bibStart != -1 && lastPos >= bibStart)
                    {
                        segment.IsBibliography = true;
                        segment.IsExcluded = true;
                        segment.ExclusionReason = "Loại trừ Mục lục Tham khảo";
                    }

                    if (string.IsNullOrWhiteSpace(clean))
                    {
                        segment.IsNoise = true;
                    }
                    else if (clean.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 3)
                    {
                        segment.IsNoise = true;
                        if (string.IsNullOrEmpty(segment.ExclusionReason))
                            segment.ExclusionReason = "Đoạn văn quá ngắn";
                    }
                    else if (!segment.IsBibliography) // Nếu không phải bib thì mới check common phrase và quote
                    {
                        // Kiểm tra trích dẫn (Quotes)
                        string trimmed = raw.Trim();
                        bool isQuote = (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) ||
                                        (trimmed.StartsWith("“") && trimmed.EndsWith("”")) ||
                                        (trimmed.StartsWith("«") && trimmed.EndsWith("»")) ||
                                        (trimmed.Count(c => c == '\"') >= 2 && trimmed.Length > 20);
                        
                        // Bước 5: Nhận diện trích dẫn APA
                        var apaRegex1 = new Regex(@"\([\p{L}\s\.\,]+,\s\d{4}[a-z]?\)"); // VD: (Nguyen Van A, 2020)
                        var apaRegex2 = new Regex(@"\p{L}[\p{L}\s\.]*\(\d{4}[a-z]?\)"); // VD: Nguyen Van A (2020)

                        bool hasApa = apaRegex1.IsMatch(raw) || apaRegex2.IsMatch(raw);

                        if (isQuote && hasApa)
                        {
                            segment.IsQuote = true;
                            segment.IsExcluded = true;
                            segment.ExclusionReason = "Trích dẫn nguyên văn chuẩn APA";
                        }
                        else if (hasApa)
                        {
                            // Xóa phần citation khỏi CleanText (Chuẩn hóa câu bằng cách loại bỏ ngoặc chứa năm)
                            string processedRaw = apaRegex1.Replace(raw, " ");
                            processedRaw = Regex.Replace(processedRaw, @"\p{L}[\p{L}\s\.]*\(\d{4}[a-z]?\)", " ");
                            clean = NormalizeText(processedRaw);
                            segment.CleanText = clean;
                        }
                        else if (isQuote)
                        {
                            segment.IsQuote = true;
                        }

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
            
            var words = Tokenize(NormalizeText(text)).Where(w => !IsStopWord(w)).ToList();
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
