using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SportShop.Api.Extensions;
using SportShop.Api.Models;
using SportShop.Application.Features.Payments.Webhook;

namespace SportShop.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public sealed partial class PaymentsController : ControllerBase
{
    private readonly ProcessMidtransWebhookCommandHandler _webhookHandler;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ProcessMidtransWebhookCommandHandler webhookHandler,
        ILogger<PaymentsController> logger)
    {
        _webhookHandler = webhookHandler;
        _logger = logger;
    }

    /// <summary>
    /// Webhook penerimaan notifikasi status pembayaran dari Midtrans.
    /// </summary>
    [HttpPost("midtrans/notification")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WebhookProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SportShopProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HandleMidtransNotification(
        [FromBody] JsonElement rawPayload,
        CancellationToken cancellationToken)
    {
        var rawText = rawPayload.GetRawText();
        var command = new ProcessMidtransWebhookCommand(rawText);
        var result = await _webhookHandler.HandleAsync(command, cancellationToken);

        if (result.IsSuccess && result.Value.Status == "amount_mismatch")
        {
            LogGrossAmountMismatch(_logger, rawText);
        }

        return result.ToActionResult(HttpContext);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Midtrans webhook gross amount mismatch detected! Payload: {Payload}")]
    private static partial void LogGrossAmountMismatch(ILogger logger, string payload);
}
