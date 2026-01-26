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
            // Trong một ứng dụng thực tế, bạn sẽ gọi một API bên ngoài ở đây:
            // return await DetectWithExternalApiAsync(text);
            
            return await DetectSimulatedAsync(text);
        }

        /// <summary>
        /// Một bộ phát hiện AI mô phỏng sử dụng các phương pháp ngôn ngữ học
        /// (Sự nhất quán về độ dài câu, mô phỏng perplexity, v.v.)
        /// </summary>
        private Task<AiDetectionResultDto> DetectSimulatedAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) 
                return Task.FromResult(new AiDetectionResultDto());

            var sentences = SplitIntoSentences(text);
            var results = new List<AiSentenceResultDto>();
            double totalProbability = 0;

            foreach (var sentence in sentences)
            {
                // Heuristic 1: Sự thiếu hụt đa dạng về dấu câu
                // Heuristic 2: Độ dài câu (AI có xu hướng tạo ra các câu có độ dài trung bình)
                // Heuristic 3: Các từ "đệm" phổ biến của AI
                
                double sentenceProb = CalculateSentenceAiProbability(sentence);
                
                results.Add(new AiSentenceResultDto
                {
                    Text = sentence,
                    AiProbability = Math.Round(sentenceProb, 2),
                    IsLikelyAi = sentenceProb > 60
                });

                totalProbability += sentenceProb;
            }

            double overallProb = sentences.Count > 0 ? totalProbability / sentences.Count : 0;
            
            // Bias: Nếu văn bản quá ngắn, độ tin cậy sẽ thấp hơn
            if (text.Length < 200) overallProb *= 0.8;

            var result = new AiDetectionResultDto
            {
                AiProbability = Math.Round(overallProb, 2),
                Sentences = results,
                DetectionLevel = overallProb > 70 ? "High" : (overallProb > 40 ? "Medium" : "Low"),
                Summary = GenerateSummary(overallProb),
                CheckedDate = DateTime.Now
            };

            return Task.FromResult(result);
        }

        private double CalculateSentenceAiProbability(string sentence)
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 5) return 10; // Các câu quá ngắn rất khó để đánh giá

            double prob = 30; // Xác suất cơ bản

            // 1. Sự nhất quán về độ dài câu (Mô phỏng Burstiness thấp)
            if (words.Length > 15 && words.Length < 25) prob += 15;

            // 2. Lạm dụng các từ nối phổ biến trong AI
            string[] aiTransitions = { "tuy nhiên", "do đó", "hơn nữa", "ngoài ra", "tóm lại", "vì vậy" };
            foreach (var word in aiTransitions)
            {
                if (sentence.ToLower().Contains(word)) prob += 5;
            }

            // 3. Mô phỏng ngữ pháp hoàn hảo (Thiếu các lỗi đánh máy/ngôn ngữ không chính thức phổ biến)
            if (!Regex.IsMatch(sentence, @"[a-z]{10,}")) prob += 5; // Các từ dài không có lỗi đánh máy

            return Math.Min(98, prob + new Random().Next(-5, 15));
        }

        private string GenerateSummary(double prob)
        {
            if (prob > 75) return "Độ tin cậy cao: Văn bản có dấu hiệu rõ rệt của việc được tạo bởi AI (ChatGPT/LLM).";
            if (prob > 50) return "Cảnh báo: Có khả năng văn bản được hỗ trợ bởi các công cụ AI.";
            return "Văn bản có vẻ được viết bởi con người (Tự nhiên).";
        }

        private List<string> SplitIntoSentences(string text)
        {
            return Regex.Split(text, @"(?<=[.!?])\s+")
                        .Where(s => s.Length > 10)
                        .ToList();
        }
    }
}
