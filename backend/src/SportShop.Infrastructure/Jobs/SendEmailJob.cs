using System.Globalization;
using Hangfire;
using Microsoft.Extensions.Logging;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 5, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public sealed partial class SendEmailJob : ISendEmailJob
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SendEmailJob> _logger;

    public SendEmailJob(IEmailSender emailSender, ILogger<SendEmailJob> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string recipient,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        LogSendingEmail(_logger, recipient, subject);
        await _emailSender.SendEmailAsync(recipient, subject, htmlBody, isHtml: true, cancellationToken);
        LogEmailSent(_logger, recipient, subject);
    }

    public async Task SendOrderCreatedEmailAsync(
        string recipientEmail,
        string recipientName,
        string orderNumber,
        decimal total,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[SportShop] Pesanan Dibuat - {orderNumber}";
        var formattedTotal = string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", total);
        var formattedExpiry = expiresAt.ToString("dd MMMM yyyy HH:mm", CultureInfo.GetCultureInfo("id-ID")) + " UTC";

        var body = $"""
        <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; padding: 24px;">
            <h2 style="color: #0f172a; margin-top: 0;">Halo {recipientName},</h2>
            <p>Terima kasih telah berbelanja di <strong>SportShop</strong>! Pesanan Anda telah berhasil dibuat.</p>
            <div style="background-color: #f8fafc; border-left: 4px solid #3b82f6; padding: 16px; margin: 20px 0; border-radius: 4px;">
                <p style="margin: 4px 0;"><strong>Nomor Pesanan:</strong> {orderNumber}</p>
                <p style="margin: 4px 0;"><strong>Total Pembayaran:</strong> {formattedTotal}</p>
                <p style="margin: 4px 0; color: #b91c1c;"><strong>Batas Pembayaran:</strong> {formattedExpiry}</p>
            </div>
            <p>Harap segera menyelesaikan pembayaran Anda sebelum batas waktu di atas agar pesanan tidak kedaluwarsa.</p>
            <p>Salam hangat,<br><strong>Tim SportShop</strong></p>
        </div>
        """;

        await SendEmailAsync(recipientEmail, subject, body, cancellationToken);
    }

    public async Task SendOrderPaidEmailAsync(
        string recipientEmail,
        string recipientName,
        string orderNumber,
        decimal total,
        DateTime paidAt,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[SportShop] Pembayaran Berhasil Dikonfirmasi - {orderNumber}";
        var formattedTotal = string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", total);
        var formattedPaid = paidAt.ToString("dd MMMM yyyy HH:mm", CultureInfo.GetCultureInfo("id-ID")) + " UTC";

        var body = $"""
        <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; padding: 24px;">
            <h2 style="color: #15803d; margin-top: 0;">Halo {recipientName}, Pembayaran Anda Telah Kami Terima!</h2>
            <p>Pembayaran untuk pesanan Anda telah berhasil diverifikasi oleh sistem SportShop.</p>
            <div style="background-color: #f0fdf4; border-left: 4px solid #22c55e; padding: 16px; margin: 20px 0; border-radius: 4px;">
                <p style="margin: 4px 0;"><strong>Nomor Pesanan:</strong> {orderNumber}</p>
                <p style="margin: 4px 0;"><strong>Total Dibayar:</strong> {formattedTotal}</p>
                <p style="margin: 4px 0;"><strong>Waktu Konfirmasi:</strong> {formattedPaid}</p>
            </div>
            <p>Pesanan Anda saat ini sedang disiapkan oleh tim kami untuk proses pengiriman. Kami akan mengirimkan email pemberitahuan berikutnya beserta nomor resi pengiriman.</p>
            <p>Salam hangat,<br><strong>Tim SportShop</strong></p>
        </div>
        """;

        await SendEmailAsync(recipientEmail, subject, body, cancellationToken);
    }

    public async Task SendOrderShippedEmailAsync(
        string recipientEmail,
        string recipientName,
        string orderNumber,
        string courier,
        string trackingNumber,
        DateTime shippedAt,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[SportShop] Pesanan Anda Sedang Dikirim - {orderNumber}";
        var formattedShipped = shippedAt.ToString("dd MMMM yyyy HH:mm", CultureInfo.GetCultureInfo("id-ID")) + " UTC";

        var body = $"""
        <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; padding: 24px;">
            <h2 style="color: #0369a1; margin-top: 0;">Halo {recipientName}, Pesanan Anda Sedang Menuju Lokasi Anda!</h2>
            <p>Paket pesanan Anda telah diserahkan kepada kurir pengiriman.</p>
            <div style="background-color: #f0f9ff; border-left: 4px solid #0284c7; padding: 16px; margin: 20px 0; border-radius: 4px;">
                <p style="margin: 4px 0;"><strong>Nomor Pesanan:</strong> {orderNumber}</p>
                <p style="margin: 4px 0;"><strong>Kurir:</strong> {courier}</p>
                <p style="margin: 4px 0;"><strong>Nomor Resi:</strong> {trackingNumber}</p>
                <p style="margin: 4px 0;"><strong>Waktu Pengiriman:</strong> {formattedShipped}</p>
            </div>
            <p>Anda dapat memantau status perjalanan paket menggunakan nomor resi di atas pada situs kurir terkait.</p>
            <p>Salam hangat,<br><strong>Tim SportShop</strong></p>
        </div>
        """;

        await SendEmailAsync(recipientEmail, subject, body, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Preparing to send transactional email to {Recipient} with subject '{Subject}'")]
    private static partial void LogSendingEmail(ILogger logger, string recipient, string subject);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transactional email successfully sent to {Recipient} with subject '{Subject}'")]
    private static partial void LogEmailSent(ILogger logger, string recipient, string subject);
}
