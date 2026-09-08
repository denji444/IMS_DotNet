using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Models.Configuration;

namespace InventoryManagementSystem.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpSettings _fallbackSmtpSettings;
        private readonly IServiceProvider _serviceProvider;

        public EmailSender(IOptions<SmtpSettings> fallbackSmtpSettings, IServiceProvider serviceProvider)
        {
            _fallbackSmtpSettings = fallbackSmtpSettings.Value;
            _serviceProvider = serviceProvider;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            string server;
            int port;
            string username;
            string password;
            bool enableSsl;
            string senderEmail;
            string senderName;

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                var dbSmtp = await dbContext.SmtpSettings.OrderByDescending(s => s.Id).FirstOrDefaultAsync();

                if (dbSmtp != null && !string.IsNullOrWhiteSpace(dbSmtp.Server) && !string.IsNullOrWhiteSpace(dbSmtp.SenderEmail))
                {
                    server = dbSmtp.Server;
                    port = dbSmtp.Port;
                    username = dbSmtp.Username ?? string.Empty;
                    password = dbSmtp.Password ?? string.Empty;
                    enableSsl = dbSmtp.EnableSsl;
                    senderEmail = dbSmtp.SenderEmail;
                    senderName = string.IsNullOrWhiteSpace(dbSmtp.SenderName) ? "Inventory App" : dbSmtp.SenderName;
                }
                else
                {
                    server = _fallbackSmtpSettings.Server;
                    port = _fallbackSmtpSettings.Port;
                    username = _fallbackSmtpSettings.Username;
                    password = _fallbackSmtpSettings.Password;
                    enableSsl = _fallbackSmtpSettings.EnableSsl;
                    senderEmail = _fallbackSmtpSettings.SenderEmail;
                    senderName = string.IsNullOrWhiteSpace(_fallbackSmtpSettings.SenderName) ? "Inventory App" : _fallbackSmtpSettings.SenderName;
                }
            }

            using (var client = new SmtpClient(server, port))
            {
                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }
                client.EnableSsl = enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
