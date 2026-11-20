import React from 'react';
import { useCategories, useBrands } from '../api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { RotateCcw } from 'lucide-react';

export interface FilterState {
  categorySlug?: string;
  brandSlug?: string;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
}

export interface ProductFiltersProps {
  filters: FilterState;
  onFilterChange: (newFilters: Partial<FilterState>) => void;
  onReset: () => void;
  onCloseMobile?: () => void;
}

export const ProductFilters: React.FC<ProductFiltersProps> = ({
  filters,
  onFilterChange,
  onReset,
  onCloseMobile,
}) => {
  const { data: categories = [], isLoading: isCategoriesLoading } = useCategories();
  const { data: brands = [], isLoading: isBrandsLoading } = useBrands();

  const [minPriceInput, setMinPriceInput] = React.useState<string>(
    filters.minPrice !== undefined ? filters.minPrice.toString() : '',
  );
  const [maxPriceInput, setMaxPriceInput] = React.useState<string>(
    filters.maxPrice !== undefined ? filters.maxPrice.toString() : '',
  );

  React.useEffect(() => {
    setMinPriceInput(filters.minPrice !== undefined ? filters.minPrice.toString() : '');
    setMaxPriceInput(filters.maxPrice !== undefined ? filters.maxPrice.toString() : '');
  }, [filters.minPrice, filters.maxPrice]);

  const handleApplyPrice = (e: React.FormEvent) => {
    e.preventDefault();
    const min = minPriceInput ? parseInt(minPriceInput, 10) : undefined;
    const max = maxPriceInput ? parseInt(maxPriceInput, 10) : undefined;
    onFilterChange({
      minPrice: isNaN(min as number) ? undefined : min,
      maxPrice: isNaN(max as number) ? undefined : max,
    });
    if (onCloseMobile) onCloseMobile();
  };

  const hasActiveFilters =
    !!filters.categorySlug ||
    !!filters.brandSlug ||
    filters.minPrice !== undefined ||
    filters.maxPrice !== undefined ||
    filters.inStock !== undefined;

  return (
    <div className="space-y-6">
      {/* Header and Reset button */}
      <div className="flex items-center justify-between pb-3 border-b">
        <h3 className="font-semibold text-base text-foreground">Filter Produk</h3>
        {hasActiveFilters && (
          <button
            onClick={() => {
              setMinPriceInput('');
              setMaxPriceInput('');
              onReset();
            }}
            className="flex items-center gap-1 text-xs text-muted-foreground hover:text-primary transition-colors"
          >
            <RotateCcw className="h-3 w-3" />
            Reset
          </button>
        )}
      </div>

      {/* Categories */}
      <div className="space-y-3">
        <Label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Kategori
        </Label>
        {isCategoriesLoading ? (
          <div className="space-y-1.5 text-xs text-muted-foreground">Memuat kategori...</div>
        ) : (
          <div className="space-y-1 text-sm max-h-52 overflow-y-auto pr-1">
            <button
              onClick={() => onFilterChange({ categorySlug: undefined })}
              className={`block w-full text-left px-2 py-1.5 rounded transition-colors ${
                !filters.categorySlug
                  ? 'bg-primary/10 text-primary font-medium'
                  : 'text-foreground/80 hover:bg-muted'
              }`}
            >
              Semua Kategori
            </button>
            {categories.map((cat) => (
              <div key={cat.id} className="space-y-1">
                <button
                  onClick={() => onFilterChange({ categorySlug: cat.slug })}
                  className={`block w-full text-left px-2 py-1.5 rounded transition-colors ${
                    filters.categorySlug === cat.slug
                      ? 'bg-primary/10 text-primary font-medium'
                      : 'text-foreground/80 hover:bg-muted'
                  }`}
                >
                  {cat.name}
                </button>
                {cat.subCategories && cat.subCategories.length > 0 && (
                  <div className="pl-4 space-y-1">
                    {cat.subCategories.map((sub) => (
                      <button
                        key={sub.id}
                        onClick={() => onFilterChange({ categorySlug: sub.slug })}
                        className={`block w-full text-left px-2 py-1 rounded text-xs transition-colors ${
                          filters.categorySlug === sub.slug
                            ? 'bg-primary/10 text-primary font-medium'
                            : 'text-muted-foreground hover:bg-muted'
                        }`}
                      >
                        {sub.name}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Brands */}
      <div className="space-y-3 pt-3 border-t">
        <Label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Brand
        </Label>
        {isBrandsLoading ? (
          <div className="space-y-1.5 text-xs text-muted-foreground">Memuat brand...</div>
        ) : (
          <div className="space-y-1 text-sm max-h-48 overflow-y-auto pr-1">
            <button
              onClick={() => onFilterChange({ brandSlug: undefined })}
              className={`block w-full text-left px-2 py-1.5 rounded transition-colors ${
                !filters.brandSlug
                  ? 'bg-primary/10 text-primary font-medium'
                  : 'text-foreground/80 hover:bg-muted'
              }`}
            >
              Semua Brand
            </button>
            {brands.map((brand) => (
              <button
                key={brand.id}
                onClick={() => onFilterChange({ brandSlug: brand.slug })}
                className={`block w-full text-left px-2 py-1.5 rounded transition-colors ${
                  filters.brandSlug === brand.slug
                    ? 'bg-primary/10 text-primary font-medium'
                    : 'text-foreground/80 hover:bg-muted'
                }`}
              >
                {brand.name}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Price Range */}
      <div className="space-y-3 pt-3 border-t">
        <Label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Rentang Harga (Rp)
        </Label>
        <form onSubmit={handleApplyPrice} className="space-y-2">
          <div className="grid grid-cols-2 gap-2">
            <div>
              <Input
                type="number"
                placeholder="Min"
                className="h-8 text-xs"
                value={minPriceInput}
                onChange={(e) => setMinPriceInput(e.target.value)}
              />
            </div>
            <div>
              <Input
                type="number"
                placeholder="Maks"
                className="h-8 text-xs"
                value={maxPriceInput}
                onChange={(e) => setMaxPriceInput(e.target.value)}
              />
            </div>
          </div>
          <Button type="submit" variant="outline" size="sm" className="w-full text-xs h-8">
            Terapkan
          </Button>
        </form>
      </div>

      {/* Availability / InStock */}
      <div className="space-y-3 pt-3 border-t">
        <label className="flex items-center gap-2 text-sm cursor-pointer select-none">
          <input
            type="checkbox"
            checked={filters.inStock === true}
            onChange={(e) =>
              onFilterChange({ inStock: e.target.checked ? true : undefined })
            }
            className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
          />
          <span className="font-medium text-foreground">Hanya produk tersedia</span>
        </label>
      </div>
    </div>
  );
};
