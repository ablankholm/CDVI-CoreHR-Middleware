using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using NLog;

namespace Lyca2CoreHrApiTask.Services
{
    public class EmailService
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var settings = Properties.Settings.Default;

                log.Info($"Sending email to {email} ...");
                var emailMessage = new MimeMessage();

                emailMessage.From.Add(new MailboxAddress(settings.SmtpDefaultFromName, settings.SmtpDefaultFromEmail));
                emailMessage.To.Add(new MailboxAddress("", email));
                emailMessage.Subject = subject;
                emailMessage.Body = new TextPart("plain") { Text = message };

                using (var client = new SmtpClient())
                {
                    client.LocalDomain = "middleware.lycagroup.com";
                    await client.ConnectAsync(settings.SmtpDomain, settings.SmtpPort, SecureSocketOptions.None).ConfigureAwait(false);
                    await client.SendAsync(emailMessage).ConfigureAwait(false);
                    await client.DisconnectAsync(true).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.Fatal($"Encoutnered exception: {ex.ToString()}");
                throw;
            }

        }
    }
}
