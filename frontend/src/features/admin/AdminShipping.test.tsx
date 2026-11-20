import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult, UseMutationResult } from '@tanstack/react-query';
import { AdminShippingRatesPage } from './shipping/pages/AdminShippingRatesPage';
import * as adminShippingRatesApiModule from './shipping/api/adminShippingRatesApi';

vi.mock('@/lib/toast', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}));

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

const mockShippingRates: adminShippingRatesApiModule.ShippingRate[] = [
  {
    provinceCode: '31',
    provinceName: 'DKI Jakarta',
    baseFee: 15000,
    perKgFee: 8000,
    estimatedDaysMin: 1,
    estimatedDaysMax: 2,
    isActive: true,
    updatedAt: '2026-11-20T10:00:00Z',
  },
  {
    provinceCode: '32',
    provinceName: 'Jawa Barat',
    baseFee: 20000,
    perKgFee: 10000,
    estimatedDaysMin: 2,
    estimatedDaysMax: 4,
    isActive: true,
    updatedAt: '2026-11-20T10:00:00Z',
  },
  {
    provinceCode: '91',
    provinceName: 'Papua Barat',
    baseFee: 65000,
    perKgFee: 40000,
    estimatedDaysMin: 5,
    estimatedDaysMax: 8,
    isActive: false,
    updatedAt: '2026-11-20T10:00:00Z',
  },
];

describe('Admin Shipping Rates Management (T10-11)', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    vi.spyOn(adminShippingRatesApiModule, 'useAdminShippingRates').mockReturnValue({
      data: mockShippingRates,
      isLoading: false,
      refetch: vi.fn(),
    } as unknown as UseQueryResult<adminShippingRatesApiModule.ShippingRate[], Error>);
  });

  it('renders shipping rates list and handles search filter', async () => {
    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminShippingRatesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(await screen.findByText('Tarif Ongkos Kirim')).toBeInTheDocument();
    expect(screen.getByText('DKI Jakarta')).toBeInTheDocument();
    expect(screen.getByText('Jawa Barat')).toBeInTheDocument();
    expect(screen.getByText('Papua Barat')).toBeInTheDocument();

    // Search for "Jawa"
    const searchInput = screen.getByPlaceholderText(/Cari nama atau kode/i);
    fireEvent.change(searchInput, { target: { value: 'Jawa' } });

    expect(screen.getByText('Jawa Barat')).toBeInTheDocument();
    expect(screen.queryByText('DKI Jakarta')).not.toBeInTheDocument();
  });

  it('filters by status active and inactive', async () => {
    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminShippingRatesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Click "Non-aktif"
    const inactiveTab = screen.getByRole('button', { name: 'Non-aktif' });
    fireEvent.click(inactiveTab);

    expect(screen.getByText('Papua Barat')).toBeInTheDocument();
    expect(screen.queryByText('DKI Jakarta')).not.toBeInTheDocument();
    expect(screen.queryByText('Jawa Barat')).not.toBeInTheDocument();
  });

  it('opens edit dialog and submits updated shipping rate', async () => {
    const mutateAsyncMock = vi.fn().mockResolvedValue({
      provinceCode: '31',
      provinceName: 'DKI Jakarta',
      baseFee: 18000,
      perKgFee: 9000,
      estimatedDaysMin: 1,
      estimatedDaysMax: 3,
      isActive: true,
      updatedAt: '2026-11-20T11:00:00Z',
    });

    vi.spyOn(adminShippingRatesApiModule, 'useUpdateShippingRateMutation').mockReturnValue({
      mutateAsync: mutateAsyncMock,
      isPending: false,
    } as unknown as UseMutationResult<
      adminShippingRatesApiModule.ShippingRate,
      Error,
      { provinceCode: string; input: adminShippingRatesApiModule.UpdateShippingRateInput },
      unknown
    >);

    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminShippingRatesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    const editButtons = screen.getAllByRole('button', { name: /Edit Tarif/i });
    expect(editButtons.length).toBeGreaterThan(0);
    fireEvent.click(editButtons[0]!);

    expect(screen.getByRole('heading', { name: 'Edit Tarif Ongkir' })).toBeInTheDocument();
    expect(screen.getByText('Kode: 31')).toBeInTheDocument();

    // Edit base fee
    const baseFeeInput = screen.getByLabelText(/Tarif Dasar \(Rp\)/i);
    fireEvent.change(baseFeeInput, { target: { value: '18000' } });

    // Submit
    const submitBtn = screen.getByRole('button', { name: 'Simpan Tarif' });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mutateAsyncMock).toHaveBeenCalledWith({
        provinceCode: '31',
        input: {
          baseFee: 18000,
          perKgFee: 8000,
          estimatedDaysMin: 1,
          estimatedDaysMax: 2,
          isActive: true,
        },
      });
    });
  });

  it('validates that max estimated days cannot be less than min days', async () => {
    const mutateAsyncMock = vi.fn();
    vi.spyOn(adminShippingRatesApiModule, 'useUpdateShippingRateMutation').mockReturnValue({
      mutateAsync: mutateAsyncMock,
      isPending: false,
    } as unknown as UseMutationResult<
      adminShippingRatesApiModule.ShippingRate,
      Error,
      { provinceCode: string; input: adminShippingRatesApiModule.UpdateShippingRateInput },
      unknown
    >);

    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminShippingRatesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    const editButtons = screen.getAllByRole('button', { name: /Edit Tarif/i });
    fireEvent.click(editButtons[0]!);

    // Set min to 5 and max to 2
    const minDaysInput = screen.getByLabelText(/Estimasi Min/i);
    fireEvent.change(minDaysInput, { target: { value: '5' } });

    const maxDaysInput = screen.getByLabelText(/Estimasi Max/i);
    fireEvent.change(maxDaysInput, { target: { value: '2' } });

    const submitBtn = screen.getByRole('button', { name: 'Simpan Tarif' });
    fireEvent.click(submitBtn);

    expect(
      await screen.findByText(/Estimasi hari maksimum harus lebih besar atau sama dengan hari minimum/i)
    ).toBeInTheDocument();
    expect(mutateAsyncMock).not.toHaveBeenCalled();
  });
});
