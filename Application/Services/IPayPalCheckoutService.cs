using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services;

public interface IPayPalCheckoutService
{
    Task<PayPalCreateOrderResult> CreateOrderAsync(
        PayPalCreateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<PayPalOrderInfo> GetOrderAsync(string orderId, CancellationToken cancellationToken = default);

    Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default);
}

public sealed record PayPalCreateOrderRequest(
    string AmountValue,
    string CurrencyCode,
    string ReturnUrl,
    string CancelUrl,
    string CustomId,
    string? ReferenceId = null,
    string? Description = null);

public sealed record PayPalCreateOrderResult(string OrderId, string ApproveUrl);

public sealed record PayPalOrderInfo(string OrderId, string Status, string? CustomId, string? ReferenceId);

public sealed record PayPalCaptureResult(
    string OrderId,
    string Status,
    string? CaptureId,
    string? CaptureStatus,
    string? CustomId,
    string? ReferenceId);
