namespace Domain.Entities;

/// <summary>
/// Usuario del sistema. La autenticación es delegada a Firebase;
/// esta entidad solo almacena el perfil y las estadísticas.
/// PK = FirebaseUid (string proveniente de Firebase Auth).
/// </summary>
public class User : AuditableEntity
{
    // ──── Identidad ────
    public string FirebaseUid { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool EmailVerified { get; set; }
    public string DisplayName { get; set; } = null!;

    // ──── Perfil ────
    public string? Career { get; set; }
    public string? Zone { get; set; }
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }

    // ──── Rol ────
    public bool IsAdmin { get; set; }

    // ──── Estadísticas ────
    public int RatingSum { get; set; }
    public int RatingCount { get; set; }
    public int TripsCount { get; set; }
    public int DriverTripsCount { get; set; }
    public int PassengerTripsCount { get; set; }

    // ──── Estado ────
    public DateTime? SuspendedUntil { get; set; }
    public bool Disabled { get; set; }

    // ──── Navegación ────
    public ICollection<Trip> DriverTrips { get; set; } = new List<Trip>();
    public ICollection<TripRequest> TripRequests { get; set; } = new List<TripRequest>();
    public ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
    public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
    public ICollection<Report> ReportsFiled { get; set; } = new List<Report>();
    public ICollection<Report> ReportsReceived { get; set; } = new List<Report>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
