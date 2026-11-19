using SportShop.Domain.Catalog;
using SportShop.Domain.Common;

namespace SportShop.Domain.Inventory;

public class InventoryMovement : IEntity
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public int ChangeQuantity { get; set; }
    public StockReason Reason { get; set; }
    public Guid? OrderId { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public InventoryMovement()
    {
    }

    public InventoryMovement(
        Guid id,
        Guid variantId,
        int changeQuantity,
        StockReason reason,
        string? note = null,
        Guid? orderId = null,
        Guid? createdBy = null,
        DateTime? createdAt = null)
    {
        if (reason == StockReason.ManualAdjust && string.IsNullOrWhiteSpace(note))
        {
            throw new DomainException("inventory.note_required", "Catatan wajib diisi untuk penyesuaian manual.");
        }

        Id = id;
        VariantId = variantId;
        ChangeQuantity = changeQuantity;
        Reason = reason;
        Note = note;
        OrderId = orderId;
        CreatedBy = createdBy;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
