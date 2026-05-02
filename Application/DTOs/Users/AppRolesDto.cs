namespace Application.DTOs.Users;

/// <summary>
/// Estructura de roles que serializa a: { admin: boolean }
/// compatible con el modelo Angular del frontend.
/// </summary>
public class AppRolesDto
{
    public bool Admin { get; set; }
}
