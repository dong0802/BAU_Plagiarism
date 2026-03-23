using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NPOI.XWPF.UserModel;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace BAU_Plagiarism_System.Core.Services
{
    public class DocumentReader
    {
        public string ReadText(Stream stream, string fileName)
        {
            if (stream == null || stream.Length == 0) return string.Empty;

            string extension = System.IO.Path.GetExtension(fileName).ToLower();
            Console.WriteLine($"[DocumentReader] Reading file: {fileName} (Ext: {extension})");

            try
            {
                // Copy to memory stream to ensure it's seekable and independent
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;

                string text = string.Empty;
                try 
                {
                    text = extension switch
                    {
                        ".docx" => ReadDocx(ms),
                        ".pdf" => ReadPdf(ms),
                        ".txt" => ReadTxt(ms),
                        _ => string.Empty
                    };
                }
                catch (Exception libEx)
                {
                    Console.WriteLine($"[DocumentReader] Library Error ({extension}): {libEx.Message}");
                    // If it's a PDF error, sometimes it's better to return what we have or empty than to crash
                    text = string.Empty;
                }

                Console.WriteLine($"[DocumentReader] Successfully processed {fileName}. Extracted {text?.Length ?? 0} characters.");
                return text ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocumentReader] CRITICAL ERROR reading {fileName}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extract text from file path asynchronously
        /// Used by DocumentService
        /// </summary>
        public async Task<string> ExtractTextAsync(string filePath)
        {
            try 
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"[DocumentReader] File not found: {filePath}");
                    return string.Empty;
                }

                Console.WriteLine($"[DocumentReader] ExtractTextAsync starting for: {filePath}");
                
                var fileName = System.IO.Path.GetFileName(filePath);
                
                // Chạy với timeout 30 giây để tránh treo
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                try
                {
                    // TỐI ƯU: Đọc trực tiếp từ Stream thay vì mảng byte
                    var result = await Task.Run(() =>
                    {
                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                        return ReadText(fs, fileName);
                    }, cts.Token);
                    
                    Console.WriteLine($"[DocumentReader] ExtractTextAsync completed. Result length: {result?.Length ?? 0}");
                    return result ?? string.Empty;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[DocumentReader] ExtractTextAsync TIMEOUT after 30s for: {filePath}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocumentReader] ExtractTextAsync Failed: {ex.GetType().Name} - {ex.Message}");
                return string.Empty;
            }
        }

        private string ReadDocx(Stream stream)
        {
            try 
            {
                using var doc = new XWPFDocument(stream);
                StringBuilder sb = new StringBuilder();
                foreach (var paragraph in doc.Paragraphs)
                {
                    if (paragraph != null && !string.IsNullOrWhiteSpace(paragraph.Text))
                    {
                        sb.AppendLine(paragraph.Text);
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocumentReader] ReadDocx Error: {ex.Message}");
                return string.Empty;
            }
        }

        private string ReadPdf(Stream stream)
        {
            try 
            {
                stream.Position = 0;
                using (var reader = new PdfReader(stream))
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        var text = PdfTextExtractor.GetTextFromPage(reader, i);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            sb.AppendLine(text);
                        }
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocumentReader] ReadPdf Error: {ex.Message}");
                return string.Empty;
            }
        }

        private string ReadTxt(Stream stream)
        {
            try 
            {
                // Reset position just in case
                if (stream.CanSeek) stream.Position = 0;
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocumentReader] ReadTxt Error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
