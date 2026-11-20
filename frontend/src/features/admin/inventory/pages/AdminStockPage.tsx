import React from 'react';
import {
  Boxes,
  AlertTriangle,
  Search,
  RefreshCw,
  ArrowUpDown,
  Filter,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Price } from '@/components/shared/Price';
import { toast } from '@/lib/toast';
import type { Product, ProductVariant } from '@/types';
import {
  useLowStockVariants,
  useAdjustStockMutation,
  type LowStockVariant,
} from '../api/adminInventoryApi';
import {
  useAdminProducts,
  adminCatalogApi,
} from '@/features/admin/catalog/api/adminCatalogApi';
import {
  AdjustStockDialog,
  type TargetVariantForAdjustment,
} from '../components/AdjustStockDialog';

export const AdminStockPage: React.FC = () => {
  const [threshold, setThreshold] = React.useState<number>(5);
  const [searchQuery, setSearchQuery] = React.useState<string>('');
  const [selectedVariant, setSelectedVariant] = React.useState<TargetVariantForAdjustment | null>(null);
  const [isDialogOpen, setIsDialogOpen] = React.useState<boolean>(false);

  // Queries
  const {
    data: lowStockVariants = [],
    isLoading: isLowStockLoading,
    refetch: refetchLowStock,
  } = useLowStockVariants(threshold);

  const {
    data: productsData,
    isLoading: isProductsLoading,
  } = useAdminProducts({ pageSize: 50 });

  // Detailed products for variants lookup cache
  const [expandedProductMap, setExpandedProductMap] = React.useState<Record<string, Product>>({});
  const [loadingProductId, setLoadingProductId] = React.useState<string | null>(null);

  const adjustStockMutation = useAdjustStockMutation();

  const handleAdjustStock = async (changeQuantity: number, note: string) => {
    if (!selectedVariant) return;

    try {
      await adjustStockMutation.mutateAsync({
        variantId: selectedVariant.variantId,
        input: { changeQuantity, note },
      });

      toast.success(
        `Stok varian ${selectedVariant.sku} berhasil disesuaikan (${changeQuantity > 0 ? '+' : ''}${changeQuantity}).`
      );

      // Also refresh detailed product if expanded
      const productId = Object.keys(expandedProductMap).find((id) =>
        expandedProductMap[id]?.variants?.some((v: ProductVariant) => v.id === selectedVariant.variantId)
      );
      if (productId) {
        const updated = await adminCatalogApi.getProduct(productId);
        setExpandedProductMap((prev) => ({ ...prev, [productId]: updated }));
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal menyesuaikan stok.';
      toast.error(msg);
      throw err;
    }
  };

  const handleOpenAdjustment = (target: TargetVariantForAdjustment) => {
    setSelectedVariant(target);
    setIsDialogOpen(true);
  };

  const toggleProductExpand = async (product: Product) => {
    if (expandedProductMap[product.id]) {
      setExpandedProductMap((prev) => {
        const next = { ...prev };
        delete next[product.id];
        return next;
      });
      return;
    }

    setLoadingProductId(product.id);
    try {
      const detail = await adminCatalogApi.getProduct(product.id);
      setExpandedProductMap((prev) => ({ ...prev, [product.id]: detail }));
    } catch {
      toast.error('Terjadi kesalahan saat memuat varian produk.');
    } finally {
      setLoadingProductId(null);
    }
  };

  const filteredProducts = (productsData?.items || []).filter((prod: Product) => {
    if (!searchQuery.trim()) return true;
    const q = searchQuery.toLowerCase();
    return prod.name.toLowerCase().includes(q) || prod.brandName?.toLowerCase().includes(q);
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Stok & Penyesuaian</h1>
          <p className="text-sm text-muted-foreground">
            Kelola stok produk, pantau barang yang menipis, dan lakukan penyesuaian kuantitas secara manual.
          </p>
        </div>
      </div>

      {/* Section 1: Stok Menipis */}
      <Card className="border-amber-500/20 shadow-sm">
        <CardHeader className="pb-3">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <div className="flex items-center gap-2">
              <div className="rounded-lg bg-amber-500/10 p-2 text-amber-600 dark:text-amber-400">
                <AlertTriangle className="h-5 w-5" />
              </div>
              <div>
                <CardTitle className="text-lg">Peringatan Stok Menipis</CardTitle>
                <CardDescription>
                  Daftar varian dengan kuantitas stok di bawah atau sama dengan ambang batas.
                </CardDescription>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <Filter className="h-3.5 w-3.5" />
                <span>Batas:</span>
              </div>
              <div className="flex items-center rounded-md border bg-background p-0.5 text-xs font-medium">
                {[5, 10, 20].map((val) => (
                  <button
                    key={val}
                    type="button"
                    onClick={() => setThreshold(val)}
                    className={`rounded px-2.5 py-1 transition-colors ${
                      threshold === val
                        ? 'bg-amber-500 text-white font-bold'
                        : 'text-muted-foreground hover:bg-muted'
                    }`}
                  >
                    ≤ {val}
                  </button>
                ))}
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => refetchLowStock()}
                className="h-8 gap-1 text-xs"
              >
                <RefreshCw className="h-3.5 w-3.5" />
                Segarkan
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLowStockLoading ? (
            <div className="space-y-3 py-2">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          ) : lowStockVariants.length === 0 ? (
            <div className="rounded-lg border border-dashed py-8 text-center text-sm text-muted-foreground">
              Tidak ada produk dengan stok menipis (≤ {threshold} unit). Semua stok aman!
            </div>
          ) : (
            <div className="overflow-x-auto rounded-lg border">
              <table className="w-full text-left text-sm">
                <thead className="border-b bg-muted/50 text-xs text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-medium">Produk & Varian</th>
                    <th className="px-4 py-3 font-medium">SKU</th>
                    <th className="px-4 py-3 font-medium">Harga</th>
                    <th className="px-4 py-3 font-medium text-center">Sisa Stok</th>
                    <th className="px-4 py-3 font-medium text-right">Aksi</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {lowStockVariants.map((item: LowStockVariant) => (
                    <tr key={item.variantId} className="hover:bg-muted/30 transition-colors">
                      <td className="px-4 py-3">
                        <div className="font-semibold text-foreground">{item.productName}</div>
                        <div className="text-xs text-muted-foreground">
                          {[item.optionSize && `Ukuran: ${item.optionSize}`, item.optionColor && `Warna: ${item.optionColor}`]
                            .filter(Boolean)
                            .join(' • ') || 'Standard'}
                        </div>
                      </td>
                      <td className="px-4 py-3 font-mono text-xs">{item.sku}</td>
                      <td className="px-4 py-3">
                        <Price price={item.price} className="text-sm font-medium" />
                      </td>
                      <td className="px-4 py-3 text-center">
                        <Badge
                          variant={item.stockQuantity === 0 ? 'destructive' : 'secondary'}
                          className={
                            item.stockQuantity === 0
                              ? ''
                              : 'bg-amber-500/15 text-amber-700 dark:text-amber-400 border-amber-500/30 font-bold'
                          }
                        >
                          {item.stockQuantity === 0 ? 'Habis (0)' : `${item.stockQuantity} unit`}
                        </Badge>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() =>
                            handleOpenAdjustment({
                              variantId: item.variantId,
                              productName: item.productName,
                              sku: item.sku,
                              optionSize: item.optionSize,
                              optionColor: item.optionColor,
                              currentStock: item.stockQuantity,
                            })
                          }
                          className="h-8 gap-1.5 text-xs font-semibold"
                        >
                          <ArrowUpDown className="h-3.5 w-3.5" />
                          Sesuaikan Stok
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Section 2: Penyesuaian Stok Seluruh Katalog */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <div>
              <CardTitle className="text-lg flex items-center gap-2">
                <Boxes className="h-5 w-5 text-primary" />
                Katalog Produk & Varian
              </CardTitle>
              <CardDescription>
                Cari produk dan klik untuk melihat varian serta memperbarui stok masing-masing SKU.
              </CardDescription>
            </div>

            <div className="relative w-full sm:w-72">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Cari produk atau SKU..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 h-9 text-sm"
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isProductsLoading ? (
            <div className="space-y-2 py-4">
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
            </div>
          ) : filteredProducts.length === 0 ? (
            <div className="rounded-lg border border-dashed py-8 text-center text-sm text-muted-foreground">
              Tidak ada produk ditemukan.
            </div>
          ) : (
            <div className="space-y-3">
              {filteredProducts.map((prod: Product) => {
                const isExpanded = !!expandedProductMap[prod.id];
                const detail = expandedProductMap[prod.id];
                const isLoadingDetail = loadingProductId === prod.id;

                return (
                  <div key={prod.id} className="rounded-lg border bg-card overflow-hidden">
                    <div
                      onClick={() => toggleProductExpand(prod)}
                      className="flex items-center justify-between p-4 cursor-pointer hover:bg-muted/30 transition-colors"
                    >
                      <div className="flex items-center gap-3">
                        <div className="h-10 w-10 shrink-0 overflow-hidden rounded border bg-muted flex items-center justify-center">
                          {prod.primaryImageUrl ? (
                            <img
                              src={prod.primaryImageUrl}
                              alt={prod.name}
                              className="h-full w-full object-cover"
                            />
                          ) : (
                            <Boxes className="h-5 w-5 text-muted-foreground" />
                          )}
                        </div>
                        <div>
                          <div className="font-semibold text-sm text-foreground">{prod.name}</div>
                          <div className="text-xs text-muted-foreground">
                            Kategori: {prod.categoryName || '-'} • Brand: {prod.brandName || '-'}
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center gap-4">
                        <div className="text-right text-xs">
                          <span className="text-muted-foreground">Rentang Harga: </span>
                          <span className="font-medium text-foreground">
                            {prod.minPrice === prod.maxPrice ? (
                              <Price price={prod.minPrice} className="inline text-xs" />
                            ) : (
                              <span>
                                <Price price={prod.minPrice} className="inline text-xs" /> -{' '}
                                <Price price={prod.maxPrice} className="inline text-xs" />
                              </span>
                            )}
                          </span>
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-xs font-semibold"
                          disabled={isLoadingDetail}
                        >
                          {isLoadingDetail ? 'Memuat...' : isExpanded ? 'Tutup Varian ▲' : 'Lihat Varian ▼'}
                        </Button>
                      </div>
                    </div>

                    {/* Expanded Variants Table */}
                    {isExpanded && detail?.variants && (
                      <div className="border-t bg-muted/20 p-4">
                        <div className="text-xs font-semibold text-muted-foreground mb-2">
                          Daftar Varian ({detail.variants.length}):
                        </div>
                        <div className="overflow-x-auto rounded border bg-background">
                          <table className="w-full text-left text-xs">
                            <thead className="border-b bg-muted/40 text-muted-foreground">
                              <tr>
                                <th className="px-3 py-2 font-medium">SKU</th>
                                <th className="px-3 py-2 font-medium">Ukuran</th>
                                <th className="px-3 py-2 font-medium">Warna</th>
                                <th className="px-3 py-2 font-medium">Harga</th>
                                <th className="px-3 py-2 font-medium text-center">Stok</th>
                                <th className="px-3 py-2 font-medium text-right">Aksi</th>
                              </tr>
                            </thead>
                            <tbody className="divide-y">
                              {detail.variants.map((variant: ProductVariant) => (
                                <tr key={variant.id} className="hover:bg-muted/30">
                                  <td className="px-3 py-2 font-mono font-medium">{variant.sku}</td>
                                  <td className="px-3 py-2">{variant.optionSize || '-'}</td>
                                  <td className="px-3 py-2">{variant.optionColor || '-'}</td>
                                  <td className="px-3 py-2">
                                    <Price price={variant.price} className="text-xs font-medium" />
                                  </td>
                                  <td className="px-3 py-2 text-center">
                                    <Badge
                                      variant={variant.stockQuantity <= 5 ? 'destructive' : 'secondary'}
                                      className="font-mono text-xs"
                                    >
                                      {variant.stockQuantity}
                                    </Badge>
                                  </td>
                                  <td className="px-3 py-2 text-right">
                                    <Button
                                      size="sm"
                                      variant="outline"
                                      onClick={() =>
                                        handleOpenAdjustment({
                                          variantId: variant.id,
                                          productName: prod.name,
                                          sku: variant.sku,
                                          optionSize: variant.optionSize,
                                          optionColor: variant.optionColor,
                                          currentStock: variant.stockQuantity,
                                        })
                                      }
                                      className="h-7 text-xs font-medium gap-1"
                                    >
                                      <ArrowUpDown className="h-3 w-3" />
                                      Ubah Stok
                                    </Button>
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Adjust Stock Dialog */}
      <AdjustStockDialog
        isOpen={isDialogOpen}
        onClose={() => {
          setIsDialogOpen(false);
          setSelectedVariant(null);
        }}
        variant={selectedVariant}
        onConfirm={handleAdjustStock}
        isSubmitting={adjustStockMutation.isPending}
      />
    </div>
  );
};
export default AdminStockPage;
