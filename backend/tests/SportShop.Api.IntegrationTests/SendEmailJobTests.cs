using System.Reflection;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SportShop.Application.Common.Interfaces;
using SportShop.Infrastructure.Jobs;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class SendEmailJobTests
{
    [Fact]
    public void SendEmailJob_ShouldHaveAutomaticRetryAttribute_ConfiguredFor5Attempts()
    {
        var attr = typeof(SendEmailJob).GetCustomAttribute<AutomaticRetryAttribute>();

        attr.Should().NotBeNull();
        attr!.Attempts.Should().Be(5);
        attr.OnAttemptsExceeded.Should().Be(AttemptsExceededAction.Fail);
    }

    [Fact]
    public async Task SendOrderCreatedEmailAsync_ShouldSendFormattedHtmlWithOrderDetails()
    {
        var mockSender = new Mock<IEmailSender>();
        string? capturedRecipient = null;
        string? capturedSubject = null;
        string? capturedBody = null;

        mockSender.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .Callback<string, string, string, bool, CancellationToken>((r, s, b, _, _) =>
            {
                capturedRecipient = r;
                capturedSubject = s;
                capturedBody = b;
            })
            .Returns(Task.CompletedTask);

        var job = new SendEmailJob(mockSender.Object, NullLogger<SendEmailJob>.Instance);

        var expiresAt = new DateTime(2026, 11, 21, 14, 0, 0, DateTimeKind.Utc);
        await job.SendOrderCreatedEmailAsync("user@example.com", "Budi Santoso", "SPT-20261120-0001", 350_000m, expiresAt);

        capturedRecipient.Should().Be("user@example.com");
        capturedSubject.Should().Contain("SPT-20261120-0001");
        capturedBody.Should().Contain("Budi Santoso");
        capturedBody.Should().Contain("SPT-20261120-0001");
        capturedBody.Should().Contain("350.000");
    }

    [Fact]
    public async Task SendOrderPaidEmailAsync_ShouldSendFormattedHtmlWithPaidDetails()
    {
        var mockSender = new Mock<IEmailSender>();
        string? capturedRecipient = null;
        string? capturedSubject = null;
        string? capturedBody = null;

        mockSender.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .Callback<string, string, string, bool, CancellationToken>((r, s, b, _, _) =>
            {
                capturedRecipient = r;
                capturedSubject = s;
                capturedBody = b;
            })
            .Returns(Task.CompletedTask);

        var job = new SendEmailJob(mockSender.Object, NullLogger<SendEmailJob>.Instance);

        var paidAt = new DateTime(2026, 11, 20, 16, 30, 0, DateTimeKind.Utc);
        await job.SendOrderPaidEmailAsync("user@example.com", "Budi Santoso", "SPT-20261120-0001", 350_000m, paidAt);

        capturedRecipient.Should().Be("user@example.com");
        capturedSubject.Should().Contain("Pembayaran Berhasil");
        capturedSubject.Should().Contain("SPT-20261120-0001");
        capturedBody.Should().Contain("Budi Santoso");
        capturedBody.Should().Contain("350.000");
    }

    [Fact]
    public async Task SendOrderShippedEmailAsync_ShouldSendFormattedHtmlWithCourierAndTrackingNumber()
    {
        var mockSender = new Mock<IEmailSender>();
        string? capturedRecipient = null;
        string? capturedSubject = null;
        string? capturedBody = null;

        mockSender.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .Callback<string, string, string, bool, CancellationToken>((r, s, b, _, _) =>
            {
                capturedRecipient = r;
                capturedSubject = s;
                capturedBody = b;
            })
            .Returns(Task.CompletedTask);

        var job = new SendEmailJob(mockSender.Object, NullLogger<SendEmailJob>.Instance);

        var shippedAt = new DateTime(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc);
        await job.SendOrderShippedEmailAsync("user@example.com", "Budi Santoso", "SPT-20261120-0001", "JNE", "JNE123456789ID", shippedAt);

        capturedRecipient.Should().Be("user@example.com");
        capturedSubject.Should().Contain("Sedang Dikirim");
        capturedBody.Should().Contain("JNE");
        capturedBody.Should().Contain("JNE123456789ID");
    }

    [Fact]
    public async Task SendEmailAsync_WhenSmtpThrows_ShouldRethrowExceptionForHangfireRetry()
    {
        var mockSender = new Mock<IEmailSender>();
        mockSender.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionRefused));

        var job = new SendEmailJob(mockSender.Object, NullLogger<SendEmailJob>.Instance);

        var act = async () => await job.SendEmailAsync("user@example.com", "Test Subject", "<p>Test</p>");

        await act.Should().ThrowAsync<System.Net.Sockets.SocketException>();
    }
}
