using Application.DTOs.Common;
using Application.DTOs.Users;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;

namespace Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinary;

    public UserService(IUnitOfWork uow, ICloudinaryService cloudinary)
    {
        _uow = uow;
        _cloudinary = cloudinary;
    }

    /// <summary>
    /// Sincroniza un usuario de Firebase hacia SQL Server.
    /// Si ya existe, actualiza los campos que hayan cambiado.
    /// </summary>
    public async Task<UserProfileDto> SyncUserAsync(SyncUserDto dto)
    {
        var displayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim();
        if (displayName is not null && displayName.Length > 200)
            displayName = displayName[..200];

        // 1. Intentar buscar por FirebaseUid primero
        var user = await _uow.Users.GetByUidAsync(dto.FirebaseUid);

        if (user is null)
        {
            // 2. SOLUCIÓN AL DUPLICADO: Si no se halla por UID, buscar por Email antes de insertar
            // Nota: Asegúrate de tener un método para buscar por email en tu repositorio, o haz la consulta pertinente.
            user = await _uow.Users.GetByEmailAsync(dto.Email);

            if (user is null)
            {
                // CASO A: El usuario es genuinamente nuevo (No existe el UID ni el Email) -> INSERT
                user = new User
                {
                    FirebaseUid = dto.FirebaseUid,
                    Email = dto.Email,
                    EmailVerified = dto.EmailVerified,
                    DisplayName = displayName ?? dto.Email,
                    Career = string.IsNullOrWhiteSpace(dto.Career) ? "Por definir" : dto.Career.Trim(),
                    Zone = string.IsNullOrWhiteSpace(dto.Zone) ? "Por definir" : dto.Zone.Trim(),
                    Phone = string.IsNullOrWhiteSpace(dto.Phone) ? "" : dto.Phone.Trim(),
                    PhotoUrl = dto.PhotoUrl
                };
                await _uow.Users.AddAsync(user);
            }
            else
            {
                // CASO B: El correo ya existía pero con otro UID (vínculo roto Firebase-SQL) -> UPDATE DE SEGURIDAD
                // Vinculamos el nuevo UID de Firebase al registro que ya teníamos en SQL Server
                user.FirebaseUid = dto.FirebaseUid;
                user.EmailVerified = dto.EmailVerified;

                if (displayName is not null)
                    user.DisplayName = displayName;

                user.Career = dto.Career ?? user.Career;
                user.Zone = dto.Zone ?? user.Zone;
                user.Phone = dto.Phone ?? user.Phone;
                user.PhotoUrl = dto.PhotoUrl ?? user.PhotoUrl;

                _uow.Users.Update(user);
            }
        }
        else
        {
            // CASO C: El usuario existe con su UID correcto -> UPDATE clásico
            user.Email = dto.Email;
            user.EmailVerified = dto.EmailVerified;

            if (displayName is not null)
                user.DisplayName = displayName;

            user.Career = dto.Career ?? user.Career;
            user.Zone = dto.Zone ?? user.Zone;
            user.Phone = dto.Phone ?? user.Phone;
            user.PhotoUrl = dto.PhotoUrl ?? user.PhotoUrl;

            _uow.Users.Update(user);
        }

        await _uow.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<UserProfileDto?> GetProfileAsync(string firebaseUid)
    {
        var user = await _uow.Users.GetByUidAsync(firebaseUid);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(string firebaseUid, UpdateProfileDto dto)
    {
        var user = await _uow.Users.GetByUidAsync(firebaseUid)
            ?? throw new KeyNotFoundException($"User {firebaseUid} not found.");

        if (!string.IsNullOrWhiteSpace(dto.DisplayName)) user.DisplayName = dto.DisplayName;
        if (dto.Career is not null) user.Career = dto.Career;
        if (dto.Zone is not null) user.Zone = dto.Zone;
        if (dto.Phone is not null) user.Phone = dto.Phone;
        
        if (dto.PhotoUrl is not null)
        {
            if (!string.IsNullOrWhiteSpace(user.PhotoUrl) && user.PhotoUrl != dto.PhotoUrl && user.PhotoUrl.Contains("cloudinary.com"))
            {
                var publicId = ExtractPublicIdFromUrl(user.PhotoUrl);
                if (!string.IsNullOrWhiteSpace(publicId))
                {
                    await _cloudinary.DeleteImageAsync(publicId);
                }
            }
            user.PhotoUrl = dto.PhotoUrl;
        }
        
        _uow.Users.Update(user);

        await _uow.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task SuspendUserAsync(string firebaseUid, DateTime until)
    {
        var user = await _uow.Users.GetByUidAsync(firebaseUid)
            ?? throw new KeyNotFoundException($"User {firebaseUid} not found.");

        user.SuspendedUntil = until;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
    }

    // ──── Admin ────

    public async Task<PagedResultDto<UserProfileDto>> GetAllUsersAsync(int page, int pageSize)
    {
        var users = await _uow.Users.GetAllAsync(page, pageSize);
        var count = await _uow.Users.CountAsync();

        return new PagedResultDto<UserProfileDto>
        {
            Items = users.Select(MapToDto).ToList(),
            TotalCount = count,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserProfileDto> AdminUpdateProfileAsync(string firebaseUid, UpdateProfileDto dto)
    {
        var user = await _uow.Users.GetByUidAsync(firebaseUid)
            ?? throw new KeyNotFoundException($"User {firebaseUid} not found.");

        if (!string.IsNullOrWhiteSpace(dto.DisplayName)) user.DisplayName = dto.DisplayName;
        if (dto.Career is not null) user.Career = dto.Career;
        if (dto.Zone is not null) user.Zone = dto.Zone;
        if (dto.Phone is not null) user.Phone = dto.Phone;
        if (dto.PhotoUrl is not null) user.PhotoUrl = dto.PhotoUrl;
        _uow.Users.Update(user);

        await _uow.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<UserProfileDto> ToggleDisabledAsync(string firebaseUid)
    {
        var user = await _uow.Users.GetByUidAsync(firebaseUid)
            ?? throw new KeyNotFoundException($"User {firebaseUid} not found.");

        user.Disabled = !user.Disabled;
        _uow.Users.Update(user);

        await _uow.SaveChangesAsync();
        return MapToDto(user);
    }

    // ──── Mapping helper ────
    private static UserProfileDto MapToDto(User user) => new()
    {
        FirebaseUid = user.FirebaseUid,
        Email = user.Email,
        EmailVerified = user.EmailVerified,
        DisplayName = user.DisplayName,
        Career = user.Career,
        Zone = user.Zone,
        Phone = user.Phone,
        PhotoUrl = user.PhotoUrl,
        Roles = new AppRolesDto { Admin = user.IsAdmin },
        RatingSum = user.RatingSum,
        RatingCount = user.RatingCount,
        TripsCount = user.TripsCount,
        DriverTripsCount = user.DriverTripsCount,
        PassengerTripsCount = user.PassengerTripsCount,
        SuspendedUntil = user.SuspendedUntil,
        Disabled = user.Disabled,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static string? ExtractPublicIdFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.Segments;
            
            int uploadIndex = -1;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].TrimEnd('/') == "upload")
                {
                    uploadIndex = i;
                    break;
                }
            }

            if (uploadIndex != -1 && uploadIndex < segments.Length - 1)
            {
                var pathSegments = segments.Skip(uploadIndex + 1).Select(s => s.TrimEnd('/'));
                var path = string.Join("/", pathSegments);
                
                var parts = path.Split('/');
                if (parts.Length > 1 && parts[0].StartsWith("v") && parts[0].Length > 1 && char.IsDigit(parts[0][1]))
                {
                    path = string.Join("/", parts.Skip(1));
                }

                var lastDot = path.LastIndexOf('.');
                if (lastDot > 0)
                {
                    path = path.Substring(0, lastDot);
                }
                
                return Uri.UnescapeDataString(path);
            }
        }
        catch { }
        return null;
    }
}
