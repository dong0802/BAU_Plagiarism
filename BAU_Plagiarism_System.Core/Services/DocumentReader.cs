using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NPOI.XWPF.UserModel;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace BAU_Plagiarism_System.Core.Services
{
    /// <summary>
    /// Bước 1 + Bước 2: Tiếp nhận tài liệu và Trích xuất nội dung văn bản
    /// 
    /// Bước 2 yêu cầu:
    ///  - Đọc nội dung từ file (.doc, .docx, .pdf, .txt)
    ///  - Chuyển đổi toàn bộ sang dạng "văn bản thuần" (plain text)
    ///  - Loại bỏ các yếu tố không cần thiết: định dạng, hình ảnh, header/footer
    ///  - Giữ lại toàn bộ nội dung chữ có nghĩa (kể cả trong bảng)
    /// </summary>
    public class DocumentReader
    {
        // Regex loại bỏ ký tự điều khiển và ký hiệu lạ (bullet, hình ảnh placeholder...)
        private static readonly Regex ControlCharsRegex = new Regex(
            @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F\uFFFD\uFFFC\uFFFB\u0000]",
            RegexOptions.Compiled);

        // Regex chuẩn hóa khoảng trắng thừa
        private static readonly Regex MultipleSpacesRegex = new Regex(
            @"[ \t]{2,}", RegexOptions.Compiled);

        // Regex loại dòng trống liên tiếp (> 2 dòng trống liền nhau)
        private static readonly Regex MultipleNewlinesRegex = new Regex(
            @"(\r?\n){3,}", RegexOptions.Compiled);

        /// <summary>
        /// Bước 1: Phân loại định dạng file và điều hướng sang bộ đọc phù hợp
        /// Bước 2: Trả về văn bản thuần, đã làm sạch
        /// </summary>
        public string ReadText(Stream stream, string fileName)
        {
            if (stream == null || stream.Length == 0) return string.Empty;

            string extension = System.IO.Path.GetExtension(fileName).ToLower();
            Console.WriteLine($"[Step1-2] Đọc file: {fileName} (Định dạng: {extension})");

            try
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;

                string rawText = string.Empty;
                try
                {
                    // Bước 2: Đọc và chuyển sang văn bản thuần theo từng định dạng
                    rawText = extension switch
                    {
                        ".doc"  => ReadDoc(ms),    // Word cũ (OLE2)
                        ".docx" => ReadDocx(ms),   // Word mới (Open XML)
                        ".pdf"  => ReadPdf(ms),    // PDF
                        ".txt"  => ReadTxt(ms),    // Plain text
                        _ => string.Empty
                    };
                }
                catch (Exception libEx)
                {
                    Console.WriteLine($"[Step2] Lỗi đọc file ({extension}): {libEx.Message}");
                    rawText = string.Empty;
                }

                // Bước 2: Làm sạch text sau khi extract — loại bỏ định dạng, ký tự lạ
                string plainText = PostProcessPlainText(rawText);

                Console.WriteLine($"[Step2] Hoàn tất. Trích xuất {plainText.Length} ký tự từ {fileName}");
                return plainText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Step2] LỖI NGHIÊM TRỌNG khi đọc {fileName}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Bước 2 - Làm sạch sau extract:
        /// Loại bỏ ký tự điều khiển, định dạng ẩn, placeholder hình ảnh,
        /// chuẩn hóa khoảng trắng và xuống dòng.
        /// </summary>
        private string PostProcessPlainText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

            // 1. Loại bỏ ký tự điều khiển (control characters) và placeholder hình ảnh
            string text = ControlCharsRegex.Replace(rawText, " ");

            // 2. Chuẩn hóa xuống dòng (Windows \r\n → \n)
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            // 3. Loại bỏ khoảng trắng thừa trong cùng một dòng
            text = MultipleSpacesRegex.Replace(text, " ");

            // 4. Giảm số dòng trống liên tiếp (tránh file có quá nhiều dòng trống)
            text = MultipleNewlinesRegex.Replace(text, "\n\n");

            // 5. Loại bỏ tab character
            text = text.Replace("\t", " ");

            return text.Trim();
        }

        /// <summary>
        /// ExtractTextAsync — Entry point bất đồng bộ từ DocumentService
        /// </summary>
        public async Task<string> ExtractTextAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"[Step2] Không tìm thấy file: {filePath}");
                    return string.Empty;
                }

                Console.WriteLine($"[Step2] Bắt đầu trích xuất: {filePath}");

                var fileName = System.IO.Path.GetFileName(filePath);

                // Timeout 30 giây để tránh treo với file lớn
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                try
                {
                    var extractionTask = Task.Run(() =>
                    {
                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                            FileShare.Read, 4096, useAsync: true);
                        return ReadText(fs, fileName);
                    }, cts.Token);

                    var delayTask = Task.Delay(TimeSpan.FromSeconds(30), cts.Token);

                    var completedTask = await Task.WhenAny(extractionTask, delayTask);
                    
                    if (completedTask == delayTask)
                    {
                        // Timeout occurred
                        Console.WriteLine($"[Step2] TIMEOUT sau 30s cho: {filePath}");
                        return string.Empty;
                    }

                    // Task.Run completed successfully
                    cts.Cancel(); // Cancel the delay task
                    var result = await extractionTask;

                    Console.WriteLine($"[Step2] Hoàn thành. Độ dài: {result?.Length ?? 0} ký tự");
                    return result ?? string.Empty;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[Step2] TIMEOUT sau 30s cho: {filePath}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Step2] Thất bại: {ex.GetType().Name} - {ex.Message}");
                return string.Empty;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Bước 2: Các bộ đọc theo định dạng — chỉ lấy TEXT, bỏ định dạng
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Bước 2: Đọc file .doc (Word cũ - OLE2 binary format)
        /// Thử NPOI HWPF qua reflection; fallback về binary text scan
        /// </summary>
        private string ReadDoc(Stream stream)
        {
            try
            {
                // Thử dùng NPOI HWPFDocument qua reflection (nếu runtime hỗ trợ)
                try
                {
                    var hwpfType = Type.GetType("NPOI.HWPF.HWPFDocument, NPOI.Core");
                    if (hwpfType != null)
                    {
                        var doc = Activator.CreateInstance(hwpfType, stream);
                        var extractorType = Type.GetType("NPOI.HWPF.Extractor.WordExtractor, NPOI.Core");
                        if (extractorType != null && doc != null)
                        {
                            var extractor = Activator.CreateInstance(extractorType, doc);
                            var textProp = extractorType.GetProperty("Text");
                            if (textProp != null)
                                return textProp.GetValue(extractor)?.ToString() ?? string.Empty;
                        }
                    }
                }
                catch { /* HWPF không khả dụng, dùng fallback */ }

                // Fallback: scan binary để tìm chuỗi Unicode (UTF-16 LE)
                return ExtractTextFromDocBinary(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Step2-Doc] Lỗi: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Fallback cho .doc: scan byte stream, trích chuỗi Unicode UTF-16 LE
        /// Lấy được text tiếng Việt (Unicode) từ file .doc nhị phân
        /// </summary>
        private string ExtractTextFromDocBinary(Stream stream)
        {
            if (stream.CanSeek) stream.Position = 0;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length - 2; i++)
            {
                // UTF-16 LE pattern: byte[i] là char, byte[i+1] = 0x00
                if (bytes[i + 1] == 0x00 && bytes[i] >= 0x20 && bytes[i] < 0x7F)
                {
                    var unicodeSb = new StringBuilder();
                    int j = i;
                    while (j < bytes.Length - 1 && bytes[j + 1] == 0x00 &&
                           (bytes[j] >= 0x20 || bytes[j] == 0x0D || bytes[j] == 0x0A))
                    {
                        unicodeSb.Append((char)bytes[j]);
                        j += 2;
                    }
                    string chunk = unicodeSb.ToString().Trim();
                    if (chunk.Length >= 3)
                    {
                        sb.Append(chunk);
                        sb.Append(' ');
                        i = j - 1;
                    }
                }
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Bước 2: Đọc file .docx (Word mới - Open XML)
        /// Lấy text từ: paragraphs (nội dung chính) + tables (bảng dữ liệu)
        /// Bỏ qua: định dạng (bold/italic/color), hình ảnh, header, footer
        /// </summary>
        private string ReadDocx(Stream stream)
        {
            try
            {
                using var doc = new XWPFDocument(stream);
                var sb = new StringBuilder();

                // 1. Đọc tất cả paragraph (nội dung chính)
                foreach (var paragraph in doc.Paragraphs)
                {
                    if (paragraph != null && !string.IsNullOrWhiteSpace(paragraph.Text))
                    {
                        sb.AppendLine(paragraph.Text);
                    }
                }

                // 2. Bước 2: Đọc nội dung trong bảng (tables) — thường bị bỏ sót
                foreach (var table in doc.Tables)
                {
                    foreach (var row in table.Rows)
                    {
                        foreach (var cell in row.GetTableCells())
                        {
                            foreach (var para in cell.Paragraphs)
                            {
                                string cellText = para.Text?.Trim() ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(cellText))
                                    sb.AppendLine(cellText);
                            }
                        }
                    }
                }

                // Hình ảnh (XWPFPicture) và định dạng (runs) bị loại bỏ tự động —
                // chúng ta chỉ lấy .Text property = văn bản thuần

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Step2-Docx] Lỗi: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Bước 2: Đọc file .pdf
        /// iTextSharp chỉ extract text, bỏ qua hình ảnh và layout
        /// </summary>
        private string ReadPdf(Stream stream)
        {
            try
            {
                stream.Position = 0;
                using var reader = new PdfReader(stream);
                var sb = new StringBuilder();

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    var text = PdfTextExtractor.GetTextFromPage(reader, i);
                    if (!string.IsNullOrWhiteSpace(text))
                        sb.AppendLine(text);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Step2-Pdf] Lỗi: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Bước 2: Đọc file .txt thuần — đã là plain text, chỉ cần decode UTF-8
        /// </summary>
        private string ReadTxt(Stream stream)
        {
            try
            {
                if (stream.CanSeek) stream.Position = 0;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Step2-Txt] Lỗi: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
