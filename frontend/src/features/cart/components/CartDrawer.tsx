import React from 'react';
import { useNavigate } from 'react-router-dom';
import { X, ShoppingBag, ArrowRight, AlertTriangle } from 'lucide-react';
import { useUiStore } from '@/stores/uiStore';
import { useCart, useUpdateCartItem, useRemoveCartItem } from '../api';
import { CartItemCard } from './CartItemCard';
import { Price } from '@/components/shared/Price';
import { Button } from '@/components/ui/button';
import { toast } from '@/lib/toast';

export const CartDrawer: React.FC = () => {
  const navigate = useNavigate();
  const { isCartDrawerOpen, closeCartDrawer } = useUiStore();
  const { data: cart, isLoading } = useCart(isCartDrawerOpen);

  const updateItemMutation = useUpdateCartItem();
  const removeItemMutation = useRemoveCartItem();

  if (!isCartDrawerOpen) return null;

  const handleUpdateQuantity = async (variantId: string, quantity: number) => {
    try {
      await updateItemMutation.mutateAsync({ variantId, quantity });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal mengubah jumlah item.';
      toast.error(msg);
    }
  };

  const handleRemoveItem = async (variantId: string) => {
    try {
      await removeItemMutation.mutateAsync(variantId);
      toast.success('Item berhasil dihapus dari keranjang.');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal menghapus item.';
      toast.error(msg);
    }
  };

  const handleGoToCheckout = () => {
    closeCartDrawer();
    navigate('/checkout');
  };

  const handleGoToCartPage = () => {
    closeCartDrawer();
    navigate('/keranjang');
  };

  return (
    <div className="fixed inset-0 z-50 flex justify-end" role="dialog" aria-modal="true">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/50 backdrop-blur-sm transition-opacity animate-in fade-in"
        onClick={closeCartDrawer}
        aria-hidden="true"
      />

      {/* Drawer Panel */}
      <div className="relative z-50 flex h-full w-full max-w-md flex-col bg-background shadow-2xl animate-in slide-in-from-right duration-200">
        {/* Header */}
        <div className="flex items-center justify-between border-b px-5 py-4">
          <div className="flex items-center gap-2">
            <ShoppingBag className="h-5 w-5 text-primary" />
            <h2 className="text-base font-bold text-foreground">
              Keranjang ({cart?.totalItems || 0})
            </h2>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={closeCartDrawer}
            className="h-8 w-8 p-0"
            aria-label="Tutup keranjang"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Warning Banner if hasIssues */}
        {cart?.hasIssues && (
          <div className="flex items-center gap-2 bg-amber-500/10 px-5 py-3 text-xs text-amber-700 dark:text-amber-300 border-b border-amber-200 dark:border-amber-900/50">
            <AlertTriangle className="h-4 w-4 flex-shrink-0" />
            <span>Beberapa item mengalami perubahan harga atau ketersediaan stok.</span>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-y-auto px-5 py-4 space-y-3">
          {isLoading ? (
            <div className="flex h-40 items-center justify-center">
              <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
            </div>
          ) : !cart || cart.items.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <div className="flex h-16 w-16 items-center justify-center rounded-full bg-muted text-muted-foreground mb-4">
                <ShoppingBag className="h-8 w-8" />
              </div>
              <p className="text-base font-semibold text-foreground">Keranjang Anda Masih Kosong</p>
              <p className="mt-1 text-xs text-muted-foreground max-w-xs">
                Yuk jelajahi berbagai perlengkapan olahraga terbaik dan tambahkan ke keranjang!
              </p>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  closeCartDrawer();
                  navigate('/produk');
                }}
                className="mt-4"
              >
                Mulai Belanja
              </Button>
            </div>
          ) : (
            cart.items.map((item) => (
              <CartItemCard
                key={item.id}
                item={item}
                onUpdateQuantity={(qty) => handleUpdateQuantity(item.variantId, qty)}
                onRemove={() => handleRemoveItem(item.variantId)}
                isUpdating={updateItemMutation.isPending || removeItemMutation.isPending}
              />
            ))
          )}
        </div>

        {/* Footer */}
        {cart && cart.items.length > 0 && (
          <div className="border-t bg-muted/30 p-5 space-y-3">
            <div className="space-y-1">
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span>Total Berat</span>
                <span>{(cart.totalWeightGram / 1000).toFixed(2)} kg</span>
              </div>
              <div className="flex items-center justify-between text-sm font-semibold">
                <span>Subtotal</span>
                <Price price={cart.subtotal} className="text-lg font-bold text-primary" />
              </div>
              <p className="text-[11px] text-muted-foreground">
                *Ongkos kirim akan dihitung saat checkout berdasarkan alamat.
              </p>
            </div>

            <div className="space-y-2 pt-1">
              <Button
                className="w-full flex items-center justify-center gap-2"
                onClick={handleGoToCheckout}
                disabled={cart.hasIssues || cart.items.length === 0}
              >
                <span>Lanjut ke Checkout</span>
                <ArrowRight className="h-4 w-4" />
              </Button>

              <Button
                variant="outline"
                className="w-full"
                onClick={handleGoToCartPage}
              >
                Lihat Keranjang Lengkap
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
