using Microsoft.Extensions.Logging;
using Onyx.IdP.Core.Interfaces;

namespace Onyx.IdP.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("Sending email to {Email} with subject {Subject}: {Message}", email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}
