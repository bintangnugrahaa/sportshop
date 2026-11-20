import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { X, Loader2 } from 'lucide-react';
import { addressSchema, AddressFormData } from '../schemas/addressSchema';
import { Address, ShippingRate } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface AddressFormDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: AddressFormData) => Promise<void>;
  initialData?: Address | null;
  provinces: ShippingRate[];
  isSubmitting?: boolean;
}

export const AddressFormDialog: React.FC<AddressFormDialogProps> = ({
  isOpen,
  onClose,
  onSubmit,
  initialData,
  provinces,
  isSubmitting = false,
}) => {
  const isEditing = Boolean(initialData);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AddressFormData>({
    resolver: zodResolver(addressSchema),
    defaultValues: {
      label: initialData?.label ?? 'Rumah',
      recipientName: initialData?.recipientName ?? '',
      phone: initialData?.phone ?? '',
      provinceCode: initialData?.provinceCode ?? '',
      city: initialData?.city ?? '',
      district: initialData?.district ?? '',
      postalCode: initialData?.postalCode ?? '',
      street: initialData?.street ?? '',
      isDefault: initialData?.isDefault ?? false,
    },
  });

  React.useEffect(() => {
    if (initialData) {
      reset({
        label: initialData.label,
        recipientName: initialData.recipientName,
        phone: initialData.phone,
        provinceCode: initialData.provinceCode,
        city: initialData.city,
        district: initialData.district ?? '',
        postalCode: initialData.postalCode,
        street: initialData.street,
        isDefault: initialData.isDefault,
      });
    } else {
      reset({
        label: 'Rumah',
        recipientName: '',
        phone: '',
        provinceCode: provinces[0]?.provinceCode ?? '',
        city: '',
        district: '',
        postalCode: '',
        street: '',
        isDefault: false,
      });
    }
  }, [initialData, provinces, reset]);

  if (!isOpen) return null;

  const onFormSubmit = async (data: AddressFormData) => {
    await onSubmit(data);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4" role="dialog" aria-modal="true">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/60 backdrop-blur-sm animate-in fade-in"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Modal Dialog Content */}
      <div className="relative z-50 w-full max-w-lg rounded-xl border bg-background p-6 shadow-2xl animate-in zoom-in-95 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between border-b pb-3">
          <h2 className="text-lg font-bold text-foreground">
            {isEditing ? 'Ubah Alamat Pengiriman' : 'Tambah Alamat Pengiriman Baru'}
          </h2>
          <Button
            variant="ghost"
            size="sm"
            onClick={onClose}
            className="h-8 w-8 p-0"
            aria-label="Tutup form alamat"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        <form onSubmit={handleSubmit(onFormSubmit)} className="mt-4 space-y-4">
          {/* Label & Recipient Name */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="label">Label Alamat *</Label>
              <Input
                id="label"
                placeholder="Contoh: Rumah, Kantor"
                {...register('label')}
                aria-invalid={Boolean(errors.label)}
              />
              {errors.label && (
                <p className="text-xs text-destructive">{errors.label.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="recipientName">Nama Penerima *</Label>
              <Input
                id="recipientName"
                placeholder="Nama lengkap penerima"
                {...register('recipientName')}
                aria-invalid={Boolean(errors.recipientName)}
              />
              {errors.recipientName && (
                <p className="text-xs text-destructive">{errors.recipientName.message}</p>
              )}
            </div>
          </div>

          {/* Phone */}
          <div className="space-y-1.5">
            <Label htmlFor="phone">Nomor Telepon *</Label>
            <Input
              id="phone"
              placeholder="08123456789"
              {...register('phone')}
              aria-invalid={Boolean(errors.phone)}
            />
            {errors.phone && (
              <p className="text-xs text-destructive">{errors.phone.message}</p>
            )}
          </div>

          {/* Province selection & City */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="provinceCode">Provinsi *</Label>
              <select
                id="provinceCode"
                {...register('provinceCode')}
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              >
                <option value="">Pilih Provinsi</option>
                {provinces.map((p) => (
                  <option key={p.provinceCode} value={p.provinceCode}>
                    {p.provinceName}
                  </option>
                ))}
              </select>
              {errors.provinceCode && (
                <p className="text-xs text-destructive">{errors.provinceCode.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="city">Kota / Kabupaten *</Label>
              <Input
                id="city"
                placeholder="Contoh: Jakarta Selatan"
                {...register('city')}
                aria-invalid={Boolean(errors.city)}
              />
              {errors.city && (
                <p className="text-xs text-destructive">{errors.city.message}</p>
              )}
            </div>
          </div>

          {/* District & Postal Code */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="district">Kecamatan (opsional)</Label>
              <Input
                id="district"
                placeholder="Contoh: Tebet"
                {...register('district')}
                aria-invalid={Boolean(errors.district)}
              />
              {errors.district && (
                <p className="text-xs text-destructive">{errors.district.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="postalCode">Kode Pos *</Label>
              <Input
                id="postalCode"
                placeholder="12810"
                {...register('postalCode')}
                aria-invalid={Boolean(errors.postalCode)}
              />
              {errors.postalCode && (
                <p className="text-xs text-destructive">{errors.postalCode.message}</p>
              )}
            </div>
          </div>

          {/* Street */}
          <div className="space-y-1.5">
            <Label htmlFor="street">Alamat Lengkap (Jalan, RT/RW, No. Rumah) *</Label>
            <textarea
              id="street"
              rows={3}
              placeholder="Jl. Tebet Raya No. 12, RT 01/RW 02..."
              {...register('street')}
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            />
            {errors.street && (
              <p className="text-xs text-destructive">{errors.street.message}</p>
            )}
          </div>

          {/* Default Address Checkbox */}
          <div className="flex items-center gap-2 pt-1">
            <input
              type="checkbox"
              id="isDefault"
              {...register('isDefault')}
              className="h-4 w-4 rounded border-input text-primary focus:ring-primary"
            />
            <Label htmlFor="isDefault" className="text-xs font-normal cursor-pointer">
              Jadikan alamat ini sebagai alamat pengiriman utama
            </Label>
          </div>

          {/* Buttons */}
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              Batal
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isEditing ? 'Simpan Perubahan' : 'Tambah Alamat'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
