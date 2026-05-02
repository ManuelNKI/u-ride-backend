namespace Application.DTOs.Users;

/// <summary>
/// Proyección pública del perfil de usuario. Incluye estadísticas
/// y el objeto Roles con la estructura que espera Angular.
/// </summary>
public class UserProfileDto
{
    public string FirebaseUid { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool EmailVerified { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Career { get; set; }
    public string? Zone { get; set; }
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }

    // Roles — serializa a: { roles: { admin: true/false } }
    public AppRolesDto Roles { get; set; } = new();

    // Estadísticas
    public int RatingSum { get; set; }
    public int RatingCount { get; set; }
    public double AverageRating => RatingCount > 0 ? (double)RatingSum / RatingCount : 0;
    public int TripsCount { get; set; }
    public int DriverTripsCount { get; set; }
    public int PassengerTripsCount { get; set; }

    // Estado
    public DateTime? SuspendedUntil { get; set; }
    public bool Disabled { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
