using System;
using System.Collections.Generic;

namespace BAU_Plagiarism_System.Core.DTOs
{
    public class AiDetectionResultDto
    {
        public double AiProbability { get; set; } // 0 to 100
        public string DetectionLevel { get; set; } = "Low"; // Low, Medium, High
        public List<AiSentenceResultDto> Sentences { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public DateTime CheckedDate { get; set; } = DateTime.Now;
    }

    public class AiSentenceResultDto
    {
        public string Text { get; set; } = string.Empty;
        public double AiProbability { get; set; }
        public bool IsLikelyAi { get; set; }
    }
}
