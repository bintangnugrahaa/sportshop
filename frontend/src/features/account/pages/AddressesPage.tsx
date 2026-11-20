import React from 'react';
import { Plus, MapPin, Loader2 } from 'lucide-react';
import {
  useAddresses,
  useShippingProvinces,
  useCreateAddress,
  useUpdateAddress,
  useDeleteAddress,
  useSetDefaultAddress,
} from '../api/addressesApi';
import { AddressCard } from '../components/AddressCard';
import { AddressFormDialog } from '../components/AddressFormDialog';
import { Address } from '@/types';
import { AddressFormData } from '../schemas/addressSchema';
import { Button } from '@/components/ui/button';
import { toast } from '@/lib/toast';

export const AddressesPage: React.FC = () => {
  const { data: addresses, isLoading: isAddressesLoading } = useAddresses();
  const { data: provinces = [] } = useShippingProvinces();

  const [isFormOpen, setIsFormOpen] = React.useState(false);
  const [selectedAddress, setSelectedAddress] = React.useState<Address | null>(null);

  const createMutation = useCreateAddress();
  const updateMutation = useUpdateAddress();
  const deleteMutation = useDeleteAddress();
  const setDefaultMutation = useSetDefaultAddress();

  const handleOpenCreate = () => {
    setSelectedAddress(null);
    setIsFormOpen(true);
  };

  const handleOpenEdit = (address: Address) => {
    setSelectedAddress(address);
    setIsFormOpen(true);
  };

  const handleCloseForm = () => {
    setIsFormOpen(false);
    setSelectedAddress(null);
  };

  const handleFormSubmit = async (formData: AddressFormData) => {
    try {
      if (selectedAddress) {
        await updateMutation.mutateAsync({
          id: selectedAddress.id,
          data: formData,
        });
        toast.success('Alamat berhasil diperbarui.');
      } else {
        await createMutation.mutateAsync(formData);
        toast.success('Alamat baru berhasil ditambahkan.');
      }
      handleCloseForm();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal menyimpan alamat.';
      toast.error(msg);
    }
  };

  const handleDelete = async (id: string) => {
    if (window.confirm('Apakah Anda yakin ingin menghapus alamat ini?')) {
      try {
        await deleteMutation.mutateAsync(id);
        toast.success('Alamat berhasil dihapus.');
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : 'Gagal menghapus alamat.';
        toast.error(msg);
      }
    }
  };

  const handleSetDefault = async (id: string) => {
    try {
      await setDefaultMutation.mutateAsync(id);
      toast.success('Alamat utama berhasil diperbarui.');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal mengubah alamat utama.';
      toast.error(msg);
    }
  };

  const isActionLoading =
    createMutation.isPending ||
    updateMutation.isPending ||
    deleteMutation.isPending ||
    setDefaultMutation.isPending;

  return (
    <div className="container mx-auto px-4 py-8 sm:px-6 lg:px-8">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground">
            Buku Alamat Pengiriman
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Kelola alamat pengiriman Anda untuk memudahkan proses checkout pesanan.
          </p>
        </div>

        <Button onClick={handleOpenCreate} className="self-start sm:self-auto">
          <Plus className="mr-2 h-4 w-4" />
          Tambah Alamat Baru
        </Button>
      </div>

      {/* Content */}
      {isAddressesLoading ? (
        <div className="flex justify-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </div>
      ) : !addresses || addresses.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center max-w-md mx-auto">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-muted text-muted-foreground mb-4">
            <MapPin className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-semibold text-foreground">Belum Ada Alamat Tersimpan</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Tambahkan alamat rumah, kantor, atau tempat tinggal Anda untuk mulai berbelanja.
          </p>
          <Button onClick={handleOpenCreate} className="mt-6">
            <Plus className="mr-2 h-4 w-4" />
            Tambah Alamat Sekarang
          </Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {addresses.map((address) => (
            <AddressCard
              key={address.id}
              address={address}
              onEdit={handleOpenEdit}
              onDelete={handleDelete}
              onSetDefault={handleSetDefault}
              isActionLoading={isActionLoading}
            />
          ))}
        </div>
      )}

      {/* Form Modal */}
      <AddressFormDialog
        isOpen={isFormOpen}
        onClose={handleCloseForm}
        onSubmit={handleFormSubmit}
        initialData={selectedAddress}
        provinces={provinces}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />
    </div>
  );
};

export default AddressesPage;
