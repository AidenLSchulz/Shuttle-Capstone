using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace MidStateShuttleService.Services
{
    /// <summary>
    /// Handles outbound email delivery via SMTP2GO for both general and admin-targeted messages.
    /// SMTP credentials and the admin email address are read from application configuration.
    /// </summary>
    public class EmailServices
    {
        private readonly IConfiguration _config;

        public EmailServices(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Sends an email to the specified recipient via SMTP2GO.
        /// </summary>
        /// <param name="recipient">The destination email address.</param>
        /// <param name="subject">The email subject line.</param>
        /// <param name="body">The email body content.</param>
        /// <param name="isHtml">When <c>true</c>, the body is rendered as HTML. Defaults to <c>false</c>.</param>
        public void SendEmail(string recipient, string subject, string body, bool isHtml = false)
        {
            // Configure the SMTP2GO client with credentials from application settings.
            SmtpClient client = new SmtpClient("mail.smtp2go.com")
            {
                Port = 2525,
                Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"]),
                EnableSsl = true
            };

            MailMessage message = new MailMessage("shuttle@mstc.edu", recipient)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            // DEV NOTE: This call is not awaited — failures will not be caught or logged.
            // Consider awaiting this and making the method async, or wrapping in a try/catch.
            client.SendMailAsync(message);
        }

        /// <summary>
        /// Sends an email to the configured admin address.
        /// The admin email is read from <c>AdminSettings:AdminEmail</c> in application configuration.
        /// </summary>
        /// <param name="subject">The email subject line.</param>
        /// <param name="body">The email body content.</param>
        /// <param name="isHtml">When <c>true</c>, the body is rendered as HTML.</param>
        /// <remarks>
        public void SendEmailToAdmin(string subject, string body, bool isHtml)
        {
            try
            {
                string adminEmail = _config["AdminSettings:AdminEmail"];

                // Silently return if the admin email address is not configured.
                if (adminEmail != null && adminEmail != "")
                {
                    SendEmail(adminEmail, subject, body, isHtml);
                    return;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}