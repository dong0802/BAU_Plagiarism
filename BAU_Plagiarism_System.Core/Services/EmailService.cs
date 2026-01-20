using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace BAU_Plagiarism_System.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpHost = emailSettings["SmtpHost"];
            var smtpPortString = emailSettings["SmtpPort"];
            var smtpPort = int.TryParse(smtpPortString, out var port) ? port : 587;
            var smtpUser = emailSettings["SmtpUser"];
            var smtpPass = emailSettings["SmtpPass"];
            var fromEmail = emailSettings["FromEmail"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || smtpUser.Contains("your-email"))
            {
                // Development mode: Log to console if not configured or using placeholders
                Console.WriteLine("\n[EMAIL SERVICE - CHẾ ĐỘ DEBUG]");
                Console.WriteLine("-------------------------------------------------");
                Console.WriteLine($"GỬI ĐẾN: {to}");
                Console.WriteLine($"TIÊU ĐỀ: {subject}");
                // Nếu là email khôi phục mật khẩu, in nội dung để lấy mã
                Console.WriteLine($"NỘI DUNG: {body}");
                Console.WriteLine("-------------------------------------------------");
                Console.WriteLine("Lưu ý: Bạn chưa cấu hình Email thật trong appsettings.json nên mã được in tại đây.\n");
                
                // Write to file for easier access by agent/user
                try {
                    File.WriteAllText("reset_code.txt", $"TO: {to}\nCODE_CONTENT:\n{body}");
                } catch {}

                return;
            }

            try 
            {
                var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? smtpUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                // In production, you might want to rethrow or log to a file
            }
        }

        public async Task SendPasswordResetCodeAsync(string to, string code)
        {
            string subject = "Mã xác nhận khôi phục mật khẩu - BAU Plagiarism System";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #003a8c; text-align: center;'>BAU Plagiarism System</h2>
                    <p>Xin chào,</p>
                    <p>Bạn đã yêu cầu khôi phục mật khẩu cho tài khoản của mình trên hệ thống kiểm tra đạo văn BAU.</p>
                    <p>Vui lòng sử dụng mã xác nhận bên dưới để đặt lại mật khẩu của bạn. Mã này có hiệu lực trong 15 phút.</p>
                    <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #1890ff; margin: 20px 0;'>
                        {code}
                    </div>
                    <p>Nếu bạn không yêu cầu thay đổi mật khẩu, vui lòng bỏ qua email này.</p>
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #999; text-align: center;'>Đây là email tự động, vui lòng không phản hồi.</p>
                </div>";

            await SendEmailAsync(to, subject, body);
        }
    }
}
