using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Catalog.Images.DeleteImage;

public sealed record DeleteProductImageCommand(Guid ProductId, Guid ImageId);

public sealed class DeleteProductImageCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly IFileStorage _fileStorage;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductImageCommandHandler(
        IProductRepository productRepository,
        IFileStorage fileStorage,
        ICacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _fileStorage = fileStorage;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteProductImageCommand command, CancellationToken cancellationToken = default)
    {
        var image = await _productRepository.GetImageByIdAsync(command.ImageId, cancellationToken);
        if (image is null || image.ProductId != command.ProductId)
        {
            return Result.Failure(Error.NotFound("image.not_found", "Gambar tidak ditemukan."));
        }

        var wasPrimary = image.IsPrimary;

        await _fileStorage.DeleteFileAsync(image.Url, cancellationToken);
        await _productRepository.RemoveImageAsync(image, cancellationToken);

        if (wasPrimary)
        {
            var remainingImages = await _productRepository.GetProductImagesAsync(command.ProductId, cancellationToken);
            var nextPrimary = remainingImages.FirstOrDefault(i => i.Id != command.ImageId);
            if (nextPrimary is not null)
            {
                nextPrimary.IsPrimary = true;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var product = await _productRepository.GetByIdAsync(command.ProductId, false, cancellationToken);
        if (product is not null)
        {
            await _cacheService.RemoveAsync($"prod:detail:{product.Slug.ToLowerInvariant()}:v1", cancellationToken);
        }

        return Result.Success();
    }
}
