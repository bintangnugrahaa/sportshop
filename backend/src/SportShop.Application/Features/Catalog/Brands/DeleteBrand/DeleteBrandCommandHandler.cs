using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Catalog.Brands.DeleteBrand;

public sealed record DeleteBrandCommand(Guid Id);

public sealed class DeleteBrandCommandHandler
{
    private readonly IBrandRepository _brandRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeleteBrandCommandHandler(
        IBrandRepository brandRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(DeleteBrandCommand command, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetByIdAsync(command.Id, cancellationToken);
        if (brand is null)
        {
            return Result.Failure(Error.NotFound("brand.not_found", "Brand tidak ditemukan."));
        }

        brand.IsActive = false;
        brand.UpdatedAt = _clock.UtcNow;

        await _brandRepository.UpdateAsync(brand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
