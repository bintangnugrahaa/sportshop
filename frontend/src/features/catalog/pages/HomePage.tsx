import React from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, ShieldCheck, Truck, CreditCard, Sparkles } from 'lucide-react';
import { useProducts, useCategories, useBrands } from '../api';
import { ProductCard } from '../components/ProductCard';
import { ProductCardSkeleton } from '../components/ProductCardSkeleton';
import { Button } from '@/components/ui/button';

export const HomePage: React.FC = () => {
  const { data: latestProductsData, isLoading: isProductsLoading } = useProducts({
    sort: 'newest',
    pageSize: 8,
  });

  const { data: categories = [], isLoading: isCategoriesLoading } = useCategories();
  const { data: brands = [], isLoading: isBrandsLoading } = useBrands();

  return (
    <div className="flex flex-col space-y-16 pb-16">
      {/* Hero Banner Section */}
      <section className="relative overflow-hidden bg-gradient-to-br from-slate-900 via-slate-850 to-primary/20 text-white py-16 sm:py-24">
        <div className="absolute inset-0 bg-[radial-gradient(#F97316_1px,transparent_1px)] [background-size:24px_24px] opacity-15" />
        <div className="container relative mx-auto px-4 sm:px-6 lg:px-8">
          <div className="max-w-2xl space-y-6">
            <div className="inline-flex items-center gap-2 rounded-full bg-primary/20 px-3 py-1 text-xs font-semibold text-primary-foreground backdrop-blur border border-primary/30">
              <Sparkles className="h-3.5 w-3.5 text-primary" />
              <span>Koleksi Olahraga Musim 2026</span>
            </div>

            <h1 className="text-3xl sm:text-5xl lg:text-6xl font-extrabold tracking-tight leading-tight text-white">
              Tingkatkan Performa Olahraga Anda.
            </h1>

            <p className="text-base sm:text-lg text-slate-300 leading-relaxed">
              Toko online terpercaya untuk sepatu lari, jersey, raket bulutangkis, bola, dan aksesori olahraga terbaik dengan garansi 100% original.
            </p>

            <div className="flex flex-wrap items-center gap-3 pt-2">
              <Link to="/produk">
                <Button size="lg" className="gap-2 text-base font-semibold shadow-lg shadow-primary/25">
                  Belanja Sekarang
                  <ArrowRight className="h-4 w-4" />
                </Button>
              </Link>
              <Link to="/produk?sort=newest">
                <Button
                  size="lg"
                  variant="outline"
                  className="text-base bg-white/10 text-white border-white/20 hover:bg-white/20 hover:text-white"
                >
                  Produk Terbaru
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Value Propositions */}
      <section className="container mx-auto px-4 sm:px-6 lg:px-8 -mt-8 sm:-mt-12">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="flex items-start gap-4 rounded-xl border bg-card p-5 shadow-sm">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <ShieldCheck className="h-6 w-6" />
            </div>
            <div>
              <h3 className="font-semibold text-sm text-foreground">100% Original</h3>
              <p className="mt-1 text-xs text-muted-foreground leading-relaxed">
                Produk bergaransi resmi dari distributor resmi.
              </p>
            </div>
          </div>

          <div className="flex items-start gap-4 rounded-xl border bg-card p-5 shadow-sm">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Truck className="h-6 w-6" />
            </div>
            <div>
              <h3 className="font-semibold text-sm text-foreground">Ongkir Flat Terjangkau</h3>
              <p className="mt-1 text-xs text-muted-foreground leading-relaxed">
                Tarif flat transparan per provinsi tanpa biaya siluman.
              </p>
            </div>
          </div>

          <div className="flex items-start gap-4 rounded-xl border bg-card p-5 shadow-sm">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <CreditCard className="h-6 w-6" />
            </div>
            <div>
              <h3 className="font-semibold text-sm text-foreground">Pembayaran Instan</h3>
              <p className="mt-1 text-xs text-muted-foreground leading-relaxed">
                QRIS, GoPay, dan Transfer Virtual Account via Midtrans Snap.
              </p>
            </div>
          </div>

          <div className="flex items-start gap-4 rounded-xl border bg-card p-5 shadow-sm">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Sparkles className="h-6 w-6" />
            </div>
            <div>
              <h3 className="font-semibold text-sm text-foreground">Stok Akurat Real-time</h3>
              <p className="mt-1 text-xs text-muted-foreground leading-relaxed">
                Reservasi stok atomik tanpa risiko overselling.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Featured Categories Section */}
      <section className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              Kategori Unggulan
            </h2>
            <p className="text-sm text-muted-foreground mt-1">
              Temukan perlengkapan sesuai jenis olahraga favorit Anda
            </p>
          </div>
          <Link
            to="/produk"
            className="text-xs font-semibold text-primary hover:underline flex items-center gap-1"
          >
            Lihat Semua <ArrowRight className="h-3 w-3" />
          </Link>
        </div>

        {isCategoriesLoading ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-24 rounded-lg bg-muted animate-pulse" />
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {categories.slice(0, 6).map((cat) => (
              <Link
                key={cat.id}
                to={`/produk?categorySlug=${cat.slug}`}
                className="group flex flex-col items-center justify-center rounded-xl border bg-card p-5 text-center transition-all hover:border-primary hover:shadow-md"
              >
                <div className="mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary group-hover:bg-primary group-hover:text-white transition-colors">
                  <span className="font-bold text-base">{cat.name.charAt(0)}</span>
                </div>
                <h3 className="text-sm font-semibold text-foreground group-hover:text-primary transition-colors line-clamp-1">
                  {cat.name}
                </h3>
              </Link>
            ))}
          </div>
        )}
      </section>

      {/* Latest Products Section */}
      <section className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              Produk Terbaru
            </h2>
            <p className="text-sm text-muted-foreground mt-1">
              Koleksi rilisan terbaru untuk perlengkapan olahraga Anda
            </p>
          </div>
          <Link
            to="/produk?sort=newest"
            className="text-xs font-semibold text-primary hover:underline flex items-center gap-1"
          >
            Lihat Semua Produk <ArrowRight className="h-3 w-3" />
          </Link>
        </div>

        {isProductsLoading ? (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <ProductCardSkeleton key={i} />
            ))}
          </div>
        ) : latestProductsData?.items && latestProductsData.items.length > 0 ? (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {latestProductsData.items.map((prod) => (
              <ProductCard key={prod.id} product={prod} />
            ))}
          </div>
        ) : (
          <div className="rounded-xl border border-dashed p-10 text-center text-sm text-muted-foreground">
            Belum ada produk yang tersedia.
          </div>
        )}
      </section>

      {/* Brands Section */}
      {brands.length > 0 && (
        <section className="container mx-auto px-4 sm:px-6 lg:px-8">
          <div className="rounded-2xl border bg-muted/30 p-8 text-center">
            <h2 className="text-lg font-bold tracking-tight text-foreground mb-1">
              Brand Olahraga Resmi
            </h2>
            <p className="text-xs text-muted-foreground mb-6">
              Bekerja sama dengan brand olahraga ternama dunia dan nasional
            </p>

            {isBrandsLoading ? (
              <div className="h-10 bg-muted animate-pulse rounded" />
            ) : (
              <div className="flex flex-wrap items-center justify-center gap-4 sm:gap-8">
                {brands.map((b) => (
                  <Link
                    key={b.id}
                    to={`/produk?brandSlug=${b.slug}`}
                    className="rounded-lg border bg-background px-5 py-2.5 text-sm font-semibold text-foreground/80 hover:border-primary hover:text-primary transition-all shadow-sm"
                  >
                    {b.name}
                  </Link>
                ))}
              </div>
            )}
          </div>
        </section>
      )}
    </div>
  );
};

export default HomePage;
