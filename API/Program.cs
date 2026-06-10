using System.Text.Json.Serialization;
using API.Middlewares;
using Application.Services;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ───────────────────────────── SERVICES ─────────────────────────────

// Infrastructure (DbContext, Repositories, Services)
builder.Services.AddInfrastructure(builder.Configuration);

// PayPal (Checkout / Orders API)
builder.Services.AddHttpClient<IPayPalCheckoutService, PayPalCheckoutService>();

// Hosted Services
builder.Services.AddHostedService<API.HostedServices.TripExpirationHostedService>();

// Controllers + JSON camelCase + enum como string
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ──── Autenticación JWT con Firebase ────
var firebaseProjectId = builder.Configuration["Firebase:ProjectId"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Manejar errores de autenticación de forma segura
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuth");
                logger.LogWarning(context.Exception,
                    "JWT authentication failed for {Path}", context.Request.Path);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ──── Swagger con soporte para Bearer Token ────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "U-Ride API",
        Version = "v1",
        Description = "API para transporte seguro compartido para estudiantes."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa tu token JWT de Firebase. Ejemplo: eyJhbGciOiJS..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ──── CORS (permisivo en desarrollo) ────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ───────────────────────────── APP ─────────────────────────────

var app = builder.Build();

// ──── Seed data (rutas y reglas predefinidas) ────
await Infrastructure.DbSeeder.SeedAsync(app.Services);

// Middleware global de excepciones (primero en el pipeline)
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "U-Ride API v1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Habilitar wwwroot para imágenes/evidencias
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
