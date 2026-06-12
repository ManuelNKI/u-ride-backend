using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class PayPalCheckoutService : IPayPalCheckoutService
{
    private readonly HttpClient _http;
    private readonly ILogger<PayPalCheckoutService> _logger;
    private readonly PayPalOptions _options;

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public PayPalCheckoutService(HttpClient http, IConfiguration configuration, ILogger<PayPalCheckoutService> logger)
    {
        _http = http;
        _logger = logger;
        _options = configuration.GetSection("PayPal").Get<PayPalOptions>() ?? new PayPalOptions();

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _http.BaseAddress = new Uri(_options.BaseUrl);
        }
    }

    public async Task<PayPalCreateOrderResult> CreateOrderAsync(
        PayPalCreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);

        using var msg = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = request.CurrencyCode,
                        value = request.AmountValue.ToString(),
                    },
                    custom_id = request.CustomId,
                    reference_id = request.ReferenceId,
                    description = request.Description,
                }
            },
            application_context = new
            {
                return_url = request.ReturnUrl,
                cancel_url = request.CancelUrl,
                user_action = "PAY_NOW",
            }
        };

        msg.Content = JsonContent.Create(payload);

        using var resp = await _http.SendAsync(msg, cancellationToken);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            // 1. Guardamos el error detallado en los logs del servidor
            _logger.LogError("PayPal API Error en Producción: Código HTTP {StatusCode}. Respuesta: {Body}", (int)resp.StatusCode, body);
            
            // 2. IMPORTANTE: Lanzamos una excepción que contenga la respuesta REAL de PayPal
            // Puedes deserializarla o enviarla directamente como mensaje para que tu API la exponga
            throw new HttpRequestException($"PayPal rechazó la solicitud: {body}", null, resp.StatusCode);
        }

        using var doc = JsonDocument.Parse(body);
        var orderId = doc.RootElement.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(orderId))
            throw new InvalidOperationException("Respuesta inválida de PayPal (sin id).");

        string? approveUrl = null;
        if (doc.RootElement.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Array)
        {
            foreach (var link in links.EnumerateArray())
            {
                var rel = link.TryGetProperty("rel", out var relEl) ? relEl.GetString() : null;
                if (!string.Equals(rel, "approve", StringComparison.OrdinalIgnoreCase)) continue;
                approveUrl = link.TryGetProperty("href", out var hrefEl) ? hrefEl.GetString() : null;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(approveUrl))
            throw new InvalidOperationException("Respuesta inválida de PayPal (sin link approve).");

        return new PayPalCreateOrderResult(orderId!, approveUrl!);
    }

    public async Task<PayPalOrderInfo> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("orderId requerido", nameof(orderId));

        var token = await GetAccessTokenAsync(cancellationToken);

        using var msg = new HttpRequestMessage(HttpMethod.Get, $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await _http.SendAsync(msg, cancellationToken);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayPal get order failed: {StatusCode} {Body}", (int)resp.StatusCode, Truncate(body));
            throw new InvalidOperationException("No se pudo consultar la orden de PayPal.");
        }

        using var doc = JsonDocument.Parse(body);
        var status = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : null;
        string? customId = null;
        string? referenceId = null;

        if (doc.RootElement.TryGetProperty("purchase_units", out var pu) && pu.ValueKind == JsonValueKind.Array)
        {
            var first = pu[0];
            customId = first.TryGetProperty("custom_id", out var ci) ? ci.GetString() : null;
            referenceId = first.TryGetProperty("reference_id", out var ri) ? ri.GetString() : null;
        }

        return new PayPalOrderInfo(orderId, status ?? "", customId, referenceId);
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("orderId requerido", nameof(orderId));

        var token = await GetAccessTokenAsync(cancellationToken);

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        msg.Content = JsonContent.Create(new { });

        using var resp = await _http.SendAsync(msg, cancellationToken);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayPal capture failed: {StatusCode} {Body}", (int)resp.StatusCode, Truncate(body));
            throw new InvalidOperationException("No se pudo capturar el pago en PayPal.");
        }

        using var doc = JsonDocument.Parse(body);
        var status = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : null;

        string? customId = null;
        string? referenceId = null;
        string? captureId = null;
        string? captureStatus = null;

        if (doc.RootElement.TryGetProperty("purchase_units", out var pu) && pu.ValueKind == JsonValueKind.Array)
        {
            var first = pu[0];
            customId = first.TryGetProperty("custom_id", out var ci) ? ci.GetString() : null;
            referenceId = first.TryGetProperty("reference_id", out var ri) ? ri.GetString() : null;

            if (first.TryGetProperty("payments", out var payments) && payments.ValueKind == JsonValueKind.Object)
            {
                if (payments.TryGetProperty("captures", out var captures) && captures.ValueKind == JsonValueKind.Array && captures.GetArrayLength() > 0)
                {
                    var cap = captures[0];
                    captureId = cap.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    captureStatus = cap.TryGetProperty("status", out var csEl) ? csEl.GetString() : null;
                }
            }
        }

        return new PayPalCaptureResult(orderId, status ?? "", captureId, captureStatus, customId, referenceId);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            return _accessToken;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
                return _accessToken;

            if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
                throw new InvalidOperationException("PayPal ClientId/ClientSecret no configurados.");

            var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

            using var msg = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
            msg.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            msg.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
            });

            using var resp = await _http.SendAsync(msg, cancellationToken);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("PayPal token failed: {StatusCode} {Body}", (int)resp.StatusCode, Truncate(body));
                throw new InvalidOperationException("No se pudo autenticar con PayPal.");
            }

            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 300;

            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Respuesta inválida de PayPal (sin access_token).");

            _accessToken = token;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn));
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static string Truncate(string? s, int max = 700)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max);
    }

    private sealed class PayPalOptions
    {
        public string BaseUrl { get; set; } = "https://api-m.paypal.com";
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
    }
}
