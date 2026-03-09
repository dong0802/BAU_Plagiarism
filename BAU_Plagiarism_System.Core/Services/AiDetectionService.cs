using BAU_Plagiarism_System.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BAU_Plagiarism_System.Core.Services
{
    public class AiDetectionService
    {
        private readonly TextProcessor _textProcessor;

        public AiDetectionService(TextProcessor textProcessor)
        {
            _textProcessor = textProcessor;
        }

        public async Task<AiDetectionResultDto> DetectAiAsync(string text)
        {
            // Trong thực tế, AI Detection yêu cầu các model ngôn ngữ lớn (LLM).
            // Ở đây chúng ta nâng cấp thuật toán Heuristic dựa trên 2 chỉ số khoa học:
            // 1. Perplexity (Độ đo mức độ dễ đoán của văn bản)
            // 2. Burstiness (Độ đo mức độ biến thiên cấu trúc câu)
            
            return await DetectAdvancedHeuristicAsync(text);
        }

        private Task<AiDetectionResultDto> DetectAdvancedHeuristicAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) 
                return Task.FromResult(new AiDetectionResultDto());

            // TỐI ƯU: Chỉ lấy mẫu 20,000 ký tự đầu tiên để phân tích AI. 
            // Điều này đủ để có độ chính xác cao mà không làm tràn bộ nhớ (crash) khi chạy F5.
            string samplingText = text.Length > 20000 ? text.Substring(0, 20000) : text;

            var rawSentences = SplitIntoSentences(samplingText);
            var sentenceWords = rawSentences.Select(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList()).ToList();
            
            // 1. Tính Perplexity (Simulated)
            // AI có Perplexity THẤP (dễ đoán). Con người có Perplexity CAO (khó đoán).
            double perplexity = CalculateSimulatedPerplexity(rawSentences);

            // 2. Tính Burstiness (Simulated)
            // Burstiness đo sự biến thiên độ dài câu. AI thấp, Con người cao.
            double burstiness = CalculateSimulatedBurstiness(sentenceWords);

            // 3. Phân tích chi tiết từng câu
            var results = new List<AiSentenceResultDto>();
            double totalScore = 0;

            foreach (var sentence in rawSentences)
            {
                double sentenceProb = CalculateSentenceAiProbability(sentence);
                
                // Điều chỉnh score dựa trên context chung của văn bản
                if (perplexity < 40) sentenceProb += 10;
                if (burstiness < 30) sentenceProb += 10;

                results.Add(new AiSentenceResultDto
                {
                    Text = sentence,
                    AiProbability = Math.Clamp(Math.Round(sentenceProb, 2), 0, 99),
                    IsLikelyAi = sentenceProb > 65
                });

                totalScore += sentenceProb;
            }

            double overallProb = rawSentences.Count > 0 ? totalScore / rawSentences.Count : 0;
            
            // Final adjustments based on metrics
            if (perplexity < 30 && burstiness < 20) overallProb = Math.Max(overallProb, 85);
            if (perplexity > 80 && burstiness > 60) overallProb = Math.Min(overallProb, 15);

            var result = new AiDetectionResultDto
            {
                AiProbability = Math.Round(overallProb, 2),
                Sentences = results,
                Perplexity = Math.Round(perplexity, 2),
                Burstiness = Math.Round(burstiness, 2),
                DetectionLevel = overallProb > 75 ? "High" : (overallProb > 45 ? "Medium" : "Low"),
                Summary = GenerateEnhancedSummary(overallProb, perplexity, burstiness),
                CheckedDate = DateTime.Now
            };

            return Task.FromResult(result);
        }

        private double CalculateSimulatedPerplexity(List<string> sentences)
        {
            // Perplexity thấp = dùng nhiều từ phổ biến, cấu trúc đơn giản (AI)
            // Perplexity cao = dùng từ vựng phong phú, cấu trúc phức tạp (Con người)
            
            if (!sentences.Any()) return 50;

            string[] rarePatterns = { "mặt khác", "trái lại", "về phương diện", "đáng chú ý là", "hệ quả là" };
            double score = 40; // Base score (low = AI-like)

            foreach (var s in sentences)
            {
                // Câu dài và dùng từ chuyển đoạn phức tạp làm tăng perplexity
                if (s.Length > 150) score += 5;
                foreach (var pattern in rarePatterns)
                {
                    if (s.ToLower().Contains(pattern)) score += 3;
                }
                
                // Dấu ngoặc kép, dấu chấm phẩy thường do con người dùng chủ động hơn
                if (s.Contains(";") || s.Contains("\"")) score += 2;
            }

            return Math.Clamp(score, 10, 100);
        }

        private double CalculateSimulatedBurstiness(List<List<string>> sentenceWords)
        {
            if (sentenceWords.Count < 2) return 50;

            // Tính độ lệch chuẩn của độ dài câu
            var lengths = sentenceWords.Select(w => (double)w.Count).ToList();
            double avg = lengths.Average();
            double sumOfSquares = lengths.Select(l => Math.Pow(l - avg, 2)).Sum();
            double stdDev = Math.Sqrt(sumOfSquares / sentenceWords.Count);

            // Burstiness cao = Standard Deviation cao (Con người)
            // Burstiness thấp = Standard Deviation thấp (AI)
            
            return Math.Clamp(stdDev * 5, 5, 100); 
        }

        private double CalculateSentenceAiProbability(string sentence)
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 6) return 20;

            double prob = 40;

            // AI thường không viết sai chính tả, dấu câu luôn chuẩn
            if (char.IsUpper(sentence[0]) && (sentence.EndsWith(".") || sentence.EndsWith("?") || sentence.EndsWith("!")))
                prob += 10;

            // Các từ "hallmark" của AI (mô phỏng)
            string[] aiMarkers = { "tổng quan", "quan trọng là", "có thể thấy rằng", "hơn nữa", "ngoài ra", "tóm lại" };
            foreach (var marker in aiMarkers)
            {
                if (sentence.ToLower().Contains(marker)) prob += 8;
            }

            // AI thường có cấu trúc câu SVO rất chuẩn
            if (words.Length >= 10 && words.Length <= 20) prob += 5;

            return prob;
        }

        private string GenerateEnhancedSummary(double prob, double perp, double burst)
        {
            string context = $"[Chỉ số: Perplexity {perp}/100, Burstiness {burst}/100]. ";
            if (prob > 75) return context + "Hệ thống phát hiện xác suất cao văn bản được tạo bởi trí tuệ nhân tạo (LLM). Văn bản có tính dễ đoán cao và cấu trúc quá đồng nhất.";
            if (prob > 45) return context + "Văn bản có một số dấu hiệu của AI, có thể là sự kết hợp giữa người viết và công cụ hỗ trợ.";
            return context + "Văn bản có vẻ tự nhiên, được viết bởi con người với sự biến thiên ngôn ngữ tốt.";
        }

        private List<string> SplitIntoSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            // Thay thế Regex phức tạp bằng logic tách chuỗi đơn giản để tránh StackOverflow
            var delimiters = new[] { '.', '!', '?' };
            var sentences = text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => s.Length > 10)
                                .ToList();
            
            return sentences;
        }
    }
}
