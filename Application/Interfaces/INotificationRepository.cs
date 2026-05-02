using Domain.Entities;

namespace Application.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetByUserUidAsync(string userUid);
    Task<int> GetUnreadCountAsync(string userUid);
    Task AddAsync(Notification notification);
    void Update(Notification notification);
}
