using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;

namespace SportShop.Application.Features.Catalog.Brands.UpdateBrand;

public sealed class UpdateBrandCommandHandler
{
    private readonly IBrandRepository _brandRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<UpdateBrandCommand> _validator;

    public UpdateBrandCommandHandler(
        IBrandRepository brandRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<UpdateBrandCommand> validator)
    {
        _brandRepository = brandRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<BrandDto>> HandleAsync(UpdateBrandCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<BrandDto>(Error.Validation("brand.validation_error", "Data brand tidak valid.", errors));
        }

        var brand = await _brandRepository.GetByIdAsync(command.Id, cancellationToken);
        if (brand is null)
        {
            return Result.Failure<BrandDto>(Error.NotFound("brand.not_found", "Brand tidak ditemukan."));
        }

        if (await _brandRepository.NameExistsAsync(command.Name, command.Id, cancellationToken))
        {
            return Result.Failure<BrandDto>(Error.Conflict("brand.name_duplicate", "Nama brand sudah digunakan."));
        }

        if (await _brandRepository.SlugExistsAsync(command.Slug, command.Id, cancellationToken))
        {
            return Result.Failure<BrandDto>(Error.Conflict("brand.slug_duplicate", "Slug brand sudah digunakan."));
        }

        brand.Update(
            command.Name,
            command.Slug,
            command.LogoUrl,
            command.IsActive,
            _clock.UtcNow);

        await _brandRepository.UpdateAsync(brand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync("brand:list:v1", cancellationToken);

        return Result.Success(CatalogMapper.ToDto(brand));
    }
}
