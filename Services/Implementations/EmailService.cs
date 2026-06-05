using MailKit.Net.Smtp;
using MimeKit;
using DentBridge.Services.Interfaces;

namespace DentBridge.Services.Implementations;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 465;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "DentBridge";
    public string FromEmail { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _settings = config.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings();
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.Password))
            _logger.LogWarning("EmailSettings is incomplete — emails will not be sent.");
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = WrapInTemplate(subject, htmlBody) };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port,
                _settings.EnableSsl ? MailKit.Security.SecureSocketOptions.Auto
                                    : MailKit.Security.SecureSocketOptions.None);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: [{Type}] {Message}", to, ex.GetType().Name, ex.Message);
        }
    }

    public Task SendAccountApprovedAsync(string to, string name) =>
        SendAsync(to, "Your DentBridge Account Has Been Approved!",
            $"<p>Dear <strong>{name}</strong>,</p>" +
            "<p>Congratulations! Your student account has been <strong style='color:#10b981'>approved</strong>.</p>" +
            "<p>You can now log in and start accepting dental cases from patients.</p>" +
            "<p>Welcome to DentBridge!</p>");

    public Task SendAccountRejectedAsync(string to, string name, string reason) =>
        SendAsync(to, "DentBridge Account Application Update",
            $"<p>Dear <strong>{name}</strong>,</p>" +
            "<p>We regret to inform you that your student account application was not approved at this time.</p>" +
            $"<p><strong>Reason:</strong> {reason}</p>" +
            "<p>You may reapply with updated documentation. Contact support for assistance.</p>");

    public Task SendCaseAcceptedAsync(string to, string patientName, string caseTitle, string studentName) =>
        SendAsync(to, "Your Dental Case Has Been Accepted!",
            $"<p>Dear <strong>{patientName}</strong>,</p>" +
            $"<p>Great news! Your case <strong>\"{caseTitle}\"</strong> has been accepted by student <strong>{studentName}</strong>.</p>" +
            "<p>They will contact you shortly to arrange your appointment.</p>");

    public Task SendCaseCompletedAsync(string to, string patientName, string caseTitle) =>
        SendAsync(to, "Your Case Has Been Completed",
            $"<p>Dear <strong>{patientName}</strong>,</p>" +
            $"<p>Your dental case <strong>\"{caseTitle}\"</strong> has been marked as completed.</p>" +
            "<p>Please log in to leave a review for your treating student. Your feedback helps the DentBridge community!</p>");

    public Task SendWelcomeEmailAsync(string to, string name) =>
        SendAsync(to, "Welcome to DentBridge!",
            $"<p>Dear <strong>{name}</strong>,</p>" +
            "<p>Welcome to <strong>DentBridge</strong> — connecting dental students with patients who need affordable care.</p>" +
            "<p>Your account is ready. Log in to get started!</p>");

    public Task SendAccountActivatedAsync(string to, string name) =>
        SendAsync(to, "Your DentBridge Account Has Been Activated",
            $"<p>Dear <strong>{name}</strong>,</p>" +
            "<p>Your account has been <strong style='color:#10b981'>reactivated</strong> by an administrator.</p>" +
            "<p>You can now log in and continue using DentBridge.</p>");

    public Task SendAccountDeactivatedAsync(string to, string name) =>
        SendAsync(to, "Your DentBridge Account Has Been Deactivated",
            $"<p>Dear <strong>{name}</strong>,</p>" +
            "<p>Your account has been <strong style='color:#ef4444'>deactivated</strong> by an administrator.</p>" +
            "<p>You will not be able to log in until your account is reactivated. If you believe this is a mistake, please contact support.</p>");

    public async Task SendContactMessageAsync(string senderName, string senderEmail, string subject, string message)
    {
        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(_settings.FromEmail));
            mimeMessage.ReplyTo.Add(new MailboxAddress(senderName, senderEmail));
            mimeMessage.Subject = $"[Contact Form] {subject}";

            var html = $"""
                <p><strong>From:</strong> {senderName} (<a href="mailto:{senderEmail}">{senderEmail}</a>)</p>
                <p><strong>Subject:</strong> {subject}</p>
                <hr style="border:none;border-top:1px solid #e5e7eb;margin:16px 0">
                <p style="white-space:pre-line">{System.Net.WebUtility.HtmlEncode(message)}</p>
                """;

            var bodyBuilder = new BodyBuilder { HtmlBody = WrapInTemplate($"Contact Form Message: {subject}", html) };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port,
                _settings.EnableSsl ? MailKit.Security.SecureSocketOptions.Auto
                                    : MailKit.Security.SecureSocketOptions.None);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact form email from {Sender}: [{Type}] {Message}", senderEmail, ex.GetType().Name, ex.Message);
        }
    }

    private static string WrapInTemplate(string title, string body) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;background:#f8fafc;margin:0;padding:20px">
          <div style="max-width:600px;margin:0 auto;background:white;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08)">
            <div style="background:linear-gradient(135deg,#0ea5e9,#06b6d4);padding:30px;text-align:center">
              <h1 style="color:white;margin:0;font-size:24px">🦷 DentBridge</h1>
              <p style="color:rgba(255,255,255,.85);margin:6px 0 0">{title}</p>
            </div>
            <div style="padding:30px;color:#374151;line-height:1.6">{body}</div>
            <div style="background:#f8fafc;padding:20px;text-align:center;color:#9ca3af;font-size:12px">
              © 2026 DentBridge. All rights reserved.
            </div>
          </div>
        </body>
        </html>
        """;
}
