using Application.Interfaces;
using Application.Services;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class SuspensionExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SuspensionExpirationWorker> _logger;

    public SuspensionExpirationWorker(IServiceProvider serviceProvider, ILogger<SuspensionExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SuspensionExpirationWorker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var expiredUsers = await uow.Users.GetExpiredSuspensionsAsync();

                if (expiredUsers.Any())
                {
                    foreach (var user in expiredUsers)
                    {
                        user.SuspendedUntil = null;
                        uow.Users.Update(user);

                        await notificationService.SendNotificationAsync(
                            userUid: user.FirebaseUid,
                            title: "Suspensión Levantada",
                            message: "Tu tiempo de suspensión ha terminado. Ya puedes volver a usar tu cuenta normalmente.",
                            type: NotificationType.System
                        );
                        
                        _logger.LogInformation($"Lifted suspension for user {user.FirebaseUid} and sent notification.");
                    }
                    await uow.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing SuspensionExpirationWorker.");
            }

            // Comprobar cada 10 minutos si hay suspensiones expiradas
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
