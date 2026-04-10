using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using BAU_Plagiarism_System.Core.DTOs;

namespace BAU_Plagiarism_System.Core.Services
{
    /// <summary>
    /// Dịch vụ tìm kiếm đạo văn trên web (Wikipedia, tin tức, nguồn công khai)
    /// </summary>
    public class WebSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly TextProcessor _textProcessor;
        private readonly SimilarityChecker _similarityChecker;

        public WebSearchService(HttpClient httpClient, TextProcessor textProcessor, SimilarityChecker similarityChecker)
        {
            _httpClient = httpClient;
            _textProcessor = textProcessor;
            _similarityChecker = similarityChecker;
        }

        /// <summary>
        /// Tìm kiếm nội dung tương tự trên web
        /// </summary>
        public async Task<WebSearchResultDto> SearchWebAsync(string content, int maxResults = 10)
        {
            var result = new WebSearchResultDto();
            
            // Trích xuất các cụm từ khóa để tìm kiếm
            var keyPhrases = ExtractSearchQueries(content);
            result.Query = string.Join(", ", keyPhrases.Take(3));
            
            var allSources = new List<WebSourceDto>();
            
            // Tìm kiếm trên Wikipedia (tiếng Việt)
            var wikiSources = await SearchWikipediaAsync(keyPhrases.Take(2).ToList());
            allSources.AddRange(wikiSources);
            
            // Mô phỏng tìm kiếm web (trong thực tế, sử dụng Google Custom Search API hoặc Bing Search API)
            var webSources = SimulateWebSearch(keyPhrases.Take(3).ToList());
            allSources.AddRange(webSources);
            
            // Tính toán điểm số tương đồng
            foreach (var source in allSources)
            {
                source.SimilarityScore = _similarityChecker.CalculateJaccardSimilarity(content, source.Snippet, 3);
            }
            
            // Lọc và sắp xếp theo độ tương đồng
            result.Sources = allSources
                .Where(s => s.SimilarityScore > 20)
                .OrderByDescending(s => s.SimilarityScore)
                .Take(maxResults)
                .ToList();
            
            result.TotalMatches = result.Sources.Count;
            
            return result;
        }

        private List<string> ExtractSearchQueries(string content)
        {
            // Trích xuất các cụm từ ý nghĩa (đơn giản hóa - có thể cải thiện bằng NLP)
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var queries = new List<string>();
            
            foreach (var sentence in sentences.Take(5))
            {
                var words = _textProcessor.Tokenize(sentence);
                if (words.Count >= 5)
                {
                    // Lấy 5-7 từ đầu tiên làm truy vấn
                    queries.Add(string.Join(" ", words.Take(Math.Min(7, words.Count))));
                }
            }
            
            return queries.Take(5).ToList();
        }

        private async Task<List<WebSourceDto>> SearchWikipediaAsync(List<string> queries)
        {
            var sources = new List<WebSourceDto>();
            
            foreach (var query in queries)
            {
                try
                {
                    var encodedQuery = HttpUtility.UrlEncode(query);
                    var url = $"https://vi.wikipedia.org/w/api.php?action=opensearch&search={encodedQuery}&limit=3&format=json";
                    
                    var response = await _httpClient.GetStringAsync(url);
                    var results = JsonSerializer.Deserialize<JsonElement>(response);
                    
                    if (results.ValueKind == JsonValueKind.Array && results.GetArrayLength() >= 4)
                    {
                        var titles = results[1];
                        var descriptions = results[2];
                        var urls = results[3];
                        
                        for (int i = 0; i < titles.GetArrayLength(); i++)
                        {
                            sources.Add(new WebSourceDto
                            {
                                Title = titles[i].GetString() ?? "",
                                Snippet = descriptions[i].GetString() ?? "",
                                Url = urls[i].GetString() ?? "",
                                SourceType = "Wikipedia"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi tìm kiếm Wikipedia: {ex.Message}");
                }
            }
            
            return sources;
        }

        private List<WebSourceDto> SimulateWebSearch(List<string> queries)
        {
            // Trong thực tế, thay thế phần này bằng Google Custom Search API hoặc Bing Search API thực tế
            // Hiện tại, chúng tôi mô phỏng với các nguồn học thuật phổ biến ở Việt Nam
            
            var simulatedSources = new List<WebSourceDto>
            {
                new WebSourceDto
                {
                    Title = "Tạp chí Khoa học - Đại học Quốc gia Hà Nội",
                    Url = "https://js.vnu.edu.vn",
                    Snippet = "Nghiên cứu về quản trị rủi ro tín dụng tại các ngân hàng thương mại Việt Nam...",
                    SourceType = "Journal"
                },
                new WebSourceDto
                {
                    Title = "VnExpress - Kinh tế",
                    Url = "https://vnexpress.net/kinh-doanh",
                    Snippet = "Phân tích hiệu quả hoạt động kinh doanh của các ngân hàng thương mại...",
                    SourceType = "News"
                },
                new WebSourceDto
                {
                    Title = "Thư viện số Học viện Ngân hàng",
                    Url = "https://lib.bau.edu.vn",
                    Snippet = "Tài liệu nghiên cứu về tài chính ngân hàng và quản trị rủi ro...",
                    SourceType = "Web"
                }
            };
            
            // Lọc dựa trên mức độ liên quan của truy vấn (đơn giản hóa)
            return simulatedSources.Where(s => 
                queries.Any(q => s.Snippet.ToLower().Contains(q.ToLower().Split(' ').FirstOrDefault() ?? ""))
            ).ToList();
        }

        /// <summary>
        /// Kiểm tra xem nội dung có tồn tại trên các cơ sở dữ liệu học thuật cụ thể hay không
        /// </summary>
        public async Task<bool> CheckAcademicDatabasesAsync(string content)
        {
            // Phần giữ chỗ cho việc tích hợp trong tương lai với:
            // - Google Scholar
            // - ResearchGate
            // - Academia.edu
            // - Các cơ sở dữ liệu học thuật Việt Nam
            
            await Task.Delay(100); // Mô phỏng cuộc gọi API
            return false;
        }
    }
}
