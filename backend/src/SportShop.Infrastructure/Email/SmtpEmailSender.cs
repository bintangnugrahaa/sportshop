using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Options;

namespace SportShop.Infrastructure.Email;

public sealed partial class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string recipient,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port);

            if (!string.IsNullOrEmpty(_options.User))
            {
                client.Credentials = new NetworkCredential(_options.User, _options.Password);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_options.From, "SportShop"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(recipient);

            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
            LogEmailSent(_logger, recipient, subject);
        }
        catch (Exception ex)
        {
            LogEmailFailed(_logger, recipient, subject, ex.Message, ex);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email successfully sent to {Recipient} with subject '{Subject}'.")]
    private static partial void LogEmailSent(ILogger logger, string recipient, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send email to {Recipient} with subject '{Subject}'. Error: {Error}")]
    private static partial void LogEmailFailed(ILogger logger, string recipient, string subject, string error, Exception ex);
}
