using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;

namespace SportShop.Application.Features.Catalog.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id);

public sealed class DeleteCategoryCommandHandler
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(DeleteCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(Error.NotFound("category.not_found", "Kategori tidak ditemukan."));
        }

        category.IsActive = false;
        category.UpdatedAt = _clock.UtcNow;

        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
