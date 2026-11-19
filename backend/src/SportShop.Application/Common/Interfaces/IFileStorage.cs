namespace SportShop.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> SaveFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
