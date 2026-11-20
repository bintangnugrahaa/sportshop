import React from 'react';
import { CheckCircle2, Clock, Truck, Package, XCircle, AlertCircle } from 'lucide-react';
import { OrderStatusHistory } from '@/types';

interface OrderTimelineProps {
  histories: OrderStatusHistory[];
}

const statusConfig: Record<
  string,
  { label: string; icon: React.ComponentType<{ className?: string }>; color: string }
> = {
  PendingPayment: {
    label: 'Pesanan Dibuat',
    icon: Clock,
    color: 'bg-amber-500 text-white',
  },
  Paid: {
    label: 'Pembayaran Diterima',
    icon: CheckCircle2,
    color: 'bg-emerald-500 text-white',
  },
  Processing: {
    label: 'Pesanan Diproses',
    icon: Package,
    color: 'bg-indigo-500 text-white',
  },
  Shipped: {
    label: 'Pesanan Dikirim',
    icon: Truck,
    color: 'bg-cyan-500 text-white',
  },
  Completed: {
    label: 'Pesanan Selesai',
    icon: CheckCircle2,
    color: 'bg-emerald-600 text-white',
  },
  Cancelled: {
    label: 'Pesanan Dibatalkan',
    icon: XCircle,
    color: 'bg-rose-500 text-white',
  },
  Expired: {
    label: 'Pesanan Kedaluwarsa',
    icon: AlertCircle,
    color: 'bg-slate-500 text-white',
  },
};

export const OrderTimeline: React.FC<OrderTimelineProps> = ({ histories }) => {
  if (!histories || histories.length === 0) {
    return (
      <div className="py-4 text-center text-xs text-muted-foreground">
        Belum ada riwayat status.
      </div>
    );
  }

  // Sort histories ascending by createdAt
  const sorted = [...histories].sort(
    (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
  );

  return (
    <div className="relative pl-6 space-y-6 before:absolute before:left-3 before:top-2 before:bottom-2 before:w-0.5 before:bg-border">
      {sorted.map((item, index) => {
        const config = statusConfig[item.toStatus] || {
          label: item.toStatus,
          icon: Clock,
          color: 'bg-muted text-muted-foreground',
        };
        const Icon = config.icon;
        const isLatest = index === sorted.length - 1;

        return (
          <div key={item.id} className="relative group">
            {/* Timeline bullet icon */}
            <div
              className={`absolute -left-[27px] top-0.5 flex h-6 w-6 items-center justify-center rounded-full ring-4 ring-background ${
                config.color
              } ${isLatest ? 'scale-110 shadow-md' : 'opacity-85'}`}
            >
              <Icon className="h-3.5 w-3.5" />
            </div>

            {/* Content */}
            <div className="text-xs sm:text-sm">
              <div className="flex flex-wrap items-baseline justify-between gap-1">
                <span className="font-semibold text-foreground">{config.label}</span>
                <span className="text-[11px] text-muted-foreground">
                  {new Date(item.createdAt).toLocaleString('id-ID', {
                    dateStyle: 'medium',
                    timeStyle: 'short',
                  })}
                </span>
              </div>

              {item.note && (
                <p className="mt-1 text-xs text-muted-foreground bg-muted/40 p-2 rounded border">
                  {item.note}
                </p>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
};
