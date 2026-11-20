import React from 'react';
import { X, Boxes, AlertCircle, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';

export interface TargetVariantForAdjustment {
  variantId: string;
  productName: string;
  sku: string;
  optionSize?: string | null;
  optionColor?: string | null;
  currentStock: number;
}

interface AdjustStockDialogProps {
  isOpen: boolean;
  onClose: () => void;
  variant: TargetVariantForAdjustment | null;
  onConfirm: (changeQuantity: number, note: string) => Promise<void>;
  isSubmitting?: boolean;
}

export const AdjustStockDialog: React.FC<AdjustStockDialogProps> = ({
  isOpen,
  onClose,
  variant,
  onConfirm,
  isSubmitting = false,
}) => {
  const [adjustmentType, setAdjustmentType] = React.useState<'add' | 'subtract'>('add');
  const [amount, setAmount] = React.useState<number>(1);
  const [note, setNote] = React.useState<string>('');
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (isOpen) {
      setAdjustmentType('add');
      setAmount(1);
      setNote('');
      setError(null);
    }
  }, [isOpen]);

  if (!isOpen || !variant) return null;

  const currentStock = variant.currentStock;
  const changeQuantity = adjustmentType === 'add' ? amount : -amount;
  const calculatedStock = currentStock + changeQuantity;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (amount <= 0 || isNaN(amount)) {
      setError('Jumlah penyesuaian harus lebih dari 0.');
      return;
    }

    if (adjustmentType === 'subtract' && amount > currentStock) {
      setError(`Pengurangan stok tidak boleh melebihi stok saat ini (${currentStock}).`);
      return;
    }

    if (!note.trim() || note.trim().length < 3) {
      setError('Alasan penyesuaian wajib diisi minimal 3 karakter.');
      return;
    }

    try {
      await onConfirm(changeQuantity, note.trim());
      onClose();
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Terjadi kesalahan saat menyimpan penyesuaian stok.');
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
            <Boxes className="h-5 w-5" />
            <h2 className="text-base font-bold text-foreground">Penyesuaian Stok Manual</h2>
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

        <div className="mt-4 rounded-lg bg-muted/50 p-3 space-y-1.5 border">
          <div className="text-xs text-muted-foreground">Detail Varian:</div>
          <p className="text-sm font-semibold text-foreground">{variant.productName}</p>
          <div className="flex flex-wrap items-center gap-2 pt-1 text-xs text-muted-foreground">
            <span className="font-mono bg-background px-1.5 py-0.5 rounded border">{variant.sku}</span>
            {variant.optionSize && <span>Ukuran: {variant.optionSize}</span>}
            {variant.optionColor && <span>Warna: {variant.optionColor}</span>}
            <Badge variant="outline" className="ml-auto font-medium">
              Stok Saat Ini: {currentStock}
            </Badge>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          {error && (
            <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-xs text-destructive">
              <AlertCircle className="h-4 w-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="space-y-1.5">
            <Label>Tipe Penyesuaian</Label>
            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => setAdjustmentType('add')}
                className={`flex items-center justify-center gap-1.5 rounded-lg border py-2 text-sm font-medium transition-colors ${
                  adjustmentType === 'add'
                    ? 'border-primary bg-primary/10 text-primary font-semibold'
                    : 'border-input hover:bg-muted text-muted-foreground'
                }`}
              >
                <span>➕ Tambah Stok</span>
              </button>
              <button
                type="button"
                onClick={() => setAdjustmentType('subtract')}
                className={`flex items-center justify-center gap-1.5 rounded-lg border py-2 text-sm font-medium transition-colors ${
                  adjustmentType === 'subtract'
                    ? 'border-destructive bg-destructive/10 text-destructive font-semibold'
                    : 'border-input hover:bg-muted text-muted-foreground'
                }`}
              >
                <span>➖ Kurangi Stok</span>
              </button>
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="adjustment-amount">
              Jumlah {adjustmentType === 'add' ? 'Penambahan' : 'Pengurangan'}
            </Label>
            <Input
              id="adjustment-amount"
              type="number"
              min="1"
              value={amount}
              onChange={(e) => setAmount(Math.max(1, parseInt(e.target.value) || 0))}
              disabled={isSubmitting}
            />
          </div>

          <div className="rounded-lg border bg-muted/30 p-3 flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Estimasi Stok Akhir:</span>
            <span className={`font-bold ${calculatedStock < 0 ? 'text-destructive' : 'text-foreground'}`}>
              {calculatedStock} unit
            </span>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="adjustment-note">Alasan Penyesuaian *</Label>
            <Input
              id="adjustment-note"
              placeholder="Contoh: Restok supplier batch #12 atau Kerusakan barang"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              disabled={isSubmitting}
            />
            <p className="text-[11px] text-muted-foreground">
              Alasan wajib diisi untuk catatan riwayat pergerakan stok (audit trail).
            </p>
          </div>

          <div className="mt-6 flex justify-end gap-2 border-t pt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Batal
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting || (adjustmentType === 'subtract' && amount > currentStock)}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Menyimpan...
                </>
              ) : (
                'Simpan Penyesuaian'
              )}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
