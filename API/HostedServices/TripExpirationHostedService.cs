using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace API.HostedServices;

public class TripExpirationHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TripExpirationHostedService> _logger;

    public TripExpirationHostedService(IServiceProvider serviceProvider, ILogger<TripExpirationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Expirar viajes no iniciados (Open) 1 hora después de departureAt
                var expirationTime = DateTime.UtcNow.AddHours(-1);

                var expiredTrips = await context.Trips
                    .Where(t => t.Status == TripStatus.Open && t.DepartureAt <= expirationTime)
                    .ToListAsync(stoppingToken);

                if (expiredTrips.Any())
                {
                    foreach (var trip in expiredTrips)
                    {
                        trip.Status = TripStatus.Expired;
                        
                        var requests = await context.TripRequests
                            .Where(r => r.TripId == trip.Id && r.Status != RequestStatus.Cancelled && r.Status != RequestStatus.Rejected)
                            .ToListAsync(stoppingToken);
                            
                        foreach(var request in requests)
                        {
                            request.Status = RequestStatus.Cancelled;
                        }
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Expired {expiredTrips.Count} trips.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing TripExpirationHostedService.");
            }

            // Revisar cada 5 minutos
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
