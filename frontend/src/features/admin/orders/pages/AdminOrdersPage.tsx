import React from 'react';
import { Link } from 'react-router-dom';
import {
  ShoppingBag,
  Search,
  RefreshCw,
  Eye,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { Price } from '@/components/shared/Price';
import { OrderStatus } from '@/types';
import { useAdminOrders } from '../api/adminOrdersApi';

const STATUS_OPTIONS: { label: string; value?: OrderStatus }[] = [
  { label: 'Semua Status' },
  { label: 'Menunggu Pembayaran', value: 'PendingPayment' },
  { label: 'Dibayar', value: 'Paid' },
  { label: 'Diproses', value: 'Processing' },
  { label: 'Dikirim', value: 'Shipped' },
  { label: 'Selesai', value: 'Completed' },
  { label: 'Dibatalkan', value: 'Cancelled' },
  { label: 'Kedaluwarsa', value: 'Expired' },
];

export const AdminOrdersPage: React.FC = () => {
  const [selectedStatus, setSelectedStatus] = React.useState<OrderStatus | undefined>(undefined);
  const [searchQuery, setSearchQuery] = React.useState<string>('');
  const [page, setPage] = React.useState<number>(1);

  const {
    data,
    isLoading,
    refetch,
  } = useAdminOrders({
    status: selectedStatus,
    q: searchQuery.trim() || undefined,
    page,
    pageSize: 15,
  });

  const orders = data?.items || [];
  const totalPages = data?.totalPages || 1;

  const handleStatusChange = (status?: OrderStatus) => {
    setSelectedStatus(status);
    setPage(1);
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    refetch();
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Daftar Pesanan</h1>
          <p className="text-sm text-muted-foreground">
            Kelola pesanan customer, proses pembayaran, input resi pengiriman, dan lacak status pesanan.
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => refetch()}
          className="h-9 gap-1.5 text-xs font-semibold self-start sm:self-auto"
        >
          <RefreshCw className="h-3.5 w-3.5" />
          Segarkan Data
        </Button>
      </div>

      {/* Filter and Search Bar */}
      <Card>
        <CardContent className="p-4 space-y-3">
          <form onSubmit={handleSearchSubmit} className="flex flex-col sm:flex-row gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Cari nomor pesanan atau nama customer..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 h-9 text-sm"
              />
            </div>
            <Button type="submit" size="sm" className="h-9 text-xs">
              Cari
            </Button>
          </form>

          {/* Status Tabs */}
          <div className="flex flex-wrap gap-1.5 border-t pt-3">
            {STATUS_OPTIONS.map((opt) => (
              <button
                key={opt.label}
                type="button"
                onClick={() => handleStatusChange(opt.value)}
                className={`rounded-md px-3 py-1 text-xs font-medium transition-colors ${
                  selectedStatus === opt.value
                    ? 'bg-primary text-primary-foreground font-semibold shadow-xs'
                    : 'bg-muted/50 hover:bg-muted text-muted-foreground'
                }`}
              >
                {opt.label}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Orders Table */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <ShoppingBag className="h-4 w-4 text-primary" />
            Total Pesanan ({data?.totalItems ?? 0})
          </CardTitle>
          <CardDescription>
            Menampilkan data pesanan sesuai kriteria filter yang dipilih.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3 py-4">
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
            </div>
          ) : orders.length === 0 ? (
            <div className="rounded-lg border border-dashed py-12 text-center text-sm text-muted-foreground">
              Tidak ada pesanan yang sesuai dengan filter.
            </div>
          ) : (
            <div className="overflow-x-auto rounded-lg border">
              <table className="w-full text-left text-sm">
                <thead className="border-b bg-muted/50 text-xs text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-medium">Nomor Pesanan</th>
                    <th className="px-4 py-3 font-medium">Tanggal</th>
                    <th className="px-4 py-3 font-medium">Customer</th>
                    <th className="px-4 py-3 font-medium">Total</th>
                    <th className="px-4 py-3 font-medium">Status</th>
                    <th className="px-4 py-3 font-medium text-right">Aksi</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {orders.map((order) => (
                    <tr key={order.id} className="hover:bg-muted/30 transition-colors">
                      <td className="px-4 py-3 font-mono font-bold text-xs text-primary">
                        <Link
                          to={`/admin/pesanan/${order.orderNumber}`}
                          className="hover:underline"
                        >
                          {order.orderNumber}
                        </Link>
                      </td>
                      <td className="px-4 py-3 text-xs text-muted-foreground">
                        {new Date(order.createdAt).toLocaleDateString('id-ID', {
                          day: 'numeric',
                          month: 'short',
                          year: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </td>
                      <td className="px-4 py-3">
                        <div className="font-semibold text-xs text-foreground">
                          {order.shippingAddress?.recipientName || '-'}
                        </div>
                        <div className="text-[11px] text-muted-foreground">
                          {order.shippingAddress?.city}, {order.shippingAddress?.provinceName}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <Price price={order.total} className="text-xs font-semibold" />
                        <div className="text-[11px] text-muted-foreground">
                          {order.items?.length || 0} item
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <StatusBadge status={order.status} />
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Link
                          to={`/admin/pesanan/${order.orderNumber}`}
                          className="inline-flex items-center gap-1 rounded-md px-2.5 py-1.5 text-xs font-semibold text-primary hover:bg-primary/10 transition-colors"
                        >
                          <Eye className="h-3.5 w-3.5" />
                          Detail
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t pt-4 text-xs text-muted-foreground">
              <div>
                Halaman {page} dari {totalPages}
              </div>
              <div className="flex items-center gap-1">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1}
                  className="h-8 gap-1 text-xs"
                >
                  <ChevronLeft className="h-3.5 w-3.5" />
                  Sebelumnya
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page >= totalPages}
                  className="h-8 gap-1 text-xs"
                >
                  Berikutnya
                  <ChevronRight className="h-3.5 w-3.5" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
};
export default AdminOrdersPage;
