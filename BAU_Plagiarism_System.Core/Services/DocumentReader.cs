using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NPOI.XWPF.UserModel;
using UglyToad.PdfPig;

namespace BAU_Plagiarism_System.Core.Services
{
    public class DocumentReader
    {
        public string ReadText(Stream stream, string fileName)
        {
            if (stream == null || stream.Length == 0) return string.Empty;

            // Đảm bảo stream có thể đọc lại (seekable) để trích xuất dữ liệu
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            string extension = Path.GetExtension(fileName).ToLower();
            Console.WriteLine($"[DocumentReader] Reading file: {fileName} (Ext: {extension})");

            try
            {
                string text = extension switch
                {
                    ".docx" => ReadDocx(ms),
                    ".pdf" => ReadPdf(ms),
                    ".txt" => ReadTxt(ms),
                    _ => string.Empty
                };

                Console.WriteLine($"[DocumentReader] Successfully extracted {text.Length} characters.");
                return text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocumentReader] ERROR reading {fileName}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extract text from file path asynchronously
        /// Used by DocumentService
        /// </summary>
        public async Task<string> ExtractTextAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            using var stream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            
            return await Task.Run(() => ReadText(stream, fileName));
        }

        private string ReadDocx(Stream stream)
        {
            XWPFDocument doc = new XWPFDocument(stream);
            StringBuilder sb = new StringBuilder();
            foreach (var paragraph in doc.Paragraphs)
            {
                sb.AppendLine(paragraph.Text);
            }
            return sb.ToString();
        }

        private string ReadPdf(Stream stream)
        {
            using (PdfDocument document = PdfDocument.Open(stream))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
                return sb.ToString();
            }
        }

        private string ReadTxt(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                // Note: StreamReader with MemoryStream needs to be careful about disposing, 
                // but since we return the string, it's fine.
                return reader.ReadToEnd();
            }
        }
    }
}
