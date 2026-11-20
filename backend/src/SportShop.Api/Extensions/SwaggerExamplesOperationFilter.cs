using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SportShop.Api.Extensions;

public class SwaggerExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? string.Empty;
        var httpMethod = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;

        // Checkout example
        if (httpMethod == "POST" && path.Contains("orders/checkout", StringComparison.OrdinalIgnoreCase))
        {
            if (operation.RequestBody?.Content != null && operation.RequestBody.Content.TryGetValue("application/json", out var requestContent))
            {
                requestContent.Example = new OpenApiObject
                {
                    ["addressId"] = new OpenApiString("0193457a-1234-7890-abcd-ef0123456789")
                };
            }

            if (operation.Responses.TryGetValue("201", out var response201) && response201.Content.TryGetValue("application/json", out var responseContent))
            {
                responseContent.Example = new OpenApiObject
                {
                    ["id"] = new OpenApiString("0193457a-5678-7890-abcd-ef0123456789"),
                    ["orderNumber"] = new OpenApiString("SPT-20261120-000001"),
                    ["status"] = new OpenApiString("PendingPayment"),
                    ["subtotal"] = new OpenApiDouble(500000.0),
                    ["shippingFee"] = new OpenApiDouble(20000.0),
                    ["total"] = new OpenApiDouble(520000.0),
                    ["totalWeightGram"] = new OpenApiInteger(1000),
                    ["snapToken"] = new OpenApiString("d4e2d36a-2001-4475-a0bc-9b2f2939bc99"),
                    ["snapRedirectUrl"] = new OpenApiString("https://app.sandbox.midtrans.com/snap/v2/vtweb/d4e2d36a-2001-4475-a0bc-9b2f2939bc99"),
                    ["paymentStatus"] = new OpenApiString("pending")
                };
            }
        }

        // Webhook notification example
        if (httpMethod == "POST" && path.Contains("payments/midtrans/notification", StringComparison.OrdinalIgnoreCase))
        {
            if (operation.RequestBody?.Content != null && operation.RequestBody.Content.TryGetValue("application/json", out var requestContent))
            {
                requestContent.Example = new OpenApiObject
                {
                    ["order_id"] = new OpenApiString("SPT-20261120-000001"),
                    ["status_code"] = new OpenApiString("200"),
                    ["gross_amount"] = new OpenApiString("520000.00"),
                    ["signature_key"] = new OpenApiString("96c64b63814de13a967597b69c6cf2c2fa8e719119ee373bb23348126e8caae38de5290a2cf757a799c2788e0dc7eb23058b8d4e0e5604ecda17adca3ea54d68"),
                    ["transaction_status"] = new OpenApiString("settlement"),
                    ["fraud_status"] = new OpenApiString("accept"),
                    ["transaction_id"] = new OpenApiString("8bb3d312-35db-44a8-a53d-2ad0b58e72dc"),
                    ["payment_type"] = new OpenApiString("qris"),
                    ["transaction_time"] = new OpenApiString("2026-11-20 12:00:00"),
                    ["settlement_time"] = new OpenApiString("2026-11-20 12:01:00")
                };
            }

            if (operation.Responses.TryGetValue("200", out var response200) && response200.Content.TryGetValue("application/json", out var responseContent))
            {
                responseContent.Example = new OpenApiObject
                {
                    ["status"] = new OpenApiString("processed"),
                    ["message"] = new OpenApiString("Notifikasi berhasil diproses: Settlement")
                };
            }
        }
    }
}
