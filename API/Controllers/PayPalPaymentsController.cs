using System.Security.Claims;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;

namespace API.Controllers;

[ApiController]
[Route("api/payments/paypal")]
public class PayPalPaymentsController : ControllerBase
{
    private readonly IPayPalCheckoutService _payPal;
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;
    private readonly IConfiguration _config;

    public PayPalPaymentsController(
        IPayPalCheckoutService payPal,
        IUnitOfWork uow,
        INotificationService notifications,
        IConfiguration config)
    {
        _payPal = payPal;
        _uow = uow;
        _notifications = notifications;
        _config = config;
    }

    public sealed class CreateOrderDto
    {
        public Guid TripRequestId { get; set; }
    }

    public sealed class CreateOrderResponse
    {
        public string OrderId { get; set; } = null!;
        public string ApproveUrl { get; set; } = null!;
    }

    [Authorize]
    [HttpPost("orders")]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var uid = GetFirebaseUid();

        var request = await _uow.TripRequests.GetByIdAsync(dto.TripRequestId);
        if (request is null) return NotFound(new { message = "Solicitud no encontrada." });

        if (request.PassengerUid != uid) return Forbid();
        if (request.Status != RequestStatus.Accepted)
            return BadRequest(new { message = "Solo se pueden pagar solicitudes aceptadas." });
        if (request.PaymentStatus == PaymentStatus.Paid)
            return BadRequest(new { message = "Esta solicitud ya ha sido pagada." });

        var total = Math.Round(request.Trip.Price * 1.10m, 2, MidpointRounding.AwayFromZero);
        var amountStr = total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        var apiBase = ResolvePublicApiBase();

        var returnUrl = $"{apiBase}/api/payments/paypal/return?tripRequestId={request.Id}";
        var cancelUrl = $"{apiBase}/api/payments/paypal/cancel?tripRequestId={request.Id}";

        var result = await _payPal.CreateOrderAsync(new PayPalCreateOrderRequest(
            AmountValue: amountStr,
            CurrencyCode: "USD",
            ReturnUrl: returnUrl,
            CancelUrl: cancelUrl,
            CustomId: request.Id.ToString(),
            ReferenceId: request.TripId.ToString(),
            Description: "U-Ride - Pago de viaje"));

        return Ok(new CreateOrderResponse
        {
            OrderId = result.OrderId,
            ApproveUrl = result.ApproveUrl,
        });
    }

    // PayPal redirects the browser here after approval
    [AllowAnonymous]
    [HttpGet("return")]
    public async Task<IActionResult> Return([FromQuery] Guid tripRequestId, [FromQuery] string token)
    {
        // token == orderId
        if (tripRequestId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            return BadRequest("Missing tripRequestId or token");

        var request = await _uow.TripRequests.GetByIdAsync(tripRequestId);

        try
        {
            var info = await _payPal.GetOrderAsync(token);
            if (!string.Equals(info.CustomId, tripRequestId.ToString(), StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid order mapping");

            var capture = await _payPal.CaptureOrderAsync(token);

            var paid = string.Equals(capture.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(capture.CaptureStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase);

            if (!paid)
                return Redirect(BuildClientRedirect(request, tripRequestId, status: "cancel"));

            if (request is not null && request.PaymentStatus != PaymentStatus.Paid)
            {
                request.PaymentStatus = PaymentStatus.Paid;
                _uow.TripRequests.Update(request);
                await _uow.SaveChangesAsync();

                await _notifications.SendNotificationAsync(
                    request.Trip.DriverUid,
                    title: "Pago recibido",
                    message: $"{request.PassengerName} ha pagado su cupo.",
                    type: NotificationType.System,
                    tripId: request.Trip.Id);
            }

            return Redirect(BuildClientRedirect(request, tripRequestId, status: "success"));
        }
        catch
        {
            return Redirect(BuildClientRedirect(request, tripRequestId, status: "error"));
        }
    }

    [AllowAnonymous]
    [HttpGet("cancel")]
    public async Task<IActionResult> Cancel([FromQuery] Guid tripRequestId)
    {
        var request = await _uow.TripRequests.GetByIdAsync(tripRequestId);
        return Redirect(BuildClientRedirect(request, tripRequestId, status: "cancel"));
    }

    private string BuildClientRedirect(Domain.Entities.TripRequest? request, Guid tripRequestId, string status)
    {
        var clientBase = ResolveClientBase();
        var s = string.IsNullOrWhiteSpace(status) ? "cancel" : status;
        var qs = $"paypal={Uri.EscapeDataString(s)}";

        if (request is null)
            return $"{clientBase}/app/trips?{qs}&tripRequestId={tripRequestId}";

        return $"{clientBase}/app/trips/{request.TripId}?{qs}&tripRequestId={tripRequestId}";
    }

    private string ResolvePublicApiBase()
    {
        var configured = _config["PayPal:PublicApiBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');

        var req = HttpContext.Request;
        return $"{req.Scheme}://{req.Host}";
    }

    private string ResolveClientBase()
    {
        var configured = _config["PayPal:ClientBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');

        // Default for local dev
        return "http://localhost:4200";
    }

    private string GetFirebaseUid()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("user_id")
           ?? throw new UnauthorizedAccessException("Firebase UID not found in token.");
}
