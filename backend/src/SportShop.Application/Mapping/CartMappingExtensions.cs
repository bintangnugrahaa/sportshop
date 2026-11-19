using SportShop.Application.Features.Cart.DTOs;
using SportShop.Domain.Carts;

namespace SportShop.Application.Mapping;

public static class CartMappingExtensions
{
    public static CartDto ToDto(this Cart cart)
    {
        var items = cart.Items.Select(item =>
        {
            var variant = item.Variant;
            var product = variant?.Product;
            var primaryImage = product?.Images?.FirstOrDefault(img => img.IsPrimary)?.Url
                ?? product?.Images?.OrderBy(img => img.SortOrder).FirstOrDefault()?.Url;

            var isActive = (variant?.IsActive ?? false) && (product?.IsActive ?? false);
            var unitPrice = variant?.Price ?? item.PriceAtAdd;
            var stockQuantity = variant?.StockQuantity ?? 0;
            var isPriceChanged = item.PriceAtAdd != unitPrice;
            var isStockInsufficient = item.Quantity > stockQuantity;
            var isAvailable = isActive && !isStockInsufficient;

            var notes = new List<string>();
            if (!isActive)
            {
                notes.Add("Produk atau varian tidak aktif.");
            }
            if (isStockInsufficient)
            {
                notes.Add($"Stok tidak mencukupi (tersisa {stockQuantity}).");
            }
            if (isPriceChanged)
            {
                notes.Add($"Harga berubah dari {item.PriceAtAdd:N0} menjadi {unitPrice:N0}.");
            }

            var statusNote = notes.Count > 0 ? string.Join("; ", notes) : null;
            var weightGram = variant?.WeightGram ?? 0;

            return new CartItemDto(
                item.Id,
                item.VariantId,
                product?.Id ?? Guid.Empty,
                product?.Name ?? string.Empty,
                product?.Slug ?? string.Empty,
                variant?.Sku ?? string.Empty,
                variant?.OptionSize,
                variant?.OptionColor,
                primaryImage,
                unitPrice,
                item.PriceAtAdd,
                item.Quantity,
                unitPrice * item.Quantity,
                weightGram,
                weightGram * item.Quantity,
                stockQuantity,
                isActive,
                isPriceChanged,
                isStockInsufficient,
                isAvailable,
                statusNote);
        }).ToList();

        var totalItems = items.Sum(i => i.Quantity);
        var totalWeightGram = items.Sum(i => i.TotalWeightGram);
        var subtotal = items.Where(i => i.IsAvailable).Sum(i => i.LineTotal);
        var hasIssues = items.Any(i => !i.IsAvailable || i.IsPriceChanged);

        return new CartDto(
            cart.Id,
            cart.UserId,
            items,
            totalItems,
            totalWeightGram,
            subtotal,
            hasIssues);
    }
}
