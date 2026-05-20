using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUidAsync(string firebaseUid);
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetAllAsync(int page, int pageSize);
    Task<int> CountAsync();
    Task AddAsync(User user);
    void Update(User user);
}
