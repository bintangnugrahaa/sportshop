import React from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { ShoppingBag, ChevronRight, ShieldCheck, Truck, RefreshCcw, Minus, Plus } from 'lucide-react';
import { useProductBySlug } from '../api';
import { ProductGallery } from '../components/ProductGallery';
import { VariantSelector } from '../components/VariantSelector';
import { ProductVariant } from '@/types';
import { useAuthStore } from '@/stores/authStore';
import { useAddToCart } from '@/features/cart/api';
import { Price } from '@/components/shared/Price';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { ErrorState } from '@/components/shared/ErrorState';
import { toast } from '@/lib/toast';

export const ProductDetailPage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuthStore();
  const { data: product, isLoading, isError, refetch } = useProductBySlug(slug || '');
  const addToCartMutation = useAddToCart();

  const [selectedVariant, setSelectedVariant] = React.useState<ProductVariant | null>(null);
  const [quantity, setQuantity] = React.useState<number>(1);

  // Initialize selected variant once product loads
  React.useEffect(() => {
    if (product && product.variants && product.variants.length > 0) {
      // Find first in-stock active variant, or fallback to first variant
      const firstAvailable =
        product.variants.find((v) => v.isActive && v.stockQuantity > 0) ||
        product.variants[0] ||
        null;
      setSelectedVariant(firstAvailable);
      setQuantity(1);
    }
  }, [product]);

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8 sm:px-6 lg:px-8">
        <Skeleton className="h-4 w-48 mb-6" />
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
          <Skeleton className="aspect-square w-full rounded-xl" />
          <div className="space-y-4">
            <Skeleton className="h-6 w-32" />
            <Skeleton className="h-8 w-3/4" />
            <Skeleton className="h-8 w-40" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-12 w-full" />
          </div>
        </div>
      </div>
    );
  }

  if (isError || !product) {
    return (
      <div className="container mx-auto px-4 py-16 sm:px-6 lg:px-8">
        <ErrorState
          title="Produk Tidak Ditemukan"
          message="Produk yang Anda cari mungkin telah dinonaktifkan atau dihapus."
          onRetry={() => refetch()}
        />
        <div className="mt-6 flex justify-center">
          <Link to="/produk">
            <Button variant="outline">Kembali ke Katalog</Button>
          </Link>
        </div>
      </div>
    );
  }

  const isOutOfStock =
    !selectedVariant || selectedVariant.stockQuantity <= 0 || !selectedVariant.isActive;

  const currentPrice = selectedVariant ? selectedVariant.price : product.basePrice;
  const maxStock = selectedVariant ? selectedVariant.stockQuantity : 1;

  const handleDecreaseQuantity = () => {
    setQuantity((prev) => Math.max(1, prev - 1));
  };

  const handleIncreaseQuantity = () => {
    setQuantity((prev) => Math.min(maxStock, prev + 1));
  };

  const handleAddToCart = async () => {
    if (!selectedVariant) return;

    if (!isAuthenticated) {
      toast.info('Silakan masuk terlebih dahulu untuk menambahkan produk ke keranjang.');
      navigate(`/masuk?redirect=${encodeURIComponent(`/produk/${product.slug}`)}`);
      return;
    }

    try {
      await addToCartMutation.mutateAsync({
        variantId: selectedVariant.id,
        quantity,
      });
      toast.success(
        `${product.name} (${[selectedVariant.optionSize, selectedVariant.optionColor].filter(Boolean).join('/') || selectedVariant.sku}) berhasil ditambahkan ke keranjang!`,
      );
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : 'Gagal menambahkan produk ke keranjang.';
      toast.error(message);
    }
  };

  return (
    <div className="container mx-auto px-4 py-6 sm:px-6 lg:px-8">
      {/* Breadcrumb Navigation */}
      <nav className="mb-6 flex items-center gap-2 text-xs text-muted-foreground flex-wrap">
        <Link to="/" className="hover:text-foreground">
          Beranda
        </Link>
        <ChevronRight className="h-3 w-3" />
        <Link to="/produk" className="hover:text-foreground">
          Katalog
        </Link>
        {product.categorySlug && (
          <>
            <ChevronRight className="h-3 w-3" />
            <Link
              to={`/produk?categorySlug=${product.categorySlug}`}
              className="hover:text-foreground"
            >
              {product.categoryName || product.categorySlug}
            </Link>
          </>
        )}
        <ChevronRight className="h-3 w-3" />
        <span className="text-foreground font-medium truncate max-w-[200px] sm:max-w-none">
          {product.name}
        </span>
      </nav>

      {/* Main 2-Column Product Detail Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-10 xl:gap-14">
        {/* Left: Gallery */}
        <div>
          <ProductGallery images={product.images} productName={product.name} />
        </div>

        {/* Right: Purchase and Details Box */}
        <div className="flex flex-col space-y-6">
          {/* Header & Brand */}
          <div className="space-y-2">
            <div className="flex items-center gap-2 text-xs font-semibold tracking-wider uppercase text-muted-foreground">
              {product.brandName && (
                <Link
                  to={`/produk?brandSlug=${product.brandSlug}`}
                  className="text-primary hover:underline"
                >
                  {product.brandName}
                </Link>
              )}
              {product.brandName && product.categoryName && <span>•</span>}
              {product.categoryName && <span>{product.categoryName}</span>}
            </div>

            <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground">
              {product.name}
            </h1>

            {/* Price display */}
            <div className="pt-2">
              <Price price={currentPrice} className="text-2xl sm:text-3xl" />
            </div>
          </div>

          <div className="border-t pt-4">
            {/* Variant Selector */}
            <VariantSelector
              variants={product.variants || []}
              selectedVariant={selectedVariant}
              onSelectVariant={(variant) => {
                setSelectedVariant(variant);
                setQuantity(1);
              }}
            />
          </div>

          {/* Quantity Stepper & Add to Cart Button */}
          <div className="space-y-4 pt-2 border-t">
            <div className="flex items-center gap-4">
              <span className="text-xs font-semibold text-foreground">Jumlah:</span>
              <div className="flex items-center rounded-md border bg-background">
                <button
                  type="button"
                  onClick={handleDecreaseQuantity}
                  disabled={quantity <= 1 || isOutOfStock}
                  className="flex h-9 w-9 items-center justify-center text-foreground hover:bg-accent disabled:opacity-40"
                  aria-label="Kurangi jumlah"
                >
                  <Minus className="h-3.5 w-3.5" />
                </button>
                <span className="w-10 text-center text-sm font-semibold">
                  {isOutOfStock ? 0 : quantity}
                </span>
                <button
                  type="button"
                  onClick={handleIncreaseQuantity}
                  disabled={quantity >= maxStock || isOutOfStock}
                  className="flex h-9 w-9 items-center justify-center text-foreground hover:bg-accent disabled:opacity-40"
                  aria-label="Tambah jumlah"
                >
                  <Plus className="h-3.5 w-3.5" />
                </button>
              </div>
              {selectedVariant && !isOutOfStock && (
                <span className="text-xs text-muted-foreground">
                  (Maks. {selectedVariant.stockQuantity} unit)
                </span>
              )}
            </div>

            {/* Add to Cart CTA */}
            <Button
              size="lg"
              className="w-full gap-2 text-base font-semibold"
              disabled={isOutOfStock || addToCartMutation.isPending}
              onClick={handleAddToCart}
            >
              <ShoppingBag className="h-5 w-5" />
              {isOutOfStock
                ? 'Stok Varian Habis'
                : addToCartMutation.isPending
                  ? 'Menambahkan...'
                  : 'Tambah ke Keranjang'}
            </Button>
          </div>

          {/* Trust Badges */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 pt-4 border-t text-xs text-muted-foreground">
            <div className="flex items-center gap-2">
              <ShieldCheck className="h-4 w-4 text-primary shrink-0" />
              <span>100% Original &amp; Resmi</span>
            </div>
            <div className="flex items-center gap-2">
              <Truck className="h-4 w-4 text-primary shrink-0" />
              <span>Pengiriman Seluruh RI</span>
            </div>
            <div className="flex items-center gap-2">
              <RefreshCcw className="h-4 w-4 text-primary shrink-0" />
              <span>Pembayaran Aman Snap</span>
            </div>
          </div>
        </div>
      </div>

      {/* Product Description Section */}
      <section className="mt-14 border-t pt-8">
        <h2 className="text-xl font-bold text-foreground mb-4">Deskripsi Produk</h2>
        <div className="prose max-w-none text-sm text-foreground/85 leading-relaxed whitespace-pre-line bg-muted/20 p-6 rounded-xl border">
          {product.description || 'Tidak ada deskripsi rinci untuk produk ini.'}
        </div>
      </section>
    </div>
  );
};

export default ProductDetailPage;
