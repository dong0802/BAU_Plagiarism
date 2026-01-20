using System.Threading.Tasks;

namespace BAU_Plagiarism_System.Core.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendPasswordResetCodeAsync(string to, string code);
    }
}
