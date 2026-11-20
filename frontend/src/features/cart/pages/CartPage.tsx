import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ShoppingBag, ArrowRight, Trash2, AlertTriangle, ShieldCheck } from 'lucide-react';
import { useCart, useUpdateCartItem, useRemoveCartItem, useClearCart } from '../api';
import { CartItemCard } from '../components/CartItemCard';
import { Price } from '@/components/shared/Price';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/lib/toast';

export const CartPage: React.FC = () => {
  const navigate = useNavigate();
  const { data: cart, isLoading } = useCart();
  const updateItemMutation = useUpdateCartItem();
  const removeItemMutation = useRemoveCartItem();
  const clearCartMutation = useClearCart();

  const handleUpdateQuantity = async (variantId: string, quantity: number) => {
    try {
      await updateItemMutation.mutateAsync({ variantId, quantity });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal memperbarui jumlah item.';
      toast.error(msg);
    }
  };

  const handleRemoveItem = async (variantId: string) => {
    try {
      await removeItemMutation.mutateAsync(variantId);
      toast.success('Item berhasil dihapus.');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal menghapus item.';
      toast.error(msg);
    }
  };

  const handleClearCart = async () => {
    if (window.confirm('Apakah Anda yakin ingin mengosongkan seluruh keranjang belanja?')) {
      try {
        await clearCartMutation.mutateAsync();
        toast.success('Keranjang belanja berhasil dikosongkan.');
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : 'Gagal mengosongkan keranjang.';
        toast.error(msg);
      }
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-12 flex justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="container mx-auto px-4 py-16 text-center max-w-md">
        <div className="flex h-20 w-20 items-center justify-center rounded-full bg-muted text-muted-foreground mx-auto mb-4">
          <ShoppingBag className="h-10 w-10" />
        </div>
        <h1 className="text-2xl font-bold tracking-tight">Keranjang Belanja Kosong</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Anda belum memiliki produk di dalam keranjang belanja. Jelajahi katalog perlengkapan
          olahraga kami sekarang!
        </p>
        <Button className="mt-6" onClick={() => navigate('/produk')}>
          Jelajahi Produk
        </Button>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 sm:px-6 lg:px-8">
      {/* Title */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground">
            Keranjang Belanja
          </h1>
          <p className="text-sm text-muted-foreground">
            Total {cart.totalItems} item produk ({((cart.totalWeightGram || 0) / 1000).toFixed(2)} kg)
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={handleClearCart}
          disabled={clearCartMutation.isPending}
          className="text-destructive hover:bg-destructive/10 border-destructive/30 self-start sm:self-auto"
        >
          <Trash2 className="mr-2 h-4 w-4" />
          Kosongkan Keranjang
        </Button>
      </div>

      {/* Warning banner when there are issues */}
      {cart.hasIssues && (
        <div className="mb-6 flex items-start gap-3 rounded-lg border border-amber-300 bg-amber-50 p-4 text-sm text-amber-800 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-200">
          <AlertTriangle className="h-5 w-5 flex-shrink-0 text-amber-600 dark:text-amber-400 mt-0.5" />
          <div>
            <h4 className="font-semibold">Perhatian: Perubahan pada Keranjang Anda</h4>
            <p className="mt-1 text-xs sm:text-sm">
              Beberapa produk di keranjang Anda mengalami perubahan harga atau ketersediaan stok
              terbaru. Harap periksa dan sesuaikan item bertanda khusus sebelum melanjutkan ke checkout.
            </p>
          </div>
        </div>
      )}

      {/* Grid: Items list & Order summary */}
      <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
        {/* Left: Cart Items */}
        <div className="lg:col-span-8 space-y-4">
          {cart.items.map((item) => (
            <CartItemCard
              key={item.id}
              item={item}
              onUpdateQuantity={(qty) => handleUpdateQuantity(item.variantId, qty)}
              onRemove={() => handleRemoveItem(item.variantId)}
              isUpdating={updateItemMutation.isPending || removeItemMutation.isPending}
            />
          ))}

          <div className="pt-2">
            <Link
              to="/produk"
              className="text-sm font-medium text-primary hover:underline inline-flex items-center gap-1"
            >
              ← Lanjut Belanja Produk Lainnya
            </Link>
          </div>
        </div>

        {/* Right: Summary Card */}
        <div className="lg:col-span-4">
          <Card className="sticky top-24">
            <CardHeader>
              <CardTitle className="text-lg">Ringkasan Belanja</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2 text-sm">
                <div className="flex justify-between text-muted-foreground">
                  <span>Total Barang</span>
                  <span>{cart.totalItems} item</span>
                </div>
                <div className="flex justify-between text-muted-foreground">
                  <span>Total Berat</span>
                  <span>{((cart.totalWeightGram || 0) / 1000).toFixed(2)} kg</span>
                </div>
                <div className="flex justify-between text-muted-foreground">
                  <span>Estimasi Ongkos Kirim</span>
                  <span className="italic">Dihitung saat checkout</span>
                </div>
                <div className="border-t pt-3 flex justify-between font-bold text-base">
                  <span>Subtotal</span>
                  <Price price={cart.subtotal} className="text-xl text-primary" />
                </div>
              </div>

              {cart.hasIssues && (
                <p className="text-xs text-destructive font-medium">
                  Harap sesuaikan item yang tidak memenuhi syarat stok/ketersediaan untuk lanjut ke checkout.
                </p>
              )}

              <Button
                className="w-full h-11 text-base font-semibold"
                disabled={cart.hasIssues || cart.items.length === 0}
                onClick={() => navigate('/checkout')}
              >
                <span>Lanjut ke Checkout</span>
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>

              <div className="flex items-center justify-center gap-2 text-xs text-muted-foreground pt-2">
                <ShieldCheck className="h-4 w-4 text-emerald-600" />
                <span>Transaksi Terenkripsi & Pembayaran Aman via Midtrans</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
};

export default CartPage;
