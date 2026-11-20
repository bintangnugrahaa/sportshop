import React from 'react';
import { X, Truck, AlertCircle, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface ShipOrderDialogProps {
  isOpen: boolean;
  onClose: () => void;
  orderNumber: string;
  onConfirm: (courier: string, trackingNumber: string, note?: string) => Promise<void>;
  isSubmitting?: boolean;
}

const COMMON_COURIERS = ['JNE', 'J&T Express', 'SiCepat', 'POS Indonesia', 'Anteraja', 'TIKI'];

export const ShipOrderDialog: React.FC<ShipOrderDialogProps> = ({
  isOpen,
  onClose,
  orderNumber,
  onConfirm,
  isSubmitting = false,
}) => {
  const [courier, setCourier] = React.useState<string>('JNE');
  const [trackingNumber, setTrackingNumber] = React.useState<string>('');
  const [note, setNote] = React.useState<string>('');
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (isOpen) {
      setCourier('JNE');
      setTrackingNumber('');
      setNote('');
      setError(null);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!courier.trim()) {
      setError('Nama kurir pengiriman wajib diisi.');
      return;
    }

    if (!trackingNumber.trim() || trackingNumber.trim().length < 3) {
      setError('Nomor resi wajib diisi minimal 3 karakter.');
      return;
    }

    try {
      await onConfirm(courier.trim(), trackingNumber.trim(), note.trim() || undefined);
      onClose();
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Gagal memperbarui status pengiriman.');
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
            <h2 className="text-base font-bold text-foreground">Kirim Pesanan & Input Resi</h2>
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

        <div className="mt-3 text-xs text-muted-foreground">
          Pesanan: <span className="font-mono font-bold text-foreground">{orderNumber}</span>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          {error && (
            <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-xs text-destructive">
              <AlertCircle className="h-4 w-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="space-y-1.5">
            <Label htmlFor="courier-select">Ekspedisi / Kurir *</Label>
            <div className="flex flex-wrap gap-1.5 mb-2">
              {COMMON_COURIERS.map((c) => (
                <button
                  key={c}
                  type="button"
                  onClick={() => setCourier(c)}
                  className={`rounded-md border px-2.5 py-1 text-xs font-medium transition-colors ${
                    courier === c
                      ? 'border-primary bg-primary text-primary-foreground font-semibold'
                      : 'border-input hover:bg-muted text-muted-foreground'
                  }`}
                >
                  {c}
                </button>
              ))}
            </div>
            <Input
              id="courier-select"
              placeholder="Atau ketik nama kurir..."
              value={courier}
              onChange={(e) => setCourier(e.target.value)}
              disabled={isSubmitting}
              required
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="tracking-number">Nomor Resi Pengiriman *</Label>
            <Input
              id="tracking-number"
              placeholder="Contoh: SOCAG0123456789"
              value={trackingNumber}
              onChange={(e) => setTrackingNumber(e.target.value.toUpperCase())}
              disabled={isSubmitting}
              className="font-mono"
              required
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="ship-note">Catatan Pengiriman (Opsional)</Label>
            <Input
              id="ship-note"
              placeholder="Contoh: Paket diantar kurir pick up sore"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              disabled={isSubmitting}
            />
          </div>

          <div className="mt-6 flex justify-end gap-2 border-t pt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Batal
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Mengirim...
                </>
              ) : (
                'Kirim Pesanan'
              )}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
