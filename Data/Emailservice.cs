using System.Net;
using System.Net.Mail;

namespace EventManagementSystem.Data;

/// <summary>
/// Sends transactional emails via Gmail SMTP. Configure under "EmailSettings" in appsettings.json:
/// SenderEmail (your Gmail address), SenderPassword (a 16-character Gmail App Password — NOT your
/// normal Gmail password; requires 2-Step Verification enabled on the account), and SenderName.
/// If SenderEmail/SenderPassword are left as placeholders, sending is skipped and a warning is logged
/// instead of throwing, so the app keeps working even before email is configured.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string body)
    {
        var senderEmail = _config["EmailSettings:SenderEmail"];
        var senderPassword = _config["EmailSettings:SenderPassword"];
        var senderName = _config["EmailSettings:SenderName"] ?? "SLIC Life Events";
        var host = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
        var port = int.TryParse(_config["EmailSettings:SmtpPort"], out var p) ? p : 587;

        if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword)
            || senderEmail == "CHANGE_ME" || senderPassword == "CHANGE_ME")
        {
            _logger.LogWarning("Email not sent to {ToEmail}: EmailSettings is not configured yet.", toEmail);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Email is best-effort — a failed send should never break the calling page (registration, check-in, etc.).
            _logger.LogWarning(ex, "Failed to send email to {ToEmail}.", toEmail);
        }
    }
}
