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
        var user = await _uow.Users.GetByUidAsync(dto.FirebaseUid);

        if (user is null)
        {
            user = new User
            {
                FirebaseUid = dto.FirebaseUid,
                Email = dto.Email,
                EmailVerified = dto.EmailVerified,
                DisplayName = dto.DisplayName,
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
            user.DisplayName = dto.DisplayName;
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

    public async Task<UserProfileDto> UpdateProfileAsync(string firebaseUid, SyncUserDto dto)
    {
        var user = await _uow.Users.GetByUidAsync(firebaseUid)
            ?? throw new KeyNotFoundException($"User {firebaseUid} not found.");

        user.DisplayName = dto.DisplayName;
        user.Career = dto.Career;
        user.Zone = dto.Zone;
        user.Phone = dto.Phone;
        user.PhotoUrl = dto.PhotoUrl;
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
