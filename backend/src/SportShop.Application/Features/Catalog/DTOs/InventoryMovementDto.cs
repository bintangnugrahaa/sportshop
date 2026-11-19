namespace SportShop.Application.Features.Catalog.DTOs;

public sealed record InventoryMovementDto(
    Guid Id,
    Guid VariantId,
    int ChangeQuantity,
    string Reason,
    Guid? OrderId,
    string? Note,
    Guid? CreatedBy,
    DateTime CreatedAt);
