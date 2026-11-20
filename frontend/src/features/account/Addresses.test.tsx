import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult } from '@tanstack/react-query';
import { AddressesPage } from './pages/AddressesPage';
import { AddressCard } from './components/AddressCard';
import { AddressFormDialog } from './components/AddressFormDialog';
import { Address, ShippingRate } from '@/types';
import * as addressesApiModule from './api/addressesApi';

const mockProvinces: ShippingRate[] = [
  {
    provinceCode: 'DKI',
    provinceName: 'DKI Jakarta',
    baseFee: 10000,
    perKgFee: 5000,
    estimatedDaysMin: 1,
    estimatedDaysMax: 2,
    isActive: true,
  },
  {
    provinceCode: 'JBR',
    provinceName: 'Jawa Barat',
    baseFee: 12000,
    perKgFee: 6000,
    estimatedDaysMin: 2,
    estimatedDaysMax: 3,
    isActive: true,
  },
];

const mockAddress: Address = {
  id: 'addr-1',
  userId: 'user-1',
  label: 'Rumah',
  recipientName: 'Budi Santoso',
  phone: '08123456789',
  provinceCode: 'DKI',
  provinceName: 'DKI Jakarta',
  city: 'Jakarta Selatan',
  district: 'Tebet',
  postalCode: '12810',
  street: 'Jl. Tebet Barat No. 10',
  isDefault: true,
  createdAt: '2026-11-20T00:00:00Z',
};

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

describe('Addresses Feature (T10-02)', () => {
  it('AddressCard displays address details and default badge', () => {
    const onEdit = vi.fn();
    const onDelete = vi.fn();
    const onSetDefault = vi.fn();

    render(
      <AddressCard
        address={mockAddress}
        onEdit={onEdit}
        onDelete={onDelete}
        onSetDefault={onSetDefault}
      />
    );

    expect(screen.getByText('Rumah')).toBeInTheDocument();
    expect(screen.getByText('Utama')).toBeInTheDocument();
    expect(screen.getByText('Budi Santoso')).toBeInTheDocument();
    expect(screen.getByText(/Jl. Tebet Barat No. 10/i)).toBeInTheDocument();
  });

  it('AddressFormDialog validates required fields using schema', async () => {
    const onSubmit = vi.fn();
    const onClose = vi.fn();

    render(
      <AddressFormDialog
        isOpen={true}
        onClose={onClose}
        onSubmit={onSubmit}
        provinces={mockProvinces}
      />
    );

    expect(screen.getByText('Tambah Alamat Pengiriman Baru')).toBeInTheDocument();

    const user = userEvent.setup();
    // Clear recipientName and submit to trigger validation
    const submitBtn = screen.getByRole('button', { name: 'Tambah Alamat' });
    await user.click(submitBtn);

    expect(await screen.findByText('Nama penerima wajib diisi.')).toBeInTheDocument();
    expect(screen.getByText('Nomor telepon wajib diisi.')).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('AddressesPage renders address cards and opens add modal', async () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(addressesApiModule, 'useAddresses').mockReturnValue({
      data: [mockAddress],
      isLoading: false,
    } as unknown as UseQueryResult<Address[], Error>);

    vi.spyOn(addressesApiModule, 'useShippingProvinces').mockReturnValue({
      data: mockProvinces,
      isLoading: false,
    } as unknown as UseQueryResult<ShippingRate[], Error>);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AddressesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Buku Alamat Pengiriman')).toBeInTheDocument();
    expect(screen.getByText('Budi Santoso')).toBeInTheDocument();

    const user = userEvent.setup();
    const addBtn = screen.getByRole('button', { name: /Tambah Alamat Baru/i });
    await user.click(addBtn);

    expect(screen.getByText('Tambah Alamat Pengiriman Baru')).toBeInTheDocument();
  });
});
