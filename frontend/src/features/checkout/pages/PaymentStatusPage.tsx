import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  CheckCircle2,
  Clock,
  XCircle,
  AlertOctagon,
  ArrowRight,
  RefreshCw,
  CreditCard,
  Package,
  Loader2,
  MapPin,
} from 'lucide-react';
import { useOrder, usePayOrder } from '../api/checkoutApi';
import { Price } from '@/components/shared/Price';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { loadSnapScript } from '@/lib/snap';
import { toast } from '@/lib/toast';

export const PaymentStatusPage: React.FC = () => {
  const { orderNumber } = useParams<{ orderNumber: string }>();
  const navigate = useNavigate();

  const {
    data: order,
    isLoading,
    isRefetching,
    refetch,
    error,
  } = useOrder(orderNumber ?? '', true);

  const payOrderMutation = usePayOrder();

  React.useEffect(() => {
    void loadSnapScript().catch(() => {});
  }, []);

  const handlePayNow = async () => {
    if (!orderNumber) return;

    try {
      const response = await payOrderMutation.mutateAsync(orderNumber);

      if (response.snapToken && window.snap) {
        window.snap.pay(response.snapToken, {
          onSuccess: () => {
            void refetch();
          },
          onPending: () => {
            void refetch();
          },
          onError: () => {
            void refetch();
          },
          onClose: () => {
            void refetch();
          },
        });
      } else if (response.snapRedirectUrl) {
        window.location.href = response.snapRedirectUrl;
      } else {
        toast.info('Token pembayaran sedang dipersiapkan, silakan klik tombol Cek Status.');
        void refetch();
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal memproses pembayaran ulang.';
      toast.error(msg);
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-20 flex flex-col items-center justify-center text-center">
        <Loader2 className="h-10 w-10 animate-spin text-primary mb-4" />
        <h2 className="text-lg font-semibold">Memeriksa Status Pembayaran...</h2>
        <p className="text-sm text-muted-foreground mt-1">
          Mohon tunggu sebentar, sistem sedang menghubungkan ke gateway pembayaran.
        </p>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="container mx-auto px-4 py-16 text-center max-w-md">
        <AlertOctagon className="h-12 w-12 text-destructive mx-auto mb-4" />
        <h2 className="text-xl font-bold">Pesanan Tidak Ditemukan</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          Nomor pesanan yang Anda cari tidak ditemukan atau Anda tidak memiliki akses ke pesanan ini.
        </p>
        <Button className="mt-6" onClick={() => navigate('/')}>
          Kembali ke Beranda
        </Button>
      </div>
    );
  }

  const isPaid = order.status === 'Paid';
  const isPending = order.status === 'PendingPayment';
  const isCancelled = order.status === 'Cancelled';
  const isExpired = order.status === 'Expired';

  return (
    <div className="container mx-auto px-4 py-10 sm:px-6 lg:px-8 max-w-2xl">
      {/* Status Hero Card */}
      <Card className="text-center p-6 sm:p-8 overflow-hidden shadow-lg border-2">
        {isPaid && (
          <div className="flex flex-col items-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100 text-emerald-600 dark:bg-emerald-950 dark:text-emerald-400 mb-4 animate-in zoom-in-50">
              <CheckCircle2 className="h-10 w-10" />
            </div>
            <h1 className="text-2xl sm:text-3xl font-black text-foreground">
              Pembayaran Berhasil!
            </h1>
            <p className="mt-2 text-sm text-muted-foreground max-w-md">
              Terima kasih! Pembayaran Anda telah kami terima dan pesanan akan segera dipersiapkan
              oleh staf kami.
            </p>
          </div>
        )}

        {isPending && (
          <div className="flex flex-col items-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-amber-100 text-amber-600 dark:bg-amber-950 dark:text-amber-400 mb-4 animate-pulse">
              <Clock className="h-10 w-10" />
            </div>
            <h1 className="text-2xl sm:text-3xl font-black text-foreground">
              Menunggu Pembayaran
            </h1>
            <p className="mt-2 text-sm text-muted-foreground max-w-md">
              Selesaikan pembayaran Anda sebelum batas waktu berakhir. Status pesanan akan otomatis
              terperbarui setelah pembayaran terkonfirmasi.
            </p>
            {order.expiresAt && (
              <p className="mt-2 text-xs font-semibold text-destructive">
                Batas Waktu: {new Date(order.expiresAt).toLocaleString('id-ID')}
              </p>
            )}
          </div>
        )}

        {isCancelled && (
          <div className="flex flex-col items-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10 text-destructive mb-4">
              <XCircle className="h-10 w-10" />
            </div>
            <h1 className="text-2xl sm:text-3xl font-black text-foreground">
              Pesanan Dibatalkan
            </h1>
            <p className="mt-2 text-sm text-muted-foreground max-w-md">
              Pesanan ini telah dibatalkan.{' '}
              {order.cancelReason ? `Alasan: ${order.cancelReason}` : ''} Stok produk telah
              dikembalikan ke sistem.
            </p>
          </div>
        )}

        {isExpired && (
          <div className="flex flex-col items-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-muted text-muted-foreground mb-4">
              <AlertOctagon className="h-10 w-10" />
            </div>
            <h1 className="text-2xl sm:text-3xl font-black text-foreground">
              Pesanan Kedaluwarsa
            </h1>
            <p className="mt-2 text-sm text-muted-foreground max-w-md">
              Batas waktu 24 jam pembayaran telah habis. Pesanan dibatalkan otomatis dan stok produk
              telah dikembalikan.
            </p>
          </div>
        )}

        {/* Order Details Summary Box */}
        <div className="mt-6 rounded-lg border bg-muted/30 p-4 text-left text-sm space-y-2">
          <div className="flex justify-between items-center">
            <span className="text-muted-foreground">Nomor Pesanan</span>
            <span className="font-mono font-bold text-foreground">{order.orderNumber}</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-muted-foreground">Status</span>
            <StatusBadge status={order.status} />
          </div>
          <div className="flex justify-between items-center">
            <span className="text-muted-foreground">Total Tagihan</span>
            <Price price={order.total} className="text-base font-extrabold text-primary" />
          </div>
          {order.shippingAddress && (
            <div className="border-t pt-2 mt-2 flex items-start gap-2 text-xs text-muted-foreground">
              <MapPin className="h-3.5 w-3.5 flex-shrink-0 text-primary mt-0.5" />
              <span>
                Penerima: {order.shippingAddress.recipientName} ({order.shippingAddress.phone}) –{' '}
                {order.shippingAddress.city}, {order.shippingAddress.provinceName}
              </span>
            </div>
          )}
        </div>

        {/* Action Buttons */}
        <div className="mt-8 flex flex-col sm:flex-row items-center justify-center gap-3">
          {isPending && (
            <>
              <Button
                className="w-full sm:w-auto h-11 px-6 font-semibold flex items-center justify-center gap-2"
                onClick={handlePayNow}
                disabled={payOrderMutation.isPending}
                data-testid="pay-now-btn"
              >
                {payOrderMutation.isPending ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    <span>Mempersiapkan...</span>
                  </>
                ) : (
                  <>
                    <CreditCard className="h-4 w-4" />
                    <span>Bayar Sekarang</span>
                  </>
                )}
              </Button>

              <Button
                variant="outline"
                className="w-full sm:w-auto h-11 px-4 text-xs"
                onClick={() => void refetch()}
                disabled={isRefetching}
              >
                <RefreshCw className={`mr-1.5 h-3.5 w-3.5 ${isRefetching ? 'animate-spin' : ''}`} />
                <span>Cek Status</span>
              </Button>
            </>
          )}

          <Button
            variant={isPending ? 'ghost' : 'default'}
            className="w-full sm:w-auto h-11 px-6"
            onClick={() => navigate(`/akun/pesanan/${order.orderNumber}`)}
          >
            <Package className="mr-2 h-4 w-4" />
            <span>Lihat Detail Pesanan</span>
          </Button>

          <Button
            variant="outline"
            className="w-full sm:w-auto h-11 px-6"
            onClick={() => navigate('/produk')}
          >
            <span>Lanjut Belanja</span>
            <ArrowRight className="ml-2 h-4 w-4" />
          </Button>
        </div>
      </Card>
    </div>
  );
};

export default PaymentStatusPage;
