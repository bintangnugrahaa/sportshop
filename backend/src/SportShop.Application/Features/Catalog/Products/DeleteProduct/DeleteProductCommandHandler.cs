using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Catalog.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id);

public sealed class DeleteProductCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(command.Id, true, cancellationToken);
        if (product is null)
        {
            return Result.Failure(Error.NotFound("product.not_found", "Produk tidak ditemukan."));
        }

        var now = _clock.UtcNow;
        product.SetActive(false, now);

        foreach (var variant in product.Variants)
        {
            variant.SetActive(false, now);
        }

        await _productRepository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
