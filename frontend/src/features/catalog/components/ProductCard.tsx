import React from 'react';
import { Link } from 'react-router-dom';
import { Package } from 'lucide-react';
import { Product } from '@/types';
import { Price } from '@/components/shared/Price';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

export interface ProductCardProps {
  product: Product;
  className?: string;
}

export const ProductCard: React.FC<ProductCardProps> = ({ product, className }) => {
  const imageUrl =
    product.primaryImageUrl ||
    (product.images && product.images.length > 0 ? product.images[0]?.url : null);

  const isOutOfStock = !product.inStock || (product.totalStock !== null && product.totalStock === 0);

  return (
    <div
      className={cn(
        'group relative flex flex-col rounded-lg border bg-card transition-all duration-200 hover:shadow-md hover:border-primary/40',
        className,
      )}
    >
      {/* Image Container with 1:1 Aspect Ratio */}
      <Link
        to={`/produk/${product.slug}`}
        className="relative aspect-square w-full overflow-hidden rounded-t-lg bg-muted/30"
        aria-label={product.name}
      >
        {imageUrl ? (
          <img
            src={imageUrl}
            alt={product.name}
            loading="lazy"
            className="h-full w-full object-cover object-center transition-transform duration-300 group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-muted-foreground">
            <Package className="h-12 w-12 stroke-1 opacity-40" />
          </div>
        )}

        {/* Stock Badge */}
        {isOutOfStock ? (
          <Badge
            variant="destructive"
            className="absolute left-2.5 top-2.5 font-bold uppercase text-[10px]"
          >
            Habis
          </Badge>
        ) : product.totalStock !== null && product.totalStock !== undefined && product.totalStock <= 5 ? (
          <Badge
            variant="warning"
            className="absolute left-2.5 top-2.5 font-bold text-[10px]"
          >
            Sisa {product.totalStock}
          </Badge>
        ) : null}
      </Link>

      {/* Info Container */}
      <div className="flex flex-1 flex-col p-3 sm:p-4">
        {/* Category & Brand info */}
        <div className="mb-1.5 flex items-center gap-1.5 text-[11px] font-medium uppercase tracking-wider text-muted-foreground truncate">
          {product.brandName && <span>{product.brandName}</span>}
          {product.brandName && product.categoryName && <span>•</span>}
          {product.categoryName && <span>{product.categoryName}</span>}
        </div>

        {/* Product Name */}
        <Link
          to={`/produk/${product.slug}`}
          className="line-clamp-2 text-sm font-medium text-foreground hover:text-primary transition-colors mb-2"
          title={product.name}
        >
          {product.name}
        </Link>

        {/* Price & Status */}
        <div className="mt-auto pt-2 border-t border-border/50 flex items-center justify-between">
          <Price
            price={product.basePrice}
            minPrice={product.minPrice}
            maxPrice={product.maxPrice}
            className="text-sm sm:text-base"
          />
        </div>
      </div>
    </div>
  );
};
