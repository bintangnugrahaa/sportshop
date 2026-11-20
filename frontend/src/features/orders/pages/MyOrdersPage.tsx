import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Package, ArrowRight, CreditCard, XCircle } from 'lucide-react';
import { useOrders, useCancelOrder } from '../api/ordersApi';
import { OrderStatus } from '@/types';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { Price } from '@/components/shared/Price';
import { Button } from '@/components/ui/button';
import { CancelOrderDialog } from '../components/CancelOrderDialog';
import { toast } from '@/lib/toast';

const STATUS_TABS: { label: string; value?: OrderStatus }[] = [
  { label: 'Semua Status' },
  { label: 'Menunggu Pembayaran', value: 'PendingPayment' },
  { label: 'Dibayar', value: 'Paid' },
  { label: 'Diproses', value: 'Processing' },
  { label: 'Dikirim', value: 'Shipped' },
  { label: 'Selesai', value: 'Completed' },
  { label: 'Dibatalkan', value: 'Cancelled' },
];

export const MyOrdersPage: React.FC = () => {
  const navigate = useNavigate();
  const [selectedStatus, setSelectedStatus] = React.useState<OrderStatus | undefined>(undefined);
  const [page, setPage] = React.useState(1);
  const [cancellingOrderNumber, setCancellingOrderNumber] = React.useState<string | null>(null);

  const { data, isLoading } = useOrders({
    status: selectedStatus,
    page,
    pageSize: 10,
  });

  const cancelOrderMutation = useCancelOrder();

  const handleConfirmCancel = async (reason?: string) => {
    if (!cancellingOrderNumber) return;
    try {
      await cancelOrderMutation.mutateAsync({
        orderNumber: cancellingOrderNumber,
        reason,
      });
      toast.success(`Pesanan ${cancellingOrderNumber} berhasil dibatalkan.`);
      setCancellingOrderNumber(null);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal membatalkan pesanan.';
      toast.error(msg);
    }
  };

  return (
    <div className="container mx-auto px-4 py-8 sm:px-6 lg:px-8">
      {/* Title */}
      <div className="mb-6">
        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground">
          Pesanan Saya
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          Pantau status pesanan perlengkapan olahraga Anda dan riwayat pengiriman.
        </p>
      </div>

      {/* Status Filter Tabs */}
      <div className="flex gap-2 overflow-x-auto pb-2 mb-6 border-b text-xs sm:text-sm">
        {STATUS_TABS.map((tab) => {
          const isActive = selectedStatus === tab.value;
          return (
            <button
              key={tab.label}
              type="button"
              onClick={() => {
                setSelectedStatus(tab.value);
                setPage(1);
              }}
              className={`whitespace-nowrap px-3.5 py-2 font-medium rounded-md transition-colors ${
                isActive
                  ? 'bg-primary text-white shadow-sm'
                  : 'bg-muted/50 text-muted-foreground hover:bg-muted hover:text-foreground'
              }`}
            >
              {tab.label}
            </button>
          );
        })}
      </div>

      {/* Orders List */}
      {isLoading ? (
        <div className="flex justify-center py-16">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center max-w-md mx-auto">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-muted text-muted-foreground mb-4">
            <Package className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-semibold text-foreground">Tidak Ada Pesanan</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            {selectedStatus
              ? 'Tidak ditemukan pesanan dengan status ini.'
              : 'Anda belum pernah melakukan pemesanan. Yuk mulai belanja sekarang!'}
          </p>
          <Button className="mt-6" onClick={() => navigate('/produk')}>
            Jelajahi Produk
          </Button>
        </div>
      ) : (
        <div className="space-y-4">
          {data.items.map((order) => (
            <div
              key={order.id}
              className="rounded-xl border bg-card p-5 transition-shadow hover:shadow-md"
              data-testid={`order-card-${order.orderNumber}`}
            >
              {/* Order Header */}
              <div className="flex flex-wrap items-center justify-between gap-3 border-b pb-3 text-xs sm:text-sm">
                <div className="flex items-center gap-3">
                  <span className="font-mono font-bold text-foreground">{order.orderNumber}</span>
                  <span className="text-muted-foreground">•</span>
                  <span className="text-muted-foreground">
                    {new Date(order.createdAt).toLocaleDateString('id-ID', {
                      day: 'numeric',
                      month: 'short',
                      year: 'numeric',
                    })}
                  </span>
                </div>
                <StatusBadge status={order.status} />
              </div>

              {/* Order Items Preview */}
              <div className="py-4 space-y-3">
                {order.items.slice(0, 3).map((item) => (
                  <div key={item.id} className="flex items-center justify-between gap-4 text-xs sm:text-sm">
                    <div className="flex items-center gap-3">
                      {item.imageUrl ? (
                        <img
                          src={item.imageUrl}
                          alt={item.productName}
                          className="h-12 w-12 rounded object-cover border bg-muted"
                        />
                      ) : (
                        <div className="h-12 w-12 rounded border bg-muted flex items-center justify-center text-[10px] text-muted-foreground">
                          Item
                        </div>
                      )}
                      <div>
                        <p className="font-medium text-foreground line-clamp-1">{item.productName}</p>
                        <p className="text-xs text-muted-foreground">
                          {item.variantLabel || item.sku} × {item.quantity}
                        </p>
                      </div>
                    </div>
                    <Price price={item.lineTotal} className="font-semibold" />
                  </div>
                ))}
                {order.items.length > 3 && (
                  <p className="text-xs text-muted-foreground pt-1">
                    +{order.items.length - 3} produk lainnya...
                  </p>
                )}
              </div>

              {/* Order Footer & Actions */}
              <div className="flex flex-wrap items-center justify-between gap-4 border-t pt-3">
                <div className="text-xs sm:text-sm">
                  <span className="text-muted-foreground block text-xs">Total Pesanan</span>
                  <Price price={order.total} className="text-base sm:text-lg font-bold text-primary" />
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  {order.status === 'PendingPayment' && (
                    <>
                      <Button
                        size="sm"
                        variant="destructive"
                        className="text-xs h-8"
                        onClick={() => setCancellingOrderNumber(order.orderNumber)}
                      >
                        <XCircle className="mr-1 h-3.5 w-3.5" />
                        Batalkan
                      </Button>

                      <Button
                        size="sm"
                        className="text-xs h-8 bg-amber-600 hover:bg-amber-700"
                        onClick={() => navigate(`/checkout/status/${order.orderNumber}`)}
                      >
                        <CreditCard className="mr-1 h-3.5 w-3.5" />
                        Bayar Sekarang
                      </Button>
                    </>
                  )}

                  <Link
                    to={`/akun/pesanan/${order.orderNumber}`}
                    className="inline-flex items-center justify-center rounded-md border border-input bg-background px-3 h-8 text-xs font-medium hover:bg-accent hover:text-accent-foreground transition-colors"
                  >
                    <span>Lihat Detail</span>
                    <ArrowRight className="ml-1 h-3.5 w-3.5" />
                  </Link>
                </div>
              </div>
            </div>
          ))}

          {/* Pagination */}
          {data.totalPages > 1 && (
            <div className="flex items-center justify-center gap-2 pt-6">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setPage((p) => Math.max(p - 1, 1))}
                disabled={!data.hasPreviousPage}
              >
                Sebelumnya
              </Button>
              <span className="text-xs text-muted-foreground px-2">
                Halaman {data.page} dari {data.totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setPage((p) => Math.min(p + 1, data.totalPages))}
                disabled={!data.hasNextPage}
              >
                Selanjutnya
              </Button>
            </div>
          )}
        </div>
      )}

      {/* Cancel Order Dialog */}
      <CancelOrderDialog
        isOpen={Boolean(cancellingOrderNumber)}
        onClose={() => setCancellingOrderNumber(null)}
        onConfirm={handleConfirmCancel}
        orderNumber={cancellingOrderNumber ?? ''}
        isSubmitting={cancelOrderMutation.isPending}
      />
    </div>
  );
};

export default MyOrdersPage;
