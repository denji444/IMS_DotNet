using System.Threading.Tasks;

namespace InventoryManagementSystem.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}
