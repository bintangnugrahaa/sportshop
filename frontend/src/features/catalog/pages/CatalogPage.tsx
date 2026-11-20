import React from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { SlidersHorizontal, Search, X, ChevronLeft, ChevronRight } from 'lucide-react';
import { useProducts } from '../api';
import { ProductCard } from '../components/ProductCard';
import { ProductCardSkeleton } from '../components/ProductCardSkeleton';
import { ProductFilters, FilterState } from '../components/ProductFilters';
import { Button } from '@/components/ui/button';
import { EmptyState } from '@/components/shared/EmptyState';
import { ErrorState } from '@/components/shared/ErrorState';

export const CatalogPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const [isMobileFiltersOpen, setIsMobileFiltersOpen] = React.useState(false);

  // Extract filter parameters from URL Query
  const q = searchParams.get('q') || undefined;
  const categorySlug = searchParams.get('categorySlug') || undefined;
  const brandSlug = searchParams.get('brandSlug') || undefined;
  const minPriceParam = searchParams.get('minPrice');
  const minPrice = minPriceParam ? parseInt(minPriceParam, 10) : undefined;
  const maxPriceParam = searchParams.get('maxPrice');
  const maxPrice = maxPriceParam ? parseInt(maxPriceParam, 10) : undefined;
  const inStockParam = searchParams.get('inStock');
  const inStock = inStockParam === 'true' ? true : undefined;
  const sort = searchParams.get('sort') || 'newest';
  const pageParam = searchParams.get('page');
  const page = pageParam ? Math.max(1, parseInt(pageParam, 10)) : 1;
  const pageSize = 12;

  // Local search query input state
  const [searchInput, setSearchInput] = React.useState(q || '');

  React.useEffect(() => {
    setSearchInput(q || '');
  }, [q]);

  // Query products from server
  const { data, isLoading, isError, refetch } = useProducts({
    q,
    categorySlug,
    brandSlug,
    minPrice,
    maxPrice,
    inStock,
    sort,
    page,
    pageSize,
  });

  // URL query helper to maintain filter state
  const updateFilters = (updates: Record<string, string | null | undefined>) => {
    const nextParams = new URLSearchParams(searchParams);
    for (const [key, value] of Object.entries(updates)) {
      if (value === undefined || value === null || value === '') {
        nextParams.delete(key);
      } else {
        nextParams.set(key, value);
      }
    }
    // Reset to page 1 unless page itself was updated
    if (!('page' in updates)) {
      nextParams.delete('page');
    }
    setSearchParams(nextParams);
  };

  const handleFilterChange = (newFilters: Partial<FilterState>) => {
    const updates: Record<string, string | null | undefined> = {};
    if ('categorySlug' in newFilters) updates.categorySlug = newFilters.categorySlug;
    if ('brandSlug' in newFilters) updates.brandSlug = newFilters.brandSlug;
    if ('minPrice' in newFilters)
      updates.minPrice = newFilters.minPrice !== undefined ? newFilters.minPrice.toString() : null;
    if ('maxPrice' in newFilters)
      updates.maxPrice = newFilters.maxPrice !== undefined ? newFilters.maxPrice.toString() : null;
    if ('inStock' in newFilters)
      updates.inStock = newFilters.inStock !== undefined ? newFilters.inStock.toString() : null;
    updateFilters(updates);
  };

  const handleResetFilters = () => {
    const nextParams = new URLSearchParams();
    if (sort && sort !== 'newest') nextParams.set('sort', sort);
    setSearchParams(nextParams);
    setIsMobileFiltersOpen(false);
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateFilters({ q: searchInput.trim() || null });
  };

  const handleClearSearch = () => {
    setSearchInput('');
    updateFilters({ q: null });
  };

  const handleSortChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    updateFilters({ sort: e.target.value });
  };

  const handlePageChange = (newPage: number) => {
    updateFilters({ page: newPage > 1 ? newPage.toString() : null });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const currentFilters: FilterState = {
    categorySlug,
    brandSlug,
    minPrice,
    maxPrice,
    inStock,
  };

  const activeFilterCount =
    (categorySlug ? 1 : 0) +
    (brandSlug ? 1 : 0) +
    (minPrice !== undefined || maxPrice !== undefined ? 1 : 0) +
    (inStock !== undefined ? 1 : 0) +
    (q ? 1 : 0);

  return (
    <div className="container mx-auto px-4 py-6 sm:px-6 lg:px-8">
      {/* Breadcrumb */}
      <nav className="mb-4 flex items-center gap-2 text-xs text-muted-foreground">
        <Link to="/" className="hover:text-foreground">
          Beranda
        </Link>
        <span>/</span>
        <span className="text-foreground font-medium">Katalog Produk</span>
      </nav>

      {/* Title & Toolbar */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between pb-6 border-b">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground">
            Katalog Produk
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {isLoading
              ? 'Memuat produk...'
              : data
                ? `Menampilkan ${data.items.length} dari ${data.totalItems} produk`
                : 'Pilihan perlengkapan olahraga terbaik'}
          </p>
        </div>

        {/* Toolbar: Search & Sort & Mobile Filter Button */}
        <div className="flex flex-wrap items-center gap-3">
          {/* In-page Search */}
          <form onSubmit={handleSearchSubmit} className="relative flex-1 sm:w-64 sm:flex-initial">
            <input
              type="text"
              placeholder="Cari dalam katalog..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="w-full rounded-md border border-input bg-background px-3 py-1.5 pl-8 pr-8 text-xs focus:outline-none focus:ring-2 focus:ring-primary h-9"
            />
            <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
            {searchInput && (
              <button
                type="button"
                onClick={handleClearSearch}
                className="absolute right-2.5 top-2.5 text-muted-foreground hover:text-foreground"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            )}
          </form>

          {/* Sort Select */}
          <div className="flex items-center gap-1.5">
            <label htmlFor="sortSelect" className="text-xs text-muted-foreground hidden sm:inline">
              Urutkan:
            </label>
            <select
              id="sortSelect"
              value={sort}
              onChange={handleSortChange}
              className="h-9 rounded-md border border-input bg-background px-3 py-1 text-xs font-medium focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <option value="newest">Terbaru</option>
              <option value="price_asc">Harga Terendah</option>
              <option value="price_desc">Harga Tertinggi</option>
              <option value="name">Nama A-Z</option>
            </select>
          </div>

          {/* Mobile Filter Toggle Button */}
          <Button
            variant="outline"
            size="sm"
            onClick={() => setIsMobileFiltersOpen(true)}
            className="lg:hidden flex items-center gap-2 h-9 text-xs"
          >
            <SlidersHorizontal className="h-3.5 w-3.5" />
            <span>Filter</span>
            {activeFilterCount > 0 && (
              <span className="flex h-5 w-5 items-center justify-center rounded-full bg-primary text-[10px] font-bold text-white">
                {activeFilterCount}
              </span>
            )}
          </Button>
        </div>
      </div>

      {/* Active Filter Chips */}
      {activeFilterCount > 0 && (
        <div className="flex flex-wrap items-center gap-2 py-3 border-b">
          <span className="text-xs text-muted-foreground">Filter aktif:</span>
          {q && (
            <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
              Kata kunci: &ldquo;{q}&rdquo;
              <button onClick={handleClearSearch} className="hover:text-destructive">
                <X className="h-3 w-3" />
              </button>
            </span>
          )}
          {categorySlug && (
            <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
              Kategori: {categorySlug}
              <button
                onClick={() => updateFilters({ categorySlug: null })}
                className="hover:text-destructive"
              >
                <X className="h-3 w-3" />
              </button>
            </span>
          )}
          {brandSlug && (
            <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
              Brand: {brandSlug}
              <button
                onClick={() => updateFilters({ brandSlug: null })}
                className="hover:text-destructive"
              >
                <X className="h-3 w-3" />
              </button>
            </span>
          )}
          {(minPrice !== undefined || maxPrice !== undefined) && (
            <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
              Harga: Rp {minPrice || 0} - Rp {maxPrice || '∞'}
              <button
                onClick={() => updateFilters({ minPrice: null, maxPrice: null })}
                className="hover:text-destructive"
              >
                <X className="h-3 w-3" />
              </button>
            </span>
          )}
          {inStock && (
            <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
              Tersedia Saja
              <button
                onClick={() => updateFilters({ inStock: null })}
                className="hover:text-destructive"
              >
                <X className="h-3 w-3" />
              </button>
            </span>
          )}
          <button
            onClick={handleResetFilters}
            className="text-xs font-medium text-primary hover:underline ml-2"
          >
            Hapus Semua
          </button>
        </div>
      )}

      {/* Main Content Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 pt-6">
        {/* Desktop Sidebar Filters */}
        <aside className="hidden lg:block lg:col-span-1">
          <div className="sticky top-20 rounded-lg border bg-card p-5">
            <ProductFilters
              filters={currentFilters}
              onFilterChange={handleFilterChange}
              onReset={handleResetFilters}
            />
          </div>
        </aside>

        {/* Mobile Filters Drawer Modal */}
        {isMobileFiltersOpen && (
          <div className="fixed inset-0 z-50 flex lg:hidden">
            <div
              className="fixed inset-0 bg-black/50 backdrop-blur-sm"
              onClick={() => setIsMobileFiltersOpen(false)}
            />
            <div className="relative ml-auto flex h-full w-full max-w-xs flex-col bg-background p-6 shadow-xl overflow-y-auto">
              <div className="flex items-center justify-between pb-4 border-b mb-4">
                <h2 className="font-semibold text-lg">Filter</h2>
                <button
                  onClick={() => setIsMobileFiltersOpen(false)}
                  className="rounded p-1 text-muted-foreground hover:text-foreground"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>
              <ProductFilters
                filters={currentFilters}
                onFilterChange={handleFilterChange}
                onReset={handleResetFilters}
                onCloseMobile={() => setIsMobileFiltersOpen(false)}
              />
            </div>
          </div>
        )}

        {/* Product Grid Area */}
        <section className="lg:col-span-3">
          {isLoading ? (
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-4">
              {Array.from({ length: 8 }).map((_, i) => (
                <ProductCardSkeleton key={i} />
              ))}
            </div>
          ) : isError ? (
            <ErrorState
              title="Gagal Memuat Katalog"
              message="Terjadi kendala saat mengambil data produk. Silakan coba kembali."
              onRetry={() => refetch()}
            />
          ) : !data || data.items.length === 0 ? (
            <EmptyState
              title="Produk Tidak Ditemukan"
              description="Tidak ada produk yang sesuai dengan kriteria filter atau pencarian Anda."
              actionLabel="Reset Filter"
              onAction={handleResetFilters}
            />
          ) : (
            <div className="space-y-8">
              {/* Products 2-col (mobile) -> 3-col (tablet) -> 4-col (desktop) */}
              <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-4">
                {data.items.map((product) => (
                  <ProductCard key={product.id} product={product} />
                ))}
              </div>

              {/* Pagination Controls */}
              {data.totalPages > 1 && (
                <div className="flex items-center justify-between border-t pt-6">
                  <p className="text-xs text-muted-foreground">
                    Halaman <span className="font-medium text-foreground">{data.page}</span> dari{' '}
                    <span className="font-medium text-foreground">{data.totalPages}</span>
                  </p>

                  <div className="flex items-center gap-1.5">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handlePageChange(data.page - 1)}
                      disabled={!data.hasPreviousPage}
                      className="gap-1 text-xs"
                    >
                      <ChevronLeft className="h-4 w-4" />
                      Sebelumnya
                    </Button>

                    <div className="hidden sm:flex items-center gap-1">
                      {Array.from({ length: data.totalPages }, (_, idx) => idx + 1)
                        .filter(
                          (p) =>
                            p === 1 ||
                            p === data.totalPages ||
                            Math.abs(p - data.page) <= 1,
                        )
                        .map((p, idx, arr) => (
                          <React.Fragment key={p}>
                            {idx > 0 && arr[idx - 1] !== p - 1 && (
                              <span className="px-1 text-xs text-muted-foreground">...</span>
                            )}
                            <Button
                              variant={p === data.page ? 'default' : 'outline'}
                              size="sm"
                              className="h-8 w-8 p-0 text-xs"
                              onClick={() => handlePageChange(p)}
                            >
                              {p}
                            </Button>
                          </React.Fragment>
                        ))}
                    </div>

                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handlePageChange(data.page + 1)}
                      disabled={!data.hasNextPage}
                      className="gap-1 text-xs"
                    >
                      Selanjutnya
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </section>
      </div>
    </div>
  );
};

export default CatalogPage;
