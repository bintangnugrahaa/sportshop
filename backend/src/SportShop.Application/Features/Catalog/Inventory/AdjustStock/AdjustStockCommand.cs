namespace SportShop.Application.Features.Catalog.Inventory.AdjustStock;

public sealed record AdjustStockCommand(
    Guid VariantId,
    int ChangeQuantity,
    string Note);
