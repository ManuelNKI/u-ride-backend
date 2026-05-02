using Application.DTOs.Users;

namespace Application.Services;

public interface IUserService
{
    Task<UserProfileDto> SyncUserAsync(SyncUserDto dto);
    Task<UserProfileDto?> GetProfileAsync(string firebaseUid);
    Task<UserProfileDto> UpdateProfileAsync(string firebaseUid, SyncUserDto dto);
    Task SuspendUserAsync(string firebaseUid, DateTime until);
}
