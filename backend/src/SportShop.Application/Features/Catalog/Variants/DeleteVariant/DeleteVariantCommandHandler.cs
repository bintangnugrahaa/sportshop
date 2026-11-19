using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Catalog.Variants.DeleteVariant;

public sealed record DeleteVariantCommand(Guid Id);

public sealed class DeleteVariantCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeleteVariantCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(DeleteVariantCommand command, CancellationToken cancellationToken = default)
    {
        var variant = await _productRepository.GetVariantByIdAsync(command.Id, cancellationToken);
        if (variant is null)
        {
            return Result.Failure(Error.NotFound("variant.not_found", "Varian tidak ditemukan."));
        }

        var now = _clock.UtcNow;
        variant.SetActive(false, now);
        await _productRepository.UpdateVariantAsync(variant, cancellationToken);

        var product = await _productRepository.GetByIdAsync(variant.ProductId, true, cancellationToken);
        if (product is not null)
        {
            product.RecalculateBasePrice(now);
            await _productRepository.UpdateAsync(product, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
