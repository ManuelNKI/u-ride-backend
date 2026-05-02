using Application.DTOs.Reviews;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;

namespace Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;

    public ReviewService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ReviewDto> CreateReviewAsync(string fromUid, CreateReviewDto dto)
    {
        if (dto.Stars < 1 || dto.Stars > 5)
            throw new ArgumentException("Stars must be between 1 and 5.");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            TripId = dto.TripId,
            FromUid = fromUid,
            ToUid = dto.ToUid,
            Stars = dto.Stars,
            Comment = dto.Comment
        };

        await _uow.Reviews.AddAsync(review);

        // Actualizar estadísticas del usuario calificado
        var toUser = await _uow.Users.GetByUidAsync(dto.ToUid);
        if (toUser is not null)
        {
            toUser.RatingSum += dto.Stars;
            toUser.RatingCount++;
            _uow.Users.Update(toUser);
        }

        await _uow.SaveChangesAsync();
        return MapToDto(review);
    }

    public async Task<List<ReviewDto>> GetByTripAsync(Guid tripId)
    {
        var reviews = await _uow.Reviews.GetByTripIdAsync(tripId);
        return reviews.Select(MapToDto).ToList();
    }

    public async Task<List<ReviewDto>> GetReceivedByUserAsync(string toUid)
    {
        var reviews = await _uow.Reviews.GetReceivedByUserAsync(toUid);
        return reviews.Select(MapToDto).ToList();
    }

    private static ReviewDto MapToDto(Review r) => new()
    {
        Id = r.Id,
        TripId = r.TripId,
        FromUid = r.FromUid,
        ToUid = r.ToUid,
        Stars = r.Stars,
        Comment = r.Comment,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
