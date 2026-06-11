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
                
                // 1. Limpiar viajes "Olvidados" (Abiertos o Cerrados pero nunca iniciados)
                var pendingThreshold = DateTime.UtcNow.AddHours(-1);
                var expiredTrips = await context.Trips
                    .Where(t => (t.Status == TripStatus.Open || t.Status == TripStatus.Closed) && t.DepartureAt <= pendingThreshold)
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
                    _logger.LogInformation($"Expired {expiredTrips.Count} 'Forgotten' trips.");
                }

                // 2. Limpiar viajes "Fantasmas" (Iniciados pero nunca finalizados)
                var inProgressThreshold = DateTime.UtcNow.AddHours(-5);
                var stuckTrips = await context.Trips
                    .Where(t => t.Status == TripStatus.InProgress && t.DepartureAt <= inProgressThreshold)
                    .ToListAsync(stoppingToken);

                if (stuckTrips.Any())
                {
                    foreach (var trip in stuckTrips)
                    {
                        trip.Status = TripStatus.Completed;
                        // Aquí se podría disparar la lógica para notificar o cobrar si estuviera implementado
                    }
                    _logger.LogInformation($"Completed {stuckTrips.Count} 'Ghost' trips that were stuck in progress.");
                }

                if (expiredTrips.Any() || stuckTrips.Any())
                {
                    await context.SaveChangesAsync(stoppingToken);
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
