using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUidAsync(string firebaseUid)
        => await _context.Users.FindAsync(firebaseUid);

    public async Task<User?> GetByEmailAsync(string email)
    {
        // Usamos el EF Context que ya tiene inyectado tu repositorio interno
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower().Trim());
    }

    public async Task<List<User>> GetAllAsync(int page, int pageSize)
        => await _context.Users
            .OrderBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

    public async Task<int> CountAsync()
        => await _context.Users.CountAsync();

    public async Task AddAsync(User user)
        => await _context.Users.AddAsync(user);

    public void Update(User user)
        => _context.Users.Update(user);
}
