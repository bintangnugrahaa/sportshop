import React from 'react';
import { Link } from 'react-router-dom';
import { Trash2, Plus, Minus, AlertTriangle, AlertCircle } from 'lucide-react';
import { CartItem } from '@/types';
import { Price } from '@/components/shared/Price';
import { Button } from '@/components/ui/button';

interface CartItemCardProps {
  item: CartItem;
  onUpdateQuantity: (quantity: number) => void;
  onRemove: () => void;
  isUpdating?: boolean;
}

export const CartItemCard: React.FC<CartItemCardProps> = ({
  item,
  onUpdateQuantity,
  onRemove,
  isUpdating = false,
}) => {
  const variantLabel = [item.optionSize, item.optionColor].filter(Boolean).join(' / ');

  return (
    <div
      className={`rounded-lg border p-4 transition-colors ${
        item.isPriceChanged || item.isStockInsufficient || !item.isAvailable
          ? 'border-amber-400 bg-amber-50/40 dark:bg-amber-950/20'
          : 'bg-card'
      }`}
      data-testid={`cart-item-${item.variantId}`}
    >
      <div className="flex gap-4">
        {/* Product image */}
        <Link
          to={`/produk/${item.productSlug}`}
          className="relative h-20 w-20 flex-shrink-0 overflow-hidden rounded-md border bg-muted"
        >
          {item.primaryImageUrl ? (
            <img
              src={item.primaryImageUrl}
              alt={item.productName}
              className="h-full w-full object-cover"
              loading="lazy"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center text-xs text-muted-foreground">
              No Image
            </div>
          )}
        </Link>

        {/* Product Details */}
        <div className="flex flex-1 flex-col justify-between">
          <div>
            <div className="flex items-start justify-between gap-2">
              <Link
                to={`/produk/${item.productSlug}`}
                className="font-medium text-foreground hover:text-primary transition-colors line-clamp-1"
              >
                {item.productName}
              </Link>
              <Button
                variant="ghost"
                size="sm"
                onClick={onRemove}
                disabled={isUpdating}
                className="h-8 w-8 p-0 text-muted-foreground hover:text-destructive"
                aria-label={`Hapus ${item.productName}`}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>

            <p className="mt-0.5 text-xs text-muted-foreground">
              {variantLabel ? `Varian: ${variantLabel}` : `SKU: ${item.sku}`}
            </p>

            <div className="mt-1 flex items-baseline gap-2">
              <Price price={item.unitPrice} className="font-semibold text-primary" />
              <span className="text-xs text-muted-foreground">
                ({(item.weightGram / 1000).toFixed(2)} kg)
              </span>
            </div>
          </div>

          {/* Stepper and Line Total */}
          <div className="mt-3 flex items-center justify-between gap-4">
            <div className="flex items-center rounded-md border bg-background">
              <button
                type="button"
                onClick={() => onUpdateQuantity(item.quantity - 1)}
                disabled={isUpdating || item.quantity <= 1}
                className="flex h-8 w-8 items-center justify-center text-muted-foreground hover:text-foreground disabled:opacity-40"
                aria-label="Kurangi jumlah"
              >
                <Minus className="h-3 w-3" />
              </button>
              <span
                className="flex h-8 w-10 items-center justify-center text-xs font-semibold"
                data-testid={`quantity-${item.variantId}`}
              >
                {item.quantity}
              </span>
              <button
                type="button"
                onClick={() => onUpdateQuantity(item.quantity + 1)}
                disabled={isUpdating || item.quantity >= item.stockQuantity}
                className="flex h-8 w-8 items-center justify-center text-muted-foreground hover:text-foreground disabled:opacity-40"
                aria-label="Tambah jumlah"
              >
                <Plus className="h-3 w-3" />
              </button>
            </div>

            <div className="text-right">
              <span className="text-xs text-muted-foreground block">Subtotal</span>
              <Price price={item.lineTotal} className="font-bold text-foreground" />
            </div>
          </div>
        </div>
      </div>

      {/* Warnings & Notes */}
      {(item.isPriceChanged || item.isStockInsufficient || !item.isAvailable || item.statusNote) && (
        <div className="mt-3 space-y-1.5 border-t pt-2 text-xs">
          {item.isPriceChanged && (
            <div className="flex items-center gap-1.5 text-amber-600 font-medium">
              <AlertTriangle className="h-3.5 w-3.5 flex-shrink-0" />
              <span>
                Harga berubah dari <Price price={item.priceAtAdd} /> menjadi{' '}
                <Price price={item.unitPrice} />.
              </span>
            </div>
          )}

          {item.isStockInsufficient && (
            <div className="flex items-center gap-1.5 text-destructive font-medium">
              <AlertCircle className="h-3.5 w-3.5 flex-shrink-0" />
              <span>
                Stok tidak mencukupi! Tersisa hanya {item.stockQuantity} item di gudang.
              </span>
            </div>
          )}

          {!item.isAvailable && (
            <div className="flex items-center gap-1.5 text-destructive font-medium">
              <AlertCircle className="h-3.5 w-3.5 flex-shrink-0" />
              <span>Varian produk ini saat ini sedang tidak aktif atau tidak tersedia.</span>
            </div>
          )}

          {item.statusNote && !item.isPriceChanged && !item.isStockInsufficient && (
            <p className="text-muted-foreground">{item.statusNote}</p>
          )}
        </div>
      )}
    </div>
  );
};
