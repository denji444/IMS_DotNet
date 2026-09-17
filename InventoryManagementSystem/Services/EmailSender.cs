using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models.Configuration;

namespace InventoryManagementSystem.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly InventoryDbContext _dbContext;
        private readonly SmtpSettings _fallbackSmtpSettings;
        private readonly IDataProtector _protector;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(
            InventoryDbContext dbContext,
            IOptions<SmtpSettings> fallbackSmtpSettings,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<EmailSender> logger)
        {
            _dbContext = dbContext;
            _fallbackSmtpSettings = fallbackSmtpSettings.Value;
            _protector = dataProtectionProvider.CreateProtector("InventoryManagementSystem.SmtpProtector");
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var db = await _dbContext.SmtpSettings.AsNoTracking().OrderByDescending(s => s.Id).FirstOrDefaultAsync();
            bool hasDbSmtp = db != null && !string.IsNullOrWhiteSpace(db.Server) && !string.IsNullOrWhiteSpace(db.SenderEmail);

            string server = hasDbSmtp ? db!.Server : _fallbackSmtpSettings.Server;
            int port = hasDbSmtp ? db!.Port : _fallbackSmtpSettings.Port;
            string username = hasDbSmtp ? (db!.Username ?? "") : _fallbackSmtpSettings.Username;
            bool enableSsl = hasDbSmtp ? db!.EnableSsl : _fallbackSmtpSettings.EnableSsl;
            string senderEmail = hasDbSmtp ? db!.SenderEmail : _fallbackSmtpSettings.SenderEmail;
            string senderName = hasDbSmtp
                ? (string.IsNullOrWhiteSpace(db!.SenderName) ? "Inventory Management System" : db!.SenderName)
                : (string.IsNullOrWhiteSpace(_fallbackSmtpSettings.SenderName) ? "Inventory Management System" : _fallbackSmtpSettings.SenderName);

            string password = _fallbackSmtpSettings.Password;
            if (hasDbSmtp && !string.IsNullOrEmpty(db!.Password))
            {
                try { password = _protector.Unprotect(db.Password); }
                catch { password = db.Password; }
            }

            try
            {
                using var client = new SmtpClient(server, port);
                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }
                client.EnableSsl = enableSsl;

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} with subject {Subject} via server {Server}:{Port}", toEmail, subject, server, port);
                throw;
            }
        }
    }
}
