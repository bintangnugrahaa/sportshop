import React from 'react';
import { OrderStatus } from '@/types';
import { cn } from '@/lib/utils';

export interface StatusBadgeProps {
  status: OrderStatus | string;
  className?: string;
}

const statusConfig: Record<string, { label: string; className: string }> = {
  PendingPayment: {
    label: 'Menunggu Pembayaran',
    className: 'bg-amber-100 text-amber-800 border-amber-300 dark:bg-amber-900/40 dark:text-amber-200',
  },
  Paid: {
    label: 'Dibayar',
    className: 'bg-blue-100 text-blue-800 border-blue-300 dark:bg-blue-900/40 dark:text-blue-200',
  },
  Processing: {
    label: 'Diproses',
    className: 'bg-indigo-100 text-indigo-800 border-indigo-300 dark:bg-indigo-900/40 dark:text-indigo-200',
  },
  Shipped: {
    label: 'Dikirim',
    className: 'bg-cyan-100 text-cyan-800 border-cyan-300 dark:bg-cyan-900/40 dark:text-cyan-200',
  },
  Completed: {
    label: 'Selesai',
    className: 'bg-emerald-100 text-emerald-800 border-emerald-300 dark:bg-emerald-900/40 dark:text-emerald-200',
  },
  Cancelled: {
    label: 'Dibatalkan',
    className: 'bg-slate-100 text-slate-800 border-slate-300 dark:bg-slate-800 dark:text-slate-200',
  },
  Expired: {
    label: 'Kedaluwarsa',
    className: 'bg-zinc-200 text-zinc-900 border-zinc-400 dark:bg-zinc-800 dark:text-zinc-300',
  },
};

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status, className }) => {
  const config = statusConfig[status] ?? {
    label: status,
    className: 'bg-muted text-muted-foreground border-border',
  };

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold',
        config.className,
        className,
      )}
    >
      {config.label}
    </span>
  );
};
