namespace Application.Interfaces;

public interface ICloudinaryService
{
    Task<bool> DeleteImageAsync(string publicId);
}
