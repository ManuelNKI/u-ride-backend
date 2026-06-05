using Application.Interfaces;
using Application.Services;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

/// <summary>
/// Método de extensión para registrar todos los servicios
/// de Infrastructure en el contenedor de DI.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ──── DbContext ────
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // ──── Repositorios ────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<ITripRequestRepository, TripRequestRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ITripRouteRepository, TripRouteRepository>();
        services.AddScoped<ITripRuleRepository, TripRuleRepository>();

        // ──── Unit of Work ────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ──── Servicios de negocio ────
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<ITripRequestService, TripRequestService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();

        // ──── Background Workers ────
        services.AddHostedService<SuspensionExpirationWorker>();

        return services;
    }
}
