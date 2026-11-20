import React from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import {
  Package,
  MapPin,
  Truck,
  CreditCard,
  ChevronRight,
  Printer,
  XCircle,
  AlertTriangle,
  Loader2,
} from 'lucide-react';
import { useOrderDetail, useCancelOrder } from '../api/ordersApi';
import { OrderTimeline } from '../components/OrderTimeline';
import { CancelOrderDialog } from '../components/CancelOrderDialog';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { Price } from '@/components/shared/Price';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/lib/toast';

export const OrderDetailPage: React.FC = () => {
  const { orderNumber } = useParams<{ orderNumber: string }>();
  const navigate = useNavigate();
  const [isCancelModalOpen, setIsCancelModalOpen] = React.useState(false);

  const { data: order, isLoading, error } = useOrderDetail(orderNumber ?? '');
  const cancelOrderMutation = useCancelOrder();

  const handleConfirmCancel = async (reason?: string) => {
    if (!orderNumber) return;
    try {
      await cancelOrderMutation.mutateAsync({
        orderNumber,
        reason,
      });
      toast.success(`Pesanan ${orderNumber} berhasil dibatalkan.`);
      setIsCancelModalOpen(false);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal membatalkan pesanan.';
      toast.error(msg);
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-20 flex justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="container mx-auto px-4 py-16 text-center max-w-md">
        <AlertTriangle className="h-12 w-12 text-destructive mx-auto mb-4" />
        <h2 className="text-xl font-bold">Pesanan Tidak Ditemukan</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          Nomor pesanan yang Anda tuju tidak valid atau Anda tidak memiliki izin.
        </p>
        <Button className="mt-6" onClick={() => navigate('/akun/pesanan')}>
          Kembali ke Daftar Pesanan
        </Button>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 sm:px-6 lg:px-8">
      {/* Breadcrumb Navigation */}
      <nav className="mb-6 flex items-center gap-2 text-xs text-muted-foreground flex-wrap">
        <Link to="/" className="hover:text-foreground">
          Beranda
        </Link>
        <ChevronRight className="h-3 w-3" />
        <Link to="/akun/pesanan" className="hover:text-foreground">
          Pesanan Saya
        </Link>
        <ChevronRight className="h-3 w-3" />
        <span className="font-mono text-foreground font-medium">{order.orderNumber}</span>
      </nav>

      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl sm:text-3xl font-black tracking-tight font-mono text-foreground">
              {order.orderNumber}
            </h1>
            <StatusBadge status={order.status} />
          </div>
          <p className="text-xs sm:text-sm text-muted-foreground mt-1">
            Dibuat pada{' '}
            {new Date(order.createdAt).toLocaleDateString('id-ID', {
              day: 'numeric',
              month: 'long',
              year: 'numeric',
              hour: '2-digit',
              minute: '2-digit',
            })}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {order.status === 'PendingPayment' && (
            <>
              <Button
                variant="destructive"
                size="sm"
                onClick={() => setIsCancelModalOpen(true)}
              >
                <XCircle className="mr-1.5 h-4 w-4" />
                Batalkan Pesanan
              </Button>

              <Button
                size="sm"
                className="bg-amber-600 hover:bg-amber-700 font-semibold"
                onClick={() => navigate(`/checkout/status/${order.orderNumber}`)}
              >
                <CreditCard className="mr-1.5 h-4 w-4" />
                Bayar Sekarang
              </Button>
            </>
          )}

          <Button
            variant="outline"
            size="sm"
            onClick={() => window.print()}
            className="print:hidden"
          >
            <Printer className="mr-1.5 h-4 w-4" />
            Cetak
          </Button>
        </div>
      </div>

      {/* Tracking Number Callout if Shipped */}
      {order.status === 'Shipped' && order.trackingNumber && (
        <div className="mb-6 rounded-lg border border-cyan-400 bg-cyan-50/50 p-4 text-cyan-950 dark:bg-cyan-950/30 dark:text-cyan-200">
          <div className="flex items-start gap-3">
            <Truck className="h-5 w-5 text-cyan-600 dark:text-cyan-400 mt-0.5" />
            <div>
              <h4 className="font-semibold text-sm">Pesanan Sedang Dalam Pengiriman</h4>
              <p className="text-xs sm:text-sm mt-0.5">
                Kurir:{' '}
                <strong className="font-bold text-foreground">
                  {order.courier || 'Ekspedisi Partner'}
                </strong>{' '}
                • Nomor Resi:{' '}
                <span className="font-mono font-bold bg-white dark:bg-black/40 px-2 py-0.5 rounded border">
                  {order.trackingNumber}
                </span>
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Main Grid Layout */}
      <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
        {/* Left Column: Items, Shipping Address, Payment info */}
        <div className="lg:col-span-8 space-y-6">
          {/* Order Items */}
          <Card>
            <CardHeader className="border-b pb-4">
              <CardTitle className="text-base font-bold flex items-center gap-2">
                <Package className="h-4 w-4 text-primary" />
                <span>Rincian Produk ({order.items.length})</span>
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-4 divide-y">
              {order.items.map((item) => (
                <div key={item.id} className="py-4 flex items-center justify-between gap-4">
                  <div className="flex items-center gap-3">
                    {item.imageUrl ? (
                      <img
                        src={item.imageUrl}
                        alt={item.productName}
                        className="h-14 w-14 rounded object-cover border bg-muted"
                      />
                    ) : (
                      <div className="h-14 w-14 rounded border bg-muted flex items-center justify-center text-xs text-muted-foreground">
                        No Image
                      </div>
                    )}
                    <div>
                      <p className="font-medium text-foreground line-clamp-1">{item.productName}</p>
                      <p className="text-xs text-muted-foreground mt-0.5">
                        {item.variantLabel ? `Varian: ${item.variantLabel}` : `SKU: ${item.sku}`}
                      </p>
                      <div className="text-xs text-muted-foreground flex items-center gap-1">
                        <Price price={item.unitPrice} className="inline text-xs font-normal" />
                        <span>× {item.quantity}</span>
                      </div>
                    </div>
                  </div>
                  <Price price={item.lineTotal} className="font-bold text-sm" />
                </div>
              ))}
            </CardContent>
          </Card>

          {/* Shipping Address */}
          <Card>
            <CardHeader className="border-b pb-4">
              <CardTitle className="text-base font-bold flex items-center gap-2">
                <MapPin className="h-4 w-4 text-primary" />
                <span>Informasi Pengiriman</span>
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-4 text-sm space-y-1.5">
              <p className="font-semibold text-foreground">
                {order.shippingAddress.recipientName} ({order.shippingAddress.phone})
              </p>
              <p className="text-muted-foreground">{order.shippingAddress.street}</p>
              <p className="text-muted-foreground text-xs">
                {order.shippingAddress.city}, {order.shippingAddress.provinceName}{' '}
                {order.shippingAddress.postalCode}
              </p>
              <div className="pt-2 text-xs text-muted-foreground border-t mt-3">
                Total Berat Paket: {(order.totalWeightGram / 1000).toFixed(2)} kg
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Right Column: Order Timeline & Financial Summary */}
        <div className="lg:col-span-4 space-y-6">
          {/* Order Timeline */}
          <Card>
            <CardHeader className="border-b pb-4">
              <CardTitle className="text-base font-bold">Pelacakan Status</CardTitle>
            </CardHeader>
            <CardContent className="pt-5">
              <OrderTimeline histories={order.statusHistories} />
            </CardContent>
          </Card>

          {/* Financial Breakdown */}
          <Card>
            <CardHeader className="border-b pb-4">
              <CardTitle className="text-base font-bold">Rincian Pembayaran</CardTitle>
            </CardHeader>
            <CardContent className="pt-4 space-y-3 text-sm">
              <div className="flex justify-between text-muted-foreground">
                <span>Subtotal Produk</span>
                <Price price={order.subtotal} />
              </div>
              <div className="flex justify-between text-muted-foreground">
                <span>Biaya Pengiriman</span>
                <Price price={order.shippingFee} />
              </div>
              <div className="border-t pt-3 flex justify-between font-bold text-base">
                <span>Total Bayar</span>
                <Price price={order.total} className="text-xl text-primary font-black" />
              </div>

              {order.payment && (
                <div className="rounded-md bg-muted/40 p-2.5 text-xs text-muted-foreground space-y-1 mt-3">
                  <div className="flex justify-between">
                    <span>Metode:</span>
                    <span className="font-medium text-foreground">
                      {order.payment.paymentType || 'Midtrans Snap'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span>Status Gateway:</span>
                    <span className="font-medium capitalize text-foreground">
                      {order.payment.status}
                    </span>
                  </div>
                  {order.paidAt && (
                    <div className="flex justify-between">
                      <span>Waktu Bayar:</span>
                      <span>{new Date(order.paidAt).toLocaleTimeString('id-ID')}</span>
                    </div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Cancel Order Dialog */}
      <CancelOrderDialog
        isOpen={isCancelModalOpen}
        onClose={() => setIsCancelModalOpen(false)}
        onConfirm={handleConfirmCancel}
        orderNumber={order.orderNumber}
        isSubmitting={cancelOrderMutation.isPending}
      />
    </div>
  );
};

export default OrderDetailPage;
