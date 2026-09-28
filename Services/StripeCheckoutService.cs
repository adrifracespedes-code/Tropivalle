using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EcommerceApp.Services;

public class StripeOptions
{
    public string SecretKey { get; set; } = "";
    public string PublishableKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string Currency { get; set; } = "bob"; // boliviano
}

public class PaymentOptions
{
    public string BankName { get; set; } = "Banco (configurar)";
    public string AccountHolder { get; set; } = "Industrias Alimenticias TropiValle";
    public string AccountNumber { get; set; } = "";
    public string AccountType { get; set; } = "Caja de ahorro";
    public string QrImageUrl { get; set; } = ""; // opcional: /images/qr-pago.png
    public string WhatsAppNumber { get; set; } = "59175950776";
}

public class StripeCheckoutService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<StripeCheckoutService> logger)
{
    public bool IsConfigured
    {
        get
        {
            var key = config["Stripe:SecretKey"]
                      ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                      ?? "";
            return key.StartsWith("sk_", StringComparison.Ordinal);
        }
    }

    public async Task<(bool Ok, string? Url, string? SessionId, string? Error)> CreateCheckoutSessionAsync(
        string orderCode,
        string productName,
        long amountInCents,
        string currency,
        string successUrl,
        string cancelUrl,
        string customerEmail,
        CancellationToken ct = default)
    {
        var secret = config["Stripe:SecretKey"]
                     ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                     ?? "";
        if (!secret.StartsWith("sk_", StringComparison.Ordinal))
            return (false, null, null, "Stripe no está configurado (falta STRIPE_SECRET_KEY).");

        var client = httpClientFactory.CreateClient("stripe");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secret);

        // Stripe Checkout Session (form-urlencoded)
        var form = new List<KeyValuePair<string, string>>
        {
            new("mode", "payment"),
            new("success_url", successUrl),
            new("cancel_url", cancelUrl),
            new("client_reference_id", orderCode),
            new("metadata[order_code]", orderCode),
            new("line_items[0][quantity]", "1"),
            new("line_items[0][price_data][currency]", currency.ToLowerInvariant()),
            new("line_items[0][price_data][unit_amount]", amountInCents.ToString()),
            new("line_items[0][price_data][product_data][name]", productName),
        };
        if (!string.IsNullOrWhiteSpace(customerEmail))
            form.Add(new("customer_email", customerEmail));

        using var content = new FormUrlEncodedContent(form);
        using var resp = await client.PostAsync("https://api.stripe.com/v1/checkout/sessions", content, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            logger.LogWarning("Stripe error: {Body}", body);
            try
            {
                using var doc = JsonDocument.Parse(body);
                var msg = doc.RootElement.GetProperty("error").GetProperty("message").GetString();
                return (false, null, null, msg ?? "Error de Stripe");
            }
            catch
            {
                return (false, null, null, "Error al crear sesión de pago Stripe");
            }
        }

        using (var doc = JsonDocument.Parse(body))
        {
            var id = doc.RootElement.GetProperty("id").GetString();
            var url = doc.RootElement.GetProperty("url").GetString();
            return (true, url, id, null);
        }
    }
}
