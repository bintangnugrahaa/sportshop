import React from 'react';
import { Package } from 'lucide-react';
import { ProductImage } from '@/types';
import { cn } from '@/lib/utils';

export interface ProductGalleryProps {
  images?: ProductImage[];
  productName: string;
}

export const ProductGallery: React.FC<ProductGalleryProps> = ({ images = [], productName }) => {
  // Sort images: primary first, then by sortOrder
  const sortedImages = React.useMemo(() => {
    return [...images].sort((a, b) => {
      if (a.isPrimary) return -1;
      if (b.isPrimary) return 1;
      return a.sortOrder - b.sortOrder;
    });
  }, [images]);

  const [selectedIndex, setSelectedIndex] = React.useState(0);

  const activeImage = sortedImages[selectedIndex];

  return (
    <div className="flex flex-col gap-4">
      {/* Main Image Display */}
      <div className="relative aspect-square w-full overflow-hidden rounded-xl border bg-muted/20">
        {activeImage ? (
          <img
            src={activeImage.url}
            alt={activeImage.altText || productName}
            className="h-full w-full object-cover object-center transition-all duration-300"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-muted-foreground">
            <Package className="h-20 w-20 stroke-1 opacity-40" />
          </div>
        )}
      </div>

      {/* Thumbnails Row */}
      {sortedImages.length > 1 && (
        <div className="flex gap-3 overflow-x-auto pb-2 scrollbar-thin">
          {sortedImages.map((image, idx) => {
            const isSelected = idx === selectedIndex;
            return (
              <button
                key={image.id || idx}
                type="button"
                onClick={() => setSelectedIndex(idx)}
                className={cn(
                  'relative h-20 w-20 shrink-0 overflow-hidden rounded-lg border bg-muted/20 transition-all',
                  isSelected
                    ? 'ring-2 ring-primary ring-offset-2 border-primary'
                    : 'opacity-70 hover:opacity-100 hover:border-primary/50',
                )}
                aria-label={`Lihat gambar ke-${idx + 1}`}
              >
                <img
                  src={image.url}
                  alt={image.altText || `${productName} preview ${idx + 1}`}
                  className="h-full w-full object-cover object-center"
                />
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
};
