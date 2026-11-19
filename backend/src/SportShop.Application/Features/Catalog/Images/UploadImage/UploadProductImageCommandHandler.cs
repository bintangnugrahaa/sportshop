using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Catalog;

namespace SportShop.Application.Features.Catalog.Images.UploadImage;

public sealed class UploadProductImageCommandHandler
{
    private const long _maxFileSize = 2 * 1024 * 1024; // 2 MB
    private static readonly HashSet<string> _allowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IProductRepository _productRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IUnitOfWork _unitOfWork;

    public UploadProductImageCommandHandler(
        IProductRepository productRepository,
        IFileStorage fileStorage,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductImageDto>> HandleAsync(UploadProductImageCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Length <= 0)
        {
            return Result.Failure<ProductImageDto>(Error.Validation("file.empty", "File gambar tidak boleh kosong."));
        }

        if (command.Length > _maxFileSize)
        {
            return Result.Failure<ProductImageDto>(Error.Validation("file.too_large", "Ukuran file gambar maksimal 2 MB."));
        }

        if (!_allowedMimeTypes.Contains(command.ContentType))
        {
            return Result.Failure<ProductImageDto>(Error.Validation("file.invalid_type", "Tipe file tidak didukung. Hanya JPEG, PNG, dan WebP yang diizinkan."));
        }

        var product = await _productRepository.GetByIdAsync(command.ProductId, true, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductImageDto>(Error.NotFound("product.not_found", "Produk tidak ditemukan."));
        }

        var fileUrl = await _fileStorage.SaveFileAsync(command.Content, command.FileName, command.ContentType, cancellationToken);

        var existingImages = await _productRepository.GetProductImagesAsync(command.ProductId, cancellationToken);
        var isPrimary = command.IsPrimary || existingImages.Count == 0;

        if (isPrimary)
        {
            foreach (var img in existingImages)
            {
                img.IsPrimary = false;
            }
        }

        var sortOrder = command.SortOrder ?? (existingImages.Count > 0 ? existingImages.Max(i => i.SortOrder) + 1 : 0);

        var image = new ProductImage(
            Guid.NewGuid(),
            command.ProductId,
            fileUrl,
            command.AltText,
            sortOrder,
            isPrimary);

        await _productRepository.AddImageAsync(image, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CatalogMapper.ToDto(image));
    }
}
