import React from 'react';
import { ProductVariant } from '@/types';
import { formatWeight } from '@/lib/format';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

export interface VariantSelectorProps {
  variants: ProductVariant[];
  selectedVariant: ProductVariant | null;
  onSelectVariant: (variant: ProductVariant) => void;
}

export const VariantSelector: React.FC<VariantSelectorProps> = ({
  variants,
  selectedVariant,
  onSelectVariant,
}) => {
  if (!variants || variants.length === 0) {
    return (
      <div className="rounded-lg border border-dashed p-4 text-sm text-muted-foreground">
        Tidak ada pilihan varian untuk produk ini.
      </div>
    );
  }

  // Check if variants have size and/or color attributes
  const hasSizes = variants.some((v) => !!v.optionSize);
  const hasColors = variants.some((v) => !!v.optionColor);

  // If variants have distinct size or color
  const distinctSizes = hasSizes
    ? Array.from(new Set(variants.map((v) => v.optionSize).filter(Boolean) as string[]))
    : [];

  const distinctColors = hasColors
    ? Array.from(new Set(variants.map((v) => v.optionColor).filter(Boolean) as string[]))
    : [];

  // If variants have both size and color
  if (hasSizes && hasColors && distinctSizes.length > 1 && distinctColors.length > 1) {
    const currentColor = selectedVariant?.optionColor || distinctColors[0];
    const currentSize = selectedVariant?.optionSize || distinctSizes[0];

    const handleSelectColor = (color: string) => {
      // Find variant with this color and current size, or first available variant with this color
      const exactMatch = variants.find(
        (v) => v.optionColor === color && v.optionSize === currentSize,
      );
      if (exactMatch && exactMatch.stockQuantity > 0 && exactMatch.isActive) {
        onSelectVariant(exactMatch);
        return;
      }
      const anyInStock = variants.find(
        (v) => v.optionColor === color && v.stockQuantity > 0 && v.isActive,
      );
      if (anyInStock) {
        onSelectVariant(anyInStock);
        return;
      }
      const fallback = variants.find((v) => v.optionColor === color);
      if (fallback) onSelectVariant(fallback);
    };

    const handleSelectSize = (size: string) => {
      const match = variants.find(
        (v) => v.optionSize === size && v.optionColor === currentColor,
      );
      if (match) {
        onSelectVariant(match);
      }
    };

    return (
      <div className="space-y-4">
        {/* Colors selector */}
        <div className="space-y-2">
          <div className="flex items-center justify-between text-xs">
            <span className="font-semibold text-foreground">Warna:</span>
            <span className="text-muted-foreground font-medium">{currentColor}</span>
          </div>
          <div className="flex flex-wrap gap-2">
            {distinctColors.map((color) => {
              // Color is out of stock if ALL variants with this color have 0 stock
              const variantsWithColor = variants.filter((v) => v.optionColor === color);
              const totalStockForColor = variantsWithColor.reduce(
                (sum, v) => sum + (v.isActive ? v.stockQuantity : 0),
                0,
              );
              const isColorOutOfStock = totalStockForColor <= 0;
              const isSelected = currentColor === color;

              return (
                <button
                  key={color}
                  type="button"
                  disabled={isColorOutOfStock}
                  onClick={() => handleSelectColor(color)}
                  className={cn(
                    'relative rounded-md border px-3 py-1.5 text-xs font-medium transition-all',
                    isSelected
                      ? 'border-primary bg-primary text-primary-foreground font-semibold shadow-sm'
                      : 'border-input bg-background hover:bg-accent hover:text-accent-foreground',
                    isColorOutOfStock &&
                      'line-through opacity-50 bg-muted/60 text-muted-foreground cursor-not-allowed border-dashed',
                  )}
                  aria-label={`Pilih warna ${color}${isColorOutOfStock ? ' (Habis)' : ''}`}
                >
                  {color}
                  {isColorOutOfStock && <span className="ml-1 text-[10px]">(Habis)</span>}
                </button>
              );
            })}
          </div>
        </div>

        {/* Sizes selector */}
        <div className="space-y-2">
          <div className="flex items-center justify-between text-xs">
            <span className="font-semibold text-foreground">Ukuran:</span>
            <span className="text-muted-foreground font-medium">{currentSize}</span>
          </div>
          <div className="flex flex-wrap gap-2">
            {distinctSizes.map((size) => {
              const variantForSize = variants.find(
                (v) => v.optionSize === size && v.optionColor === currentColor,
              );
              const isOutOfStock =
                !variantForSize || !variantForSize.isActive || variantForSize.stockQuantity <= 0;
              const isSelected = selectedVariant?.optionSize === size;

              return (
                <button
                  key={size}
                  type="button"
                  disabled={isOutOfStock}
                  onClick={() => handleSelectSize(size)}
                  className={cn(
                    'min-w-10 rounded-md border px-3 py-1.5 text-center text-xs font-medium transition-all',
                    isSelected
                      ? 'border-primary bg-primary text-primary-foreground font-semibold shadow-sm'
                      : 'border-input bg-background hover:bg-accent hover:text-accent-foreground',
                    isOutOfStock &&
                      'line-through opacity-50 bg-muted/60 text-muted-foreground cursor-not-allowed border-dashed',
                  )}
                  aria-label={`Pilih ukuran ${size}${isOutOfStock ? ' (Habis)' : ''}`}
                >
                  {size}
                </button>
              );
            })}
          </div>
        </div>

        {/* Variant Info Summary */}
        {selectedVariant && (
          <div className="rounded-md bg-muted/40 p-3 text-xs space-y-1">
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">SKU:</span>
              <span className="font-mono font-medium">{selectedVariant.sku}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Berat:</span>
              <span>{formatWeight(selectedVariant.weightGram)}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Status Stok:</span>
              {selectedVariant.stockQuantity > 5 ? (
                <span className="font-medium text-emerald-600 dark:text-emerald-400">
                  Tersedia ({selectedVariant.stockQuantity} unit)
                </span>
              ) : selectedVariant.stockQuantity > 0 ? (
                <span className="font-medium text-amber-600 dark:text-amber-400">
                  Sisa {selectedVariant.stockQuantity} unit
                </span>
              ) : (
                <span className="font-semibold text-destructive">Stok Habis</span>
              )}
            </div>
          </div>
        )}
      </div>
    );
  }

  // Otherwise, render list of all variants directly as chips
  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between text-xs">
        <span className="font-semibold text-foreground">Pilih Varian:</span>
        {selectedVariant && (
          <span className="text-muted-foreground font-medium">
            {[selectedVariant.optionSize, selectedVariant.optionColor].filter(Boolean).join(' / ') ||
              selectedVariant.sku}
          </span>
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        {variants.map((v) => {
          const isOutOfStock = !v.isActive || v.stockQuantity <= 0;
          const isSelected = selectedVariant?.id === v.id;
          const label =
            [v.optionSize, v.optionColor].filter(Boolean).join(' - ') || v.sku;

          return (
            <button
              key={v.id}
              type="button"
              disabled={isOutOfStock}
              onClick={() => onSelectVariant(v)}
              className={cn(
                'relative flex items-center gap-1.5 rounded-md border px-3 py-1.5 text-xs font-medium transition-all',
                isSelected
                  ? 'border-primary bg-primary text-primary-foreground font-semibold shadow-sm'
                  : 'border-input bg-background hover:bg-accent hover:text-accent-foreground',
                isOutOfStock &&
                  'line-through opacity-50 bg-muted/60 text-muted-foreground cursor-not-allowed border-dashed',
              )}
              aria-label={`Varian ${label}${isOutOfStock ? ' (Habis)' : ''}`}
            >
              <span>{label}</span>
              {isOutOfStock && (
                <Badge variant="outline" className="px-1 py-0 text-[9px] border-destructive/50 text-destructive">
                  Habis
                </Badge>
              )}
            </button>
          );
        })}
      </div>

      {selectedVariant && (
        <div className="rounded-md bg-muted/40 p-3 text-xs space-y-1">
          <div className="flex items-center justify-between">
            <span className="text-muted-foreground">SKU:</span>
            <span className="font-mono font-medium">{selectedVariant.sku}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-muted-foreground">Berat:</span>
            <span>{formatWeight(selectedVariant.weightGram)}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-muted-foreground">Ketersediaan:</span>
            {selectedVariant.stockQuantity > 5 ? (
              <span className="font-medium text-emerald-600 dark:text-emerald-400">
                Stok tersedia ({selectedVariant.stockQuantity} unit)
              </span>
            ) : selectedVariant.stockQuantity > 0 ? (
              <span className="font-medium text-amber-600 dark:text-amber-400">
                Sisa {selectedVariant.stockQuantity} unit
              </span>
            ) : (
              <span className="font-semibold text-destructive">Stok Habis</span>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
