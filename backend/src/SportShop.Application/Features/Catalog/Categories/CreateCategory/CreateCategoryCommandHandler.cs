using FluentValidation;
using SportShop.Application.Common;
using SportShop.Application.Common.Interfaces;
using SportShop.Application.Features.Catalog.DTOs;
using SportShop.Application.Mapping;
using SportShop.Domain.Catalog;

namespace SportShop.Application.Features.Catalog.Categories.CreateCategory;

public sealed class CreateCategoryCommandHandler
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CreateCategoryCommand> _validator;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CreateCategoryCommand> validator)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CategoryDto>> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<CategoryDto>(Error.Validation("category.validation_error", "Data kategori tidak valid.", errors));
        }

        if (await _categoryRepository.SlugExistsAsync(command.Slug, null, cancellationToken))
        {
            return Result.Failure<CategoryDto>(Error.Conflict("category.slug_duplicate", "Slug kategori sudah digunakan."));
        }

        if (command.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(command.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<CategoryDto>(Error.NotFound("category.parent_not_found", "Kategori induk tidak ditemukan."));
            }

            if (parent.ParentId.HasValue)
            {
                return Result.Failure<CategoryDto>(Error.Validation("category.max_depth", "Kategori maksimal 2 tingkat."));
            }
        }

        var category = new Category(
            Guid.NewGuid(),
            command.Name,
            command.Slug,
            command.ParentId,
            command.SortOrder,
            command.IsActive,
            _clock.UtcNow);

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CatalogMapper.ToDto(category));
    }
}
