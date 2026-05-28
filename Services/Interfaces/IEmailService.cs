namespace DentBridge.Services.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendAccountApprovedAsync(string to, string name);
    Task SendAccountRejectedAsync(string to, string name, string reason);
    Task SendCaseAcceptedAsync(string to, string patientName, string caseTitle, string studentName);
    Task SendCaseCompletedAsync(string to, string patientName, string caseTitle);
    Task SendWelcomeEmailAsync(string to, string name);
}
