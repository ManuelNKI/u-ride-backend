using Application.DTOs.Common;
using Application.DTOs.Users;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;

namespace Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;

    public UserService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <summary>
    /// Sincroniza un usuario de Firebase hacia SQL Server.
    /// Si ya existe, actualiza los campos que hayan cambiado.
    /// </summary>
    public async Task<UserProfileDto> SyncUserAsync(SyncUserDto dto)
    {
        // Asegurar que displayName nunca sea null o vacío para la BD, y truncar a 200 chars
        var displayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim();
        if (displayName is not null && displayName.Length > 200)
            displayName = displayName[..200];

        var user = await _uow.Users.GetByUidAsync(dto.FirebaseUid);

        if (user is null)
        {
            user = new User
            {
                FirebaseUid = dto.FirebaseUid,
                Email = dto.Email,
                EmailVerified = dto.EmailVerified,
                DisplayName = displayName ?? dto.Email, // fallback al email si no hay nombre
                Career = dto.Career,
                Zone = dto.Zone,
                Phone = dto.Phone,
                PhotoUrl = dto.PhotoUrl
            };
            await _uow.Users.AddAsync(user);
        }
        else
        {
            user.Email = dto.Email;
            user.EmailVerified = dto.EmailVerified;
            // Solo actualizar DisplayName si el nuevo valor no está vacío
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
        if (dto.PhotoUrl is not null) user.PhotoUrl = dto.PhotoUrl;
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
}
