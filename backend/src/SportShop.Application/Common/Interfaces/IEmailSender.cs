namespace SportShop.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(
        string recipient,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);
}
