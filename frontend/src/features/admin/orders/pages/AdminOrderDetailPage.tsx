import React from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  ArrowLeft,
  Package,
  Truck,
  MapPin,
  Clock,
  CheckCircle2,
  XCircle,
  AlertCircle,
  PlayCircle,
  Loader2,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { Price } from '@/components/shared/Price';
import { toast } from '@/lib/toast';
import {
  useAdminOrderDetail,
  useAdminProcessOrderMutation,
  useAdminShipOrderMutation,
  useAdminCompleteOrderMutation,
  useAdminCancelOrderMutation,
} from '../api/adminOrdersApi';
import { ShipOrderDialog } from '../components/ShipOrderDialog';
import { AdminCancelOrderDialog } from '../components/AdminCancelOrderDialog';

export const AdminOrderDetailPage: React.FC = () => {
  const { orderNumber = '' } = useParams<{ orderNumber: string }>();

  const { data: order, isLoading, error } = useAdminOrderDetail(orderNumber);

  const processOrderMutation = useAdminProcessOrderMutation();
  const shipOrderMutation = useAdminShipOrderMutation();
  const completeOrderMutation = useAdminCompleteOrderMutation();
  const cancelOrderMutation = useAdminCancelOrderMutation();

  const [isShipModalOpen, setIsShipModalOpen] = React.useState(false);
  const [isCancelModalOpen, setIsCancelModalOpen] = React.useState(false);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-8 w-24" />
          <Skeleton className="h-8 w-48" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="md:col-span-2 space-y-6">
            <Skeleton className="h-64 w-full" />
            <Skeleton className="h-48 w-full" />
          </div>
          <div className="space-y-6">
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-48 w-full" />
          </div>
        </div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="rounded-xl border border-destructive/20 bg-destructive/5 p-8 text-center space-y-4">
        <AlertCircle className="mx-auto h-12 w-12 text-destructive" />
        <h2 className="text-xl font-bold text-destructive">Pesanan Tidak Ditemukan</h2>
        <p className="text-sm text-muted-foreground">
          Pesanan dengan nomor {orderNumber} tidak ditemukan atau gagal dimuat.
        </p>
        <Link
          to="/admin/pesanan"
          className="inline-flex items-center justify-center rounded-md border border-input bg-background h-9 px-3 text-xs font-medium hover:bg-accent hover:text-accent-foreground"
        >
          <ArrowLeft className="mr-2 h-4 w-4" />
          Kembali ke Daftar Pesanan
        </Link>
      </div>
    );
  }

  const handleProcessOrder = async () => {
    try {
      await processOrderMutation.mutateAsync({ orderNumber: order.orderNumber });
      toast.success(`Pesanan ${order.orderNumber} sedang diproses.`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal memproses pesanan.';
      toast.error(msg);
    }
  };

  const handleShipOrder = async (courier: string, trackingNumber: string, note?: string) => {
    try {
      await shipOrderMutation.mutateAsync({
        orderNumber: order.orderNumber,
        input: { courier, trackingNumber, note },
      });
      toast.success(`Pesanan ${order.orderNumber} berhasil dikirim dengan kurir ${courier}!`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal mengirim pesanan.';
      toast.error(msg);
      throw err;
    }
  };

  const handleCompleteOrder = async () => {
    try {
      await completeOrderMutation.mutateAsync({ orderNumber: order.orderNumber });
      toast.success(`Pesanan ${order.orderNumber} telah selesai.`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal menyelesaikan pesanan.';
      toast.error(msg);
    }
  };

  const handleCancelOrder = async (reason?: string) => {
    try {
      await cancelOrderMutation.mutateAsync({
        orderNumber: order.orderNumber,
        input: { reason },
      });
      toast.success(`Pesanan ${order.orderNumber} berhasil dibatalkan.`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal membatalkan pesanan.';
      toast.error(msg);
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Bar */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <Link
            to="/admin/pesanan"
            className="inline-flex items-center justify-center rounded-md h-8 w-8 text-muted-foreground hover:bg-muted hover:text-foreground"
            aria-label="Kembali ke Daftar Pesanan"
          >
            <ArrowLeft className="h-4 w-4" />
          </Link>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-bold tracking-tight text-foreground font-mono">
                {order.orderNumber}
              </h1>
              <StatusBadge status={order.status} />
            </div>
            <p className="text-xs text-muted-foreground mt-0.5">
              Dibuat pada{' '}
              {new Date(order.createdAt).toLocaleDateString('id-ID', {
                weekday: 'long',
                day: 'numeric',
                month: 'long',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
              })}
            </p>
          </div>
        </div>

        {/* Action Controls - Strictly Based on Status */}
        <div className="flex flex-wrap items-center gap-2">
          {order.status === 'Paid' && (
            <>
              <Button
                variant="destructive"
                size="sm"
                onClick={() => setIsCancelModalOpen(true)}
                disabled={cancelOrderMutation.isPending}
                className="text-xs"
              >
                <XCircle className="mr-1.5 h-3.5 w-3.5" />
                Batalkan Pesanan
              </Button>
              <Button
                size="sm"
                onClick={handleProcessOrder}
                disabled={processOrderMutation.isPending}
                className="text-xs font-semibold"
              >
                {processOrderMutation.isPending ? (
                  <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
                ) : (
                  <PlayCircle className="mr-1.5 h-3.5 w-3.5" />
                )}
                Proses Pesanan
              </Button>
            </>
          )}

          {order.status === 'Processing' && (
            <>
              <Button
                variant="destructive"
                size="sm"
                onClick={() => setIsCancelModalOpen(true)}
                disabled={cancelOrderMutation.isPending}
                className="text-xs"
              >
                <XCircle className="mr-1.5 h-3.5 w-3.5" />
                Batalkan Pesanan
              </Button>
              <Button
                size="sm"
                onClick={() => setIsShipModalOpen(true)}
                disabled={shipOrderMutation.isPending}
                className="text-xs font-semibold bg-cyan-600 hover:bg-cyan-700 text-white"
              >
                <Truck className="mr-1.5 h-3.5 w-3.5" />
                Kirim Pesanan (Input Resi)
              </Button>
            </>
          )}

          {order.status === 'Shipped' && (
            <Button
              size="sm"
              onClick={handleCompleteOrder}
              disabled={completeOrderMutation.isPending}
              className="text-xs font-semibold bg-emerald-600 hover:bg-emerald-700 text-white"
            >
              {completeOrderMutation.isPending ? (
                <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
              ) : (
                <CheckCircle2 className="mr-1.5 h-3.5 w-3.5" />
              )}
              Selesaikan Pesanan
            </Button>
          )}

          {order.status === 'PendingPayment' && (
            <Button
              variant="destructive"
              size="sm"
              onClick={() => setIsCancelModalOpen(true)}
              disabled={cancelOrderMutation.isPending}
              className="text-xs"
            >
              <XCircle className="mr-1.5 h-3.5 w-3.5" />
              Batalkan Pesanan
            </Button>
          )}
        </div>
      </div>

      {/* Main Content Layout */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Left Column: Items and Timeline */}
        <div className="md:col-span-2 space-y-6">
          {/* Order Items */}
          <Card>
            <CardHeader className="pb-3 border-b">
              <CardTitle className="text-base flex items-center gap-2">
                <Package className="h-4 w-4 text-primary" />
                Item yang Dipesan ({order.items.length})
              </CardTitle>
            </CardHeader>
            <CardContent className="divide-y p-0">
              {order.items.map((item) => (
                <div key={item.id} className="flex items-center gap-4 p-4">
                  <div className="h-16 w-16 shrink-0 overflow-hidden rounded border bg-muted flex items-center justify-center">
                    {item.imageUrl ? (
                      <img
                        src={item.imageUrl}
                        alt={item.productName}
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <Package className="h-6 w-6 text-muted-foreground" />
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-semibold text-sm text-foreground truncate">
                      {item.productName}
                    </p>
                    <div className="text-xs text-muted-foreground flex items-center gap-2 mt-0.5">
                      <span className="font-mono bg-muted px-1.5 py-0.2 rounded border">
                        {item.sku}
                      </span>
                      {item.variantLabel && <span>{item.variantLabel}</span>}
                    </div>
                    <div className="text-xs text-muted-foreground mt-1">
                      {item.quantity} × <Price price={item.unitPrice} className="inline text-xs" />
                    </div>
                  </div>
                  <div className="text-right">
                    <Price price={item.lineTotal} className="text-sm font-bold text-foreground" />
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>

          {/* Status History Timeline */}
          <Card>
            <CardHeader className="pb-3 border-b">
              <CardTitle className="text-base flex items-center gap-2">
                <Clock className="h-4 w-4 text-primary" />
                Riwayat & Jejak Status
              </CardTitle>
            </CardHeader>
            <CardContent className="p-4">
              {order.statusHistories && order.statusHistories.length > 0 ? (
                <div className="relative pl-6 space-y-4 before:absolute before:left-2 before:top-2 before:bottom-2 before:w-0.5 before:bg-border">
                  {order.statusHistories.map((history) => (
                    <div key={history.id} className="relative">
                      <div className="absolute -left-6 top-1.5 h-2.5 w-2.5 rounded-full border-2 border-background bg-primary" />
                      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1">
                        <div className="flex items-center gap-2">
                          <StatusBadge status={history.toStatus} />
                          {history.fromStatus && (
                            <span className="text-xs text-muted-foreground">
                              (dari {history.fromStatus})
                            </span>
                          )}
                        </div>
                        <span className="text-xs text-muted-foreground">
                          {new Date(history.createdAt).toLocaleDateString('id-ID', {
                            day: 'numeric',
                            month: 'short',
                            year: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit',
                          })}
                        </span>
                      </div>
                      {history.note && (
                        <p className="mt-1 text-xs text-muted-foreground bg-muted/30 rounded p-2 border">
                          {history.note}
                        </p>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-xs text-muted-foreground text-center py-2">
                  Belum ada riwayat perubahan status.
                </p>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Right Column: Customer Info & Cost Breakdown */}
        <div className="space-y-6">
          {/* Shipping Information */}
          <Card>
            <CardHeader className="pb-3 border-b">
              <CardTitle className="text-base flex items-center gap-2">
                <MapPin className="h-4 w-4 text-primary" />
                Pengiriman & Penerima
              </CardTitle>
            </CardHeader>
            <CardContent className="p-4 space-y-3 text-xs">
              <div>
                <span className="text-muted-foreground">Penerima:</span>
                <p className="font-semibold text-foreground text-sm">
                  {order.shippingAddress?.recipientName}
                </p>
                <p className="text-muted-foreground">{order.shippingAddress?.phone}</p>
              </div>

              <div className="border-t pt-2">
                <span className="text-muted-foreground">Alamat Lengkap:</span>
                <p className="text-foreground mt-0.5 leading-relaxed">
                  {order.shippingAddress?.street}
                </p>
                <p className="text-muted-foreground">
                  {order.shippingAddress?.city}, {order.shippingAddress?.provinceName} (
                  {order.shippingAddress?.postalCode})
                </p>
              </div>

              <div className="border-t pt-2 space-y-1">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Total Berat:</span>
                  <span className="font-medium text-foreground">{order.totalWeightGram} gram</span>
                </div>
                {order.courier && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Kurir:</span>
                    <span className="font-medium text-foreground">{order.courier}</span>
                  </div>
                )}
                {order.trackingNumber && (
                  <div className="flex justify-between items-center bg-cyan-50 dark:bg-cyan-950/40 p-2 rounded border border-cyan-200">
                    <span className="text-muted-foreground font-medium">Nomor Resi:</span>
                    <span className="font-mono font-bold text-cyan-800 dark:text-cyan-200">
                      {order.trackingNumber}
                    </span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Cost Breakdown */}
          <Card>
            <CardHeader className="pb-3 border-b">
              <CardTitle className="text-base">Ringkasan Pembayaran</CardTitle>
            </CardHeader>
            <CardContent className="p-4 space-y-2 text-xs">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Subtotal Produk</span>
                <Price price={order.subtotal} className="font-medium" />
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Biaya Pengiriman</span>
                <Price price={order.shippingFee} className="font-medium" />
              </div>
              <div className="flex justify-between border-t pt-2 text-sm font-bold">
                <span className="text-foreground">Total Pembayaran</span>
                <Price price={order.total} className="text-primary text-base font-bold" />
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Ship Order Dialog */}
      <ShipOrderDialog
        isOpen={isShipModalOpen}
        onClose={() => setIsShipModalOpen(false)}
        orderNumber={order.orderNumber}
        onConfirm={handleShipOrder}
        isSubmitting={shipOrderMutation.isPending}
      />

      {/* Admin Cancel Order Dialog */}
      <AdminCancelOrderDialog
        isOpen={isCancelModalOpen}
        onClose={() => setIsCancelModalOpen(false)}
        orderNumber={order.orderNumber}
        onConfirm={handleCancelOrder}
        isSubmitting={cancelOrderMutation.isPending}
      />
    </div>
  );
};
export default AdminOrderDetailPage;
