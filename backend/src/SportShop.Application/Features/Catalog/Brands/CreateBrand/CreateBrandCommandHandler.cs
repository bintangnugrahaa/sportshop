using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Catalog;

namespace SportShop.Application.Features.Catalog.Brands.CreateBrand;

public sealed class CreateBrandCommandHandler
{
    private readonly IBrandRepository _brandRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CreateBrandCommand> _validator;

    public CreateBrandCommandHandler(
        IBrandRepository brandRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CreateBrandCommand> validator)
    {
        _brandRepository = brandRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<BrandDto>> HandleAsync(CreateBrandCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<BrandDto>(Error.Validation("brand.validation_error", "Data brand tidak valid.", errors));
        }

        if (await _brandRepository.NameExistsAsync(command.Name, null, cancellationToken))
        {
            return Result.Failure<BrandDto>(Error.Conflict("brand.name_duplicate", "Nama brand sudah digunakan."));
        }

        if (await _brandRepository.SlugExistsAsync(command.Slug, null, cancellationToken))
        {
            return Result.Failure<BrandDto>(Error.Conflict("brand.slug_duplicate", "Slug brand sudah digunakan."));
        }

        var brand = new Brand(
            Guid.NewGuid(),
            command.Name,
            command.Slug,
            command.LogoUrl,
            command.IsActive,
            _clock.UtcNow);

        await _brandRepository.AddAsync(brand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync("brand:list:v1", cancellationToken);

        return Result.Success(CatalogMapper.ToDto(brand));
    }
}
