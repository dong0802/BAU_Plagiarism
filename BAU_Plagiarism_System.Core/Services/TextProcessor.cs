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
            "trên cơ sở đó", "có thể thấy rằng", "hệ thống ngân hàng thương mại"
        };

        public string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.ToLower();
            text = Regex.Replace(text, @"[^\p{L}\p{N}\s]", " ");
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
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
                // Tìm sự xuất hiện đầu tiên của từ khóa danh mục tham khảo gần cuối tài liệu
                int bestIndex = -1;
                foreach (var keyword in BibliographyKeywords)
                {
                    int index = text.ToLower().LastIndexOf(keyword);
                    // Thông thường danh mục tham khảo nằm ở 20% cuối của tài liệu
                    if (index > text.Length * 0.7 && index > bestIndex)
                    {
                        bestIndex = index;
                    }
                }

                if (bestIndex != -1)
                {
                    return text.Substring(0, bestIndex);
                }
            }

            return text;
        }

        public List<TextSegment> SplitIntoSmartSegments(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<TextSegment>();

            // Chia thành các câu nhưng vẫn giữ lại văn bản gốc để hiển thị
            var segments = new List<TextSegment>();
            // Chia theo dấu câu nhưng giữ lại dấu đó
            var matches = Regex.Matches(text, @"[^.!?\n]+[.!?\n]*|[\n]+");

            foreach (Match match in matches)
            {
                string raw = match.Value;
                string clean = NormalizeText(raw);
                
                var segment = new TextSegment { RawText = raw, CleanText = clean };

                if (string.IsNullOrWhiteSpace(clean))
                {
                    segment.IsNoise = true;
                }
                else if (clean.Split(' ').Length < 3)
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
            }

            return segments;
        }

        public List<string> Tokenize(string text)
        {
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public HashSet<string> GenerateNGrams(string text, int n)
        {
            var words = Tokenize(NormalizeText(text));
            var nGrams = new HashSet<string>();

            for (int i = 0; i <= words.Count - n; i++)
            {
                var gram = string.Join(" ", words.GetRange(i, n));
                nGrams.Add(gram);
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
