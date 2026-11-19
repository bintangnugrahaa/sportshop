using SportShop.Application.Features.Catalog.DTOs;

namespace SportShop.Application.Features.Catalog.Images.UploadImage;

public sealed record UploadProductImageCommand(
    Guid ProductId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string? AltText = null,
    int? SortOrder = null,
    bool IsPrimary = false);
