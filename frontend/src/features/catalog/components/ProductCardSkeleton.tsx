import React from 'react';
import { Skeleton } from '@/components/ui/skeleton';

export const ProductCardSkeleton: React.FC = () => {
  return (
    <div className="flex flex-col rounded-lg border bg-card p-0 overflow-hidden shadow-sm">
      <Skeleton className="aspect-square w-full" />
      <div className="p-3 sm:p-4 space-y-2.5">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-3/4" />
        <div className="pt-2 border-t flex justify-between items-center">
          <Skeleton className="h-5 w-28" />
        </div>
      </div>
    </div>
  );
};
