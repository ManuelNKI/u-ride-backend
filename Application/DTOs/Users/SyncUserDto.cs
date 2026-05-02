namespace Application.DTOs.Users;

/// <summary>
/// DTO recibido desde el frontend la primera vez que un usuario
/// inicia sesión, para sincronizar su perfil de Firebase → SQL Server.
/// </summary>
public class SyncUserDto
{
    public string FirebaseUid { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool EmailVerified { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Career { get; set; }
    public string? Zone { get; set; }
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
}
