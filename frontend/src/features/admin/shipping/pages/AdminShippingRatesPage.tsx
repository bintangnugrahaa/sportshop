import React from 'react';
import {
  Truck,
  Search,
  RefreshCw,
  Edit,
  CheckCircle2,
  XCircle,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Price } from '@/components/shared/Price';
import { toast } from '@/lib/toast';
import {
  useAdminShippingRates,
  useUpdateShippingRateMutation,
  type ShippingRate,
  type UpdateShippingRateInput,
} from '../api/adminShippingRatesApi';
import { EditShippingRateDialog } from '../components/EditShippingRateDialog';

export const AdminShippingRatesPage: React.FC = () => {
  const [searchQuery, setSearchQuery] = React.useState<string>('');
  const [statusFilter, setStatusFilter] = React.useState<'all' | 'active' | 'inactive'>('all');
  const [selectedRate, setSelectedRate] = React.useState<ShippingRate | null>(null);
  const [isDialogOpen, setIsDialogOpen] = React.useState<boolean>(false);

  const { data: rates = [], isLoading, refetch } = useAdminShippingRates();
  const updateRateMutation = useUpdateShippingRateMutation();

  const handleOpenEdit = (rate: ShippingRate) => {
    setSelectedRate(rate);
    setIsDialogOpen(true);
  };

  const handleConfirmEdit = async (input: UpdateShippingRateInput) => {
    if (!selectedRate) return;

    try {
      await updateRateMutation.mutateAsync({
        provinceCode: selectedRate.provinceCode,
        input,
      });
      toast.success(`Tarif ongkir provinsi ${selectedRate.provinceName} berhasil diperbarui.`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal memperbarui tarif ongkir.';
      toast.error(msg);
      throw err;
    }
  };

  const filteredRates = rates.filter((rate) => {
    // Status filter
    if (statusFilter === 'active' && !rate.isActive) return false;
    if (statusFilter === 'inactive' && rate.isActive) return false;

    // Search filter
    if (!searchQuery.trim()) return true;
    const q = searchQuery.toLowerCase();
    return (
      rate.provinceName.toLowerCase().includes(q) ||
      rate.provinceCode.toLowerCase().includes(q)
    );
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Tarif Ongkos Kirim</h1>
          <p className="text-sm text-muted-foreground">
            Kelola tarif dasar, tarif per kilogram, dan estimasi waktu pengiriman flat per provinsi.
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
        <CardContent className="p-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div className="relative flex-1 sm:max-w-xs">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Cari nama atau kode provinsi..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-9 h-9 text-sm"
            />
          </div>

          <div className="flex items-center gap-1.5 self-start sm:self-auto">
            <span className="text-xs text-muted-foreground mr-1">Status:</span>
            {[
              { label: 'Semua', value: 'all' },
              { label: 'Aktif', value: 'active' },
              { label: 'Non-aktif', value: 'inactive' },
            ].map((tab) => (
              <button
                key={tab.value}
                type="button"
                onClick={() => setStatusFilter(tab.value as 'all' | 'active' | 'inactive')}
                className={`rounded-md px-3 py-1 text-xs font-medium transition-colors ${
                  statusFilter === tab.value
                    ? 'bg-primary text-primary-foreground font-semibold shadow-xs'
                    : 'bg-muted/50 hover:bg-muted text-muted-foreground'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Shipping Rates Table */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Truck className="h-4 w-4 text-primary" />
            Daftar Tarif Wilayah ({filteredRates.length} Provinsi)
          </CardTitle>
          <CardDescription>
            Tarif dihitung menggunakan rumus: Biaya Dasar + (Berat Bulat kg × Biaya per Kg).
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3 py-4">
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
            </div>
          ) : filteredRates.length === 0 ? (
            <div className="rounded-lg border border-dashed py-12 text-center text-sm text-muted-foreground">
              Tidak ada data tarif provinsi yang sesuai dengan filter pencarian.
            </div>
          ) : (
            <div className="overflow-x-auto rounded-lg border">
              <table className="w-full text-left text-sm">
                <thead className="border-b bg-muted/50 text-xs text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-medium">Kode</th>
                    <th className="px-4 py-3 font-medium">Provinsi</th>
                    <th className="px-4 py-3 font-medium">Tarif Dasar</th>
                    <th className="px-4 py-3 font-medium">Tarif / Kg</th>
                    <th className="px-4 py-3 font-medium">Estimasi Waktu</th>
                    <th className="px-4 py-3 font-medium text-center">Status</th>
                    <th className="px-4 py-3 font-medium text-right">Aksi</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {filteredRates.map((rate) => (
                    <tr key={rate.provinceCode} className="hover:bg-muted/30 transition-colors">
                      <td className="px-4 py-3 font-mono text-xs font-semibold text-muted-foreground">
                        {rate.provinceCode}
                      </td>
                      <td className="px-4 py-3 font-semibold text-foreground">
                        {rate.provinceName}
                      </td>
                      <td className="px-4 py-3">
                        <Price price={rate.baseFee} className="text-xs font-medium" />
                      </td>
                      <td className="px-4 py-3">
                        <Price price={rate.perKgFee} className="text-xs font-medium" />
                      </td>
                      <td className="px-4 py-3 text-xs text-muted-foreground">
                        {rate.estimatedDaysMin} – {rate.estimatedDaysMax} hari
                      </td>
                      <td className="px-4 py-3 text-center">
                        <Badge
                          variant={rate.isActive ? 'default' : 'secondary'}
                          className={`text-xs ${
                            rate.isActive
                              ? 'bg-emerald-100 text-emerald-800 border-emerald-300 dark:bg-emerald-900/40 dark:text-emerald-200'
                              : 'bg-muted text-muted-foreground'
                          }`}
                        >
                          {rate.isActive ? (
                            <span className="flex items-center gap-1">
                              <CheckCircle2 className="h-3 w-3" /> Aktif
                            </span>
                          ) : (
                            <span className="flex items-center gap-1">
                              <XCircle className="h-3 w-3" /> Non-aktif
                            </span>
                          )}
                        </Badge>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleOpenEdit(rate)}
                          className="h-8 gap-1.5 text-xs font-semibold"
                        >
                          <Edit className="h-3.5 w-3.5" />
                          Edit Tarif
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Edit Shipping Rate Dialog */}
      <EditShippingRateDialog
        isOpen={isDialogOpen}
        onClose={() => {
          setIsDialogOpen(false);
          setSelectedRate(null);
        }}
        rate={selectedRate}
        onConfirm={handleConfirmEdit}
        isSubmitting={updateRateMutation.isPending}
      />
    </div>
  );
};
export default AdminShippingRatesPage;
