using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

/// <summary>
/// Seeder de datos de prueba. Crea rutas y reglas predefinidas
/// si no existen aún en la base de datos.
/// Se ejecuta al iniciar la aplicación.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            if (context.Database.IsRelational())
            {
                // Aplicar migraciones pendientes
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrated successfully.");
            }
            else
            {
                // Crear base de datos para entornos no relacionales (InMemory)
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("In-memory database created.");
            }

            await SeedRoutesAsync(context, logger);
            await SeedRulesAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database seeding.");
        }
    }

    private static async Task SeedRoutesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.TripRoutes.AnyAsync())
        {
            logger.LogInformation("TripRoutes already seeded. Skipping.");
            return;
        }

        var routes = new[]
        {
            "Izamba - Huachi Chico - Querochaca",
            "Ingahurco - Huachi Chico - Querochaca",
            "Ingahurco - Huachi Chico",
            "Huachi Chico - Querochaca",
            "Huachi Chico - Ingahurco",
            "Ficoa - Huachi Chico - Querochaca",
            "Ambato Centro - Querochaca",
            "Pelileo - Querochaca",
        };

        foreach (var name in routes)
        {
            context.TripRoutes.Add(new TripRoute
            {
                Id = Guid.NewGuid(),
                Name = name
            });
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} trip routes.", routes.Length);
    }

    private static async Task SeedRulesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.TripRules.AnyAsync())
        {
            logger.LogInformation("TripRules already seeded. Skipping.");
            return;
        }

        var rules = new[]
        {
            "Puntualidad",
            "Respeto y buen trato",
            "No compartir datos sensibles",
            "Uso obligatorio de cinturón de seguridad",
            "No fumar dentro del vehículo",
            "Mantener limpio el vehículo",
        };

        foreach (var text in rules)
        {
            context.TripRules.Add(new TripRule
            {
                Id = Guid.NewGuid(),
                Text = text
            });
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} trip rules.", rules.Length);
    }
}
