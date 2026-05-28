using Application.DTOs.Common;
using Application.DTOs.Users;

namespace Application.Services;

public interface IUserService
{
    Task<UserProfileDto> SyncUserAsync(SyncUserDto dto);
    Task<UserProfileDto?> GetProfileAsync(string firebaseUid);
    Task<UserProfileDto> UpdateProfileAsync(string firebaseUid, UpdateProfileDto dto);
    Task SuspendUserAsync(string firebaseUid, DateTime until);
    Task UnsuspendUserAsync(string firebaseUid);

    // ──── Admin ────
    Task<PagedResultDto<UserProfileDto>> GetAllUsersAsync(int page, int pageSize);
    Task<UserProfileDto> AdminUpdateProfileAsync(string firebaseUid, UpdateProfileDto dto);
    Task<UserProfileDto> ToggleDisabledAsync(string firebaseUid);
}
