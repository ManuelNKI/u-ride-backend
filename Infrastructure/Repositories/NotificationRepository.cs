using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetByUserUidAsync(string userUid)
        => await _context.Notifications
            .Where(n => n.UserUid == userUid)
            .OrderByDescending(n => n.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task<int> GetUnreadCountAsync(string userUid)
        => await _context.Notifications
            .CountAsync(n => n.UserUid == userUid && !n.Read);

    public async Task AddAsync(Notification notification)
        => await _context.Notifications.AddAsync(notification);

    public void Update(Notification notification)
        => _context.Notifications.Update(notification);
}
