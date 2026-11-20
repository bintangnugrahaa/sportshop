import React from 'react';
import { X, Truck, AlertCircle, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { ShippingRate, UpdateShippingRateInput } from '../api/adminShippingRatesApi';

interface EditShippingRateDialogProps {
  isOpen: boolean;
  onClose: () => void;
  rate: ShippingRate | null;
  onConfirm: (input: UpdateShippingRateInput) => Promise<void>;
  isSubmitting?: boolean;
}

export const EditShippingRateDialog: React.FC<EditShippingRateDialogProps> = ({
  isOpen,
  onClose,
  rate,
  onConfirm,
  isSubmitting = false,
}) => {
  const [baseFee, setBaseFee] = React.useState<number>(0);
  const [perKgFee, setPerKgFee] = React.useState<number>(0);
  const [minDays, setMinDays] = React.useState<number>(1);
  const [maxDays, setMaxDays] = React.useState<number>(3);
  const [isActive, setIsActive] = React.useState<boolean>(true);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (rate && isOpen) {
      setBaseFee(rate.baseFee);
      setPerKgFee(rate.perKgFee);
      setMinDays(rate.estimatedDaysMin);
      setMaxDays(rate.estimatedDaysMax);
      setIsActive(rate.isActive);
      setError(null);
    }
  }, [rate, isOpen]);

  if (!isOpen || !rate) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (baseFee < 0 || isNaN(baseFee)) {
      setError('Biaya dasar pengiriman tidak boleh negatif.');
      return;
    }

    if (perKgFee < 0 || isNaN(perKgFee)) {
      setError('Biaya per kilogram tidak boleh negatif.');
      return;
    }

    if (minDays < 0 || isNaN(minDays)) {
      setError('Estimasi hari minimum tidak boleh negatif.');
      return;
    }

    if (maxDays < minDays || isNaN(maxDays)) {
      setError('Estimasi hari maksimum harus lebih besar atau sama dengan hari minimum.');
      return;
    }

    try {
      await onConfirm({
        baseFee,
        perKgFee,
        estimatedDaysMin: minDays,
        estimatedDaysMax: maxDays,
        isActive,
      });
      onClose();
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Gagal memperbarui tarif ongkos kirim.');
      }
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4" role="dialog" aria-modal="true">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/60 backdrop-blur-sm animate-in fade-in"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Modal Content */}
      <div className="relative z-50 w-full max-w-md rounded-xl border bg-background p-6 shadow-2xl animate-in zoom-in-95">
        <div className="flex items-start justify-between border-b pb-3">
          <div className="flex items-center gap-2 text-primary">
            <Truck className="h-5 w-5" />
            <h2 className="text-base font-bold text-foreground">Edit Tarif Ongkir</h2>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={onClose}
            className="h-8 w-8 p-0"
            disabled={isSubmitting}
            aria-label="Tutup dialog"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        <div className="mt-3 rounded-lg bg-muted/40 p-3 text-xs border">
          <div className="text-muted-foreground">Provinsi:</div>
          <div className="text-sm font-bold text-foreground mt-0.5">{rate.provinceName}</div>
          <div className="font-mono text-muted-foreground mt-0.5">Kode: {rate.provinceCode}</div>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          {error && (
            <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-xs text-destructive">
              <AlertCircle className="h-4 w-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="base-fee">Tarif Dasar (Rp) *</Label>
              <Input
                id="base-fee"
                type="number"
                min="0"
                value={baseFee}
                onChange={(e) => setBaseFee(Math.max(0, parseInt(e.target.value) || 0))}
                disabled={isSubmitting}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="per-kg-fee">Tarif per Kg (Rp) *</Label>
              <Input
                id="per-kg-fee"
                type="number"
                min="0"
                value={perKgFee}
                onChange={(e) => setPerKgFee(Math.max(0, parseInt(e.target.value) || 0))}
                disabled={isSubmitting}
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="min-days">Estimasi Min (Hari) *</Label>
              <Input
                id="min-days"
                type="number"
                min="0"
                value={minDays}
                onChange={(e) => setMinDays(Math.max(0, parseInt(e.target.value) || 0))}
                disabled={isSubmitting}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="max-days">Estimasi Max (Hari) *</Label>
              <Input
                id="max-days"
                type="number"
                min="0"
                value={maxDays}
                onChange={(e) => setMaxDays(Math.max(0, parseInt(e.target.value) || 0))}
                disabled={isSubmitting}
                required
              />
            </div>
          </div>

          <div className="flex items-center gap-2 pt-1">
            <input
              id="is-active"
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              disabled={isSubmitting}
              className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <Label htmlFor="is-active" className="cursor-pointer text-xs font-medium">
              Aktifkan Tarif Pengiriman Ini (Customer dapat memilih provinsi ini saat checkout)
            </Label>
          </div>

          <div className="mt-6 flex justify-end gap-2 border-t pt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Batal
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Menyimpan...
                </>
              ) : (
                'Simpan Tarif'
              )}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
