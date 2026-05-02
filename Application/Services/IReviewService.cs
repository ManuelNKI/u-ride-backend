using Application.DTOs.Reviews;

namespace Application.Services;

public interface IReviewService
{
    Task<ReviewDto> CreateReviewAsync(string fromUid, CreateReviewDto dto);
    Task<List<ReviewDto>> GetByTripAsync(Guid tripId);
    Task<List<ReviewDto>> GetReceivedByUserAsync(string toUid);
}
