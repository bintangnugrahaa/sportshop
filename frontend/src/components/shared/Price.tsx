import React from 'react';
import { formatIdr } from '@/lib/format';
import { cn } from '@/lib/utils';

export interface PriceProps {
  price?: number | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  className?: string;
  originalPrice?: number | null;
}

export const Price: React.FC<PriceProps> = ({
  price,
  minPrice,
  maxPrice,
  className,
  originalPrice,
}) => {
  if (minPrice !== undefined && minPrice !== null && maxPrice !== undefined && maxPrice !== null && minPrice !== maxPrice) {
    return (
      <div className={cn('flex items-baseline gap-1 font-semibold text-primary', className)}>
        <span>{formatIdr(minPrice)}</span>
        <span className="text-muted-foreground text-xs font-normal">–</span>
        <span>{formatIdr(maxPrice)}</span>
      </div>
    );
  }

  const effectivePrice = price ?? minPrice ?? 0;

  return (
    <div className={cn('flex items-baseline gap-2 font-semibold text-primary', className)}>
      <span>{formatIdr(effectivePrice)}</span>
      {originalPrice && originalPrice > effectivePrice && (
        <span className="text-xs font-normal text-muted-foreground line-through">
          {formatIdr(originalPrice)}
        </span>
      )}
    </div>
  );
};
