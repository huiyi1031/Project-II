using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PropertyManagement.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendActivationEmailAsync(string toEmail, string name, string tempPw, string role);
        Task SendPasswordResetEmailAsync(string toEmail, string tempPw);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"] ?? "587");
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"] ?? "Property Management System";
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                // If not configured, just log it (useful for local dev without real credentials)
                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning($"SMTP settings are missing. Email to {toEmail} was not sent. Content: {subject} | {body}");
                    return;
                }

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? username, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email successfully sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
            }
        }

        public async Task SendActivationEmailAsync(string toEmail, string name, string tempPw, string role)
        {
            var appUrl = _configuration["AppUrl"] ?? "http://localhost:4201";
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial, sans-serif; background:#f5f9ff; padding:20px;'>
  <div style='max-width:560px; margin:0 auto; background:#fff; border-radius:16px; border:1px solid #dbe7fb; overflow:hidden;'>
    <div style='background:linear-gradient(135deg,#0b2d5c,#1f5fae); padding:32px; color:#fff; text-align:center;'>
      <h1 style='margin:0; font-size:22px;'>🏢 Property Management System</h1>
      <p style='margin:8px 0 0; color:#cde0ff; font-size:14px;'>Account Activation</p>
    </div>
    <div style='padding:32px;'>
      <h2 style='color:#0b2d5c; margin-top:0;'>Welcome, {name}!</h2>
      <p style='color:#3d546e;'>Your <strong>{role}</strong> account has been created by the Property Management Office.</p>
      <p style='color:#3d546e;'>Use the credentials below to log in for the first time. You will be required to set a new permanent password immediately.</p>
      
      <div style='background:#eaf3ff; border-radius:12px; padding:20px; margin:20px 0; border-left:4px solid #1f5fae;'>
        <p style='margin:0 0 8px; color:#6b7a90; font-size:12px; text-transform:uppercase; letter-spacing:1px;'>Your Login Credentials</p>
        <p style='margin:4px 0;'><strong>Email:</strong> {toEmail}</p>
        <p style='margin:4px 0;'><strong>Temporary Password:</strong> <code style='background:#fff; padding:2px 8px; border-radius:6px; font-size:16px; border:1px solid #dbe7fb;'>{tempPw}</code></p>
      </div>
      
      <div style='background:#fff8e1; border-radius:12px; padding:16px; border-left:4px solid #e2a400;'>
        <p style='margin:0; color:#7a5c00; font-size:13px;'>⚠️ This temporary password is only valid for your next login. Please set a new permanent password immediately after logging in.</p>
      </div>
      
      <div style='margin-top:24px; text-align:center;'>
        <a href='{appUrl}/auth/login' 
           style='display:inline-block; background:linear-gradient(135deg,#1f5fae,#2f7de0); color:#fff; padding:12px 32px; border-radius:10px; text-decoration:none; font-weight:600; font-size:15px;'>
          Login Now →
        </a>
      </div>
    </div>
    <div style='background:#f5f9ff; padding:16px; text-align:center; font-size:11px; color:#6b7a90;'>
      Property Management System &mdash; Do not share your credentials with anyone.
    </div>
  </div>
</body>
</html>";
            await SendEmailAsync(toEmail, "Welcome to the Property Management System - Account Activation", body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string tempPw)
        {
            var appUrl = _configuration["AppUrl"] ?? "http://localhost:4201";
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial, sans-serif; background:#f5f9ff; padding:20px;'>
  <div style='max-width:560px; margin:0 auto; background:#fff; border-radius:16px; border:1px solid #dbe7fb; overflow:hidden;'>
    <div style='background:linear-gradient(135deg,#0b2d5c,#1f5fae); padding:32px; color:#fff; text-align:center;'>
      <h1 style='margin:0; font-size:22px;'>🏢 Property Management System</h1>
      <p style='margin:8px 0 0; color:#cde0ff; font-size:14px;'>Password Reset</p>
    </div>
    <div style='padding:32px;'>
      <h2 style='color:#0b2d5c; margin-top:0;'>Password Reset Request</h2>
      <p style='color:#3d546e;'>We received a request to reset the password for your account.</p>
      <p style='color:#3d546e;'>Use the credentials below to log in. You will be required to set a new permanent password immediately.</p>
      
      <div style='background:#eaf3ff; border-radius:12px; padding:20px; margin:20px 0; border-left:4px solid #1f5fae;'>
        <p style='margin:0 0 8px; color:#6b7a90; font-size:12px; text-transform:uppercase; letter-spacing:1px;'>Your Login Credentials</p>
        <p style='margin:4px 0;'><strong>Email:</strong> {toEmail}</p>
        <p style='margin:4px 0;'><strong>Temporary Password:</strong> <code style='background:#fff; padding:2px 8px; border-radius:6px; font-size:16px; border:1px solid #dbe7fb;'>{tempPw}</code></p>
      </div>
      
      <div style='background:#fff8e1; border-radius:12px; padding:16px; border-left:4px solid #e2a400;'>
        <p style='margin:0; color:#7a5c00; font-size:13px;'>⚠️ This temporary password is only valid for your next login. Please set a new permanent password immediately after logging in.</p>
      </div>
      
      <div style='margin-top:24px; text-align:center;'>
        <a href='{appUrl}/auth/login' 
           style='display:inline-block; background:linear-gradient(135deg,#1f5fae,#2f7de0); color:#fff; padding:12px 32px; border-radius:10px; text-decoration:none; font-weight:600; font-size:15px;'>
          Login Now →
        </a>
      </div>
    </div>
    <div style='background:#f5f9ff; padding:16px; text-align:center; font-size:11px; color:#6b7a90;'>
      Property Management System &mdash; If you did not request this, please contact the property management office.
    </div>
  </div>
</body>
</html>";
            await SendEmailAsync(toEmail, "Property Management System - Password Reset", body);
        }
    }
}
