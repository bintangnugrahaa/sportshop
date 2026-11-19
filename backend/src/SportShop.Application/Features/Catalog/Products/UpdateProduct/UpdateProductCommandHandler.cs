using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<UpdateProductCommand> _validator;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IBrandRepository brandRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<UpdateProductCommand> validator)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ProductDto>> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<ProductDto>(Error.Validation("product.validation_error", "Data produk tidak valid.", errors));
        }

        var product = await _productRepository.GetByIdAsync(command.Id, true, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("product.not_found", "Produk tidak ditemukan."));
        }

        if (await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken) is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("category.not_found", "Kategori tidak ditemukan."));
        }

        if (await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken) is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("brand.not_found", "Brand tidak ditemukan."));
        }

        if (await _productRepository.SlugExistsAsync(command.Slug, command.Id, cancellationToken))
        {
            return Result.Failure<ProductDto>(Error.Conflict("product.slug_duplicate", "Slug produk sudah digunakan."));
        }

        var now = _clock.UtcNow;
        product.UpdateDetails(
            command.Name,
            command.Slug,
            command.Description,
            command.CategoryId,
            command.BrandId,
            now);
        product.SetActive(command.IsActive, now);

        await _productRepository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CatalogMapper.ToDto(product));
    }
}
