import React from 'react';
import { Link } from 'react-router-dom';
import {
  TrendingUp,
  ShoppingBag,
  AlertTriangle,
  Clock,
  ArrowRight,
  Boxes,
  Loader2,
} from 'lucide-react';
import { useAdminDashboard } from '../api/adminDashboardApi';
import { Price } from '@/components/shared/Price';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { OrderStatus } from '@/types';

export const AdminDashboardPage: React.FC = () => {
  const { data, isLoading, error } = useAdminDashboard();

  if (isLoading) {
    return (
      <div className="py-20 flex flex-col items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary mb-3" />
        <p className="text-sm text-muted-foreground">Memuat data ringkasan dashboard...</p>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="py-12 text-center max-w-md mx-auto">
        <AlertTriangle className="h-10 w-10 text-destructive mx-auto mb-3" />
        <h2 className="text-lg font-bold">Gagal Memuat Dashboard</h2>
        <p className="text-sm text-muted-foreground mt-1">
          Terjadi kesalahan saat memuat data statistik dashboard.
        </p>
      </div>
    );
  }

  const { kpi, revenueChart, topSellingProducts, lowStockProducts, recentOrders } = data;

  return (
    <div className="space-y-8">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl sm:text-3xl font-black tracking-tight text-foreground">
          Dashboard Ringkasan
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          Pantau metrik penjualan toko, ketersediaan inventaris, dan aktivitas pesanan terkini.
        </p>
      </div>

      {/* KPI Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Card 1: Orders Today */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-semibold text-muted-foreground uppercase">
              Pesanan Hari Ini
            </CardTitle>
            <ShoppingBag className="h-4 w-4 text-primary" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-black text-foreground">{kpi.ordersToday}</div>
            <p className="text-xs text-muted-foreground mt-1">Transaksi masuk hari ini</p>
          </CardContent>
        </Card>

        {/* Card 2: Revenue 7 Days */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-semibold text-muted-foreground uppercase">
              Omzet 7 Hari
            </CardTitle>
            <TrendingUp className="h-4 w-4 text-emerald-600" />
          </CardHeader>
          <CardContent>
            <Price price={kpi.revenue7Days} className="text-2xl font-black text-emerald-600" />
            <p className="text-xs text-muted-foreground mt-1">Pendapatan seminggu terakhir</p>
          </CardContent>
        </Card>

        {/* Card 3: Pending Processing */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-semibold text-muted-foreground uppercase">
              Perlu Diproses
            </CardTitle>
            <Clock className="h-4 w-4 text-amber-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-black text-amber-600">
              {kpi.pendingProcessingOrders}
            </div>
            <p className="text-xs text-muted-foreground mt-1">Pesanan lunas menunggu kirim</p>
          </CardContent>
        </Card>

        {/* Card 4: Low Stock */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
            <CardTitle className="text-xs font-semibold text-muted-foreground uppercase">
              Stok Menipis
            </CardTitle>
            <Boxes className="h-4 w-4 text-rose-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-black text-rose-600">{kpi.lowStockCount}</div>
            <p className="text-xs text-muted-foreground mt-1">Varian dengan stok ≤ 5</p>
          </CardContent>
        </Card>
      </div>

      {/* 30-Day Revenue Trend Bar/Chart */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between border-b pb-4">
          <div>
            <CardTitle className="text-base font-bold">Tren Penjualan 30 Hari Terakhir</CardTitle>
            <div className="text-xs text-muted-foreground mt-0.5 flex items-center gap-1">
              <span>Total omzet 30 hari:</span>
              <Price price={kpi.revenue30Days} className="inline font-bold text-foreground" />
            </div>
          </div>
        </CardHeader>
        <CardContent className="pt-6">
          {revenueChart.length === 0 ? (
            <p className="text-xs text-muted-foreground py-8 text-center">
              Belum ada data riwayat omzet 30 hari.
            </p>
          ) : (
            <div className="space-y-3">
              <div className="flex items-end gap-1.5 h-36 overflow-x-auto pb-2 pt-4 px-1">
                {revenueChart.map((d) => {
                  const maxRevenue = Math.max(...revenueChart.map((r) => r.revenue), 1);
                  const heightPercent = Math.max(Math.round((d.revenue / maxRevenue) * 100), 4);

                  return (
                    <div
                      key={d.date}
                      className="flex-1 min-w-[20px] flex flex-col items-center justify-end h-full group relative"
                    >
                      <div
                        style={{ height: `${heightPercent}%` }}
                        className="w-full rounded-t bg-primary/70 hover:bg-primary transition-all cursor-pointer"
                        title={`${d.date}: ${d.ordersCount} pesanan (Rp ${d.revenue.toLocaleString('id-ID')})`}
                      />
                    </div>
                  );
                })}
              </div>
              <div className="flex justify-between text-[11px] text-muted-foreground border-t pt-2">
                <span>{revenueChart[0]?.date}</span>
                <span>{revenueChart[revenueChart.length - 1]?.date}</span>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Two Column Grid: Top Selling Products & Low Stock Alert */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Top Selling Products */}
        <Card>
          <CardHeader className="border-b pb-4 flex flex-row items-center justify-between">
            <CardTitle className="text-base font-bold flex items-center gap-2">
              <TrendingUp className="h-4 w-4 text-primary" />
              <span>Produk Terlaris</span>
            </CardTitle>
          </CardHeader>
          <CardContent className="pt-4 divide-y">
            {topSellingProducts.length === 0 ? (
              <p className="text-xs text-muted-foreground py-6 text-center">
                Belum ada data produk terlaris.
              </p>
            ) : (
              topSellingProducts.map((p) => (
                <div key={p.productId} className="py-3 flex items-center justify-between gap-4">
                  <div className="flex items-center gap-3">
                    {p.imageUrl ? (
                      <img
                        src={p.imageUrl}
                        alt={p.productName}
                        className="h-10 w-10 rounded object-cover border bg-muted"
                      />
                    ) : (
                      <div className="h-10 w-10 rounded border bg-muted flex items-center justify-center text-[10px] text-muted-foreground">
                        Item
                      </div>
                    )}
                    <div>
                      <p className="text-xs sm:text-sm font-semibold text-foreground line-clamp-1">
                        {p.productName}
                      </p>
                      <p className="text-[11px] text-muted-foreground">SKU: {p.sku}</p>
                    </div>
                  </div>
                  <div className="text-right text-xs">
                    <span className="font-bold text-foreground block">
                      {p.totalQuantitySold} terjual
                    </span>
                    <Price price={p.totalRevenue} className="text-xs text-muted-foreground" />
                  </div>
                </div>
              ))
            )}
          </CardContent>
        </Card>

        {/* Low Stock Alerts */}
        <Card>
          <CardHeader className="border-b pb-4 flex flex-row items-center justify-between">
            <CardTitle className="text-base font-bold flex items-center gap-2 text-rose-600">
              <Boxes className="h-4 w-4" />
              <span>Stok Menipis (≤ 5)</span>
            </CardTitle>
            <Link
              to="/admin/stok"
              className="inline-flex items-center text-xs h-7 px-2.5 rounded-md hover:bg-accent text-foreground transition-colors"
            >
              <span>Kelola Stok</span>
              <ArrowRight className="ml-1 h-3 w-3" />
            </Link>
          </CardHeader>
          <CardContent className="pt-4 divide-y">
            {lowStockProducts.length === 0 ? (
              <p className="text-xs text-emerald-600 py-6 text-center font-medium">
                Semua stok produk dalam kondisi aman.
              </p>
            ) : (
              lowStockProducts.map((item) => (
                <div key={item.variantId} className="py-3 flex items-center justify-between gap-4">
                  <div>
                    <p className="text-xs sm:text-sm font-semibold text-foreground line-clamp-1">
                      {item.productName}
                    </p>
                    <p className="text-[11px] text-muted-foreground">
                      {[item.optionSize, item.optionColor].filter(Boolean).join(' / ') || item.sku}
                    </p>
                  </div>
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-bold bg-rose-100 text-rose-800 dark:bg-rose-950 dark:text-rose-200">
                    Sisa {item.stockQuantity}
                  </span>
                </div>
              ))
            )}
          </CardContent>
        </Card>
      </div>

      {/* Recent Orders Table */}
      <Card>
        <CardHeader className="border-b pb-4 flex flex-row items-center justify-between">
          <div>
            <CardTitle className="text-base font-bold">Pesanan Terbaru</CardTitle>
            <CardDescription className="text-xs mt-0.5">
              10 transaksi pesanan pelanggan terakhir yang masuk ke sistem.
            </CardDescription>
          </div>
          <Link
            to="/admin/pesanan"
            className="inline-flex items-center text-xs h-8 px-3 rounded-md border border-input bg-background hover:bg-accent text-foreground transition-colors"
          >
            <span>Semua Pesanan</span>
            <ArrowRight className="ml-1.5 h-3.5 w-3.5" />
          </Link>
        </CardHeader>
        <CardContent className="pt-4 overflow-x-auto">
          {recentOrders.length === 0 ? (
            <p className="text-xs text-muted-foreground py-6 text-center">
              Belum ada pesanan terbaru.
            </p>
          ) : (
            <table className="w-full text-left text-xs sm:text-sm">
              <thead className="border-b text-muted-foreground text-xs uppercase bg-muted/40">
                <tr>
                  <th className="py-2.5 px-3">No. Pesanan</th>
                  <th className="py-2.5 px-3">Pelanggan</th>
                  <th className="py-2.5 px-3">Jumlah Item</th>
                  <th className="py-2.5 px-3">Total</th>
                  <th className="py-2.5 px-3">Status</th>
                  <th className="py-2.5 px-3">Waktu</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {recentOrders.map((ord) => (
                  <tr key={ord.id} className="hover:bg-muted/30">
                    <td className="py-3 px-3 font-mono font-bold text-foreground">
                      <Link
                        to={`/admin/pesanan/${ord.orderNumber}`}
                        className="hover:text-primary transition-colors"
                      >
                        {ord.orderNumber}
                      </Link>
                    </td>
                    <td className="py-3 px-3">{ord.customerName}</td>
                    <td className="py-3 px-3 text-muted-foreground">{ord.itemsCount} produk</td>
                    <td className="py-3 px-3 font-semibold">
                      <Price price={ord.total} />
                    </td>
                    <td className="py-3 px-3">
                      <StatusBadge status={ord.status as OrderStatus} />
                    </td>
                    <td className="py-3 px-3 text-muted-foreground text-xs">
                      {new Date(ord.createdAt).toLocaleDateString('id-ID', {
                        day: 'numeric',
                        month: 'short',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

export default AdminDashboardPage;
