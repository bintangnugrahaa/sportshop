import React from 'react';
import { X, AlertTriangle, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface AdminCancelOrderDialogProps {
  isOpen: boolean;
  onClose: () => void;
  orderNumber: string;
  onConfirm: (reason?: string) => Promise<void>;
  isSubmitting?: boolean;
}

export const AdminCancelOrderDialog: React.FC<AdminCancelOrderDialogProps> = ({
  isOpen,
  onClose,
  orderNumber,
  onConfirm,
  isSubmitting = false,
}) => {
  const [reason, setReason] = React.useState('');

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onConfirm(reason.trim() || undefined);
    onClose();
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
          <div className="flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-5 w-5" />
            <h2 className="text-base font-bold text-foreground">Batalkan Pesanan (Admin)</h2>
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

        <p className="mt-3 text-xs text-muted-foreground leading-relaxed">
          Apakah Anda yakin ingin membatalkan pesanan{' '}
          <strong className="text-foreground">{orderNumber}</strong>? Stok produk yang dipesan akan otomatis
          dikembalikan ke gudang.
        </p>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="admin-cancel-reason">Alasan Pembatalan (Opsional)</Label>
            <Input
              id="admin-cancel-reason"
              placeholder="Contoh: Permintaan pembeli / Stok fisik rusak"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              disabled={isSubmitting}
            />
          </div>

          <div className="mt-6 flex justify-end gap-2 border-t pt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Kembali
            </Button>
            <Button type="submit" variant="destructive" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Membatalkan...
                </>
              ) : (
                'Konfirmasi Batalkan'
              )}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
