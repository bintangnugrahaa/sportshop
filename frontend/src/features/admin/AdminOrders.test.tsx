import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult, UseMutationResult } from '@tanstack/react-query';
import { AdminOrdersPage } from './orders/pages/AdminOrdersPage';
import { AdminOrderDetailPage } from './orders/pages/AdminOrderDetailPage';
import * as adminOrdersApiModule from './orders/api/adminOrdersApi';
import type { Order, PagedResult } from '@/types';

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

const mockOrderPaid: Order = {
  id: 'ord-1',
  orderNumber: 'ORD-PAID-001',
  userId: 'user-1',
  status: 'Paid',
  subtotal: 1000000,
  shippingFee: 50000,
  total: 1050000,
  totalWeightGram: 1000,
  shippingAddress: {
    recipientName: 'Budi Santoso',
    phone: '08123456789',
    provinceCode: '31',
    provinceName: 'DKI Jakarta',
    city: 'Jakarta Selatan',
    postalCode: '12190',
    street: 'Jl. Sudirman No. 45',
  },
  expiresAt: '2026-11-21T10:00:00Z',
  createdAt: '2026-11-20T10:00:00Z',
  items: [
    {
      id: 'item-1',
      variantId: 'var-1',
      productName: 'Sepatu Running Pro',
      variantLabel: 'Merah / 42',
      sku: 'RUN-RED-42',
      unitPrice: 500000,
      quantity: 2,
      lineTotal: 1000000,
    },
  ],
  statusHistories: [
    {
      id: 'hist-1',
      toStatus: 'Paid',
      createdAt: '2026-11-20T10:05:00Z',
      note: 'Pembayaran sukses',
    },
  ],
};

const mockOrderProcessing: Order = {
  ...mockOrderPaid,
  id: 'ord-2',
  orderNumber: 'ORD-PROC-002',
  status: 'Processing',
};

const mockOrderShipped: Order = {
  ...mockOrderPaid,
  id: 'ord-3',
  orderNumber: 'ORD-SHIP-003',
  status: 'Shipped',
  courier: 'JNE',
  trackingNumber: 'SOCAG0123456789',
};

const mockOrderCompleted: Order = {
  ...mockOrderPaid,
  id: 'ord-4',
  orderNumber: 'ORD-COMP-004',
  status: 'Completed',
  courier: 'JNE',
  trackingNumber: 'SOCAG0123456789',
};

const mockPagedOrders: PagedResult<Order> = {
  items: [mockOrderPaid, mockOrderProcessing, mockOrderShipped],
  page: 1,
  pageSize: 15,
  totalItems: 3,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

describe('Admin Orders Management (T10-10)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('AdminOrdersPage renders order list and handles status tab change', async () => {
    vi.spyOn(adminOrdersApiModule, 'useAdminOrders').mockReturnValue({
      data: mockPagedOrders,
      isLoading: false,
      refetch: vi.fn(),
    } as unknown as UseQueryResult<PagedResult<Order>, Error>);

    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminOrdersPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(await screen.findByText('Daftar Pesanan')).toBeInTheDocument();
    expect(screen.getByText('ORD-PAID-001')).toBeInTheDocument();
    expect(screen.getByText('ORD-PROC-002')).toBeInTheDocument();
    expect(screen.getByText('ORD-SHIP-003')).toBeInTheDocument();
    expect(screen.getAllByText('Budi Santoso').length).toBe(3);

    // Click filter tab "Diproses"
    const processingTab = screen.getByRole('button', { name: 'Diproses' });
    fireEvent.click(processingTab);
    expect(processingTab).toHaveClass('bg-primary');
  });

  it('AdminOrderDetailPage shows correct actions for Paid order and handles Process action', async () => {
    vi.spyOn(adminOrdersApiModule, 'useAdminOrderDetail').mockReturnValue({
      data: mockOrderPaid,
      isLoading: false,
    } as unknown as UseQueryResult<Order, Error>);

    const processMutateMock = vi.fn().mockResolvedValue({
      ...mockOrderPaid,
      status: 'Processing',
    });

    vi.spyOn(adminOrdersApiModule, 'useAdminProcessOrderMutation').mockReturnValue({
      mutateAsync: processMutateMock,
      isPending: false,
    } as unknown as UseMutationResult<Order, Error, { orderNumber: string; input?: adminOrdersApiModule.AdminProcessOrderInput }, unknown>);

    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/admin/pesanan/ORD-PAID-001']}>
          <Routes>
            <Route path="/admin/pesanan/:orderNumber" element={<AdminOrderDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Paid actions: "Proses Pesanan" & "Batalkan Pesanan" must exist
    expect(screen.getByRole('button', { name: /Proses Pesanan/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Batalkan Pesanan/i })).toBeInTheDocument();

    // Shipped / Completed actions must NOT exist
    expect(screen.queryByRole('button', { name: /Kirim Pesanan/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Selesaikan Pesanan/i })).not.toBeInTheDocument();

    // Click "Proses Pesanan"
    fireEvent.click(screen.getByRole('button', { name: /Proses Pesanan/i }));
    await waitFor(() => {
      expect(processMutateMock).toHaveBeenCalledWith({ orderNumber: 'ORD-PAID-001' });
    });
  });

  it('AdminOrderDetailPage opens ShipOrderDialog for Processing order and sends courier & tracking', async () => {
    vi.spyOn(adminOrdersApiModule, 'useAdminOrderDetail').mockReturnValue({
      data: mockOrderProcessing,
      isLoading: false,
    } as unknown as UseQueryResult<Order, Error>);

    const shipMutateMock = vi.fn().mockResolvedValue({
      ...mockOrderProcessing,
      status: 'Shipped',
      courier: 'JNE',
      trackingNumber: 'SOCAG0123456789',
    });

    vi.spyOn(adminOrdersApiModule, 'useAdminShipOrderMutation').mockReturnValue({
      mutateAsync: shipMutateMock,
      isPending: false,
    } as unknown as UseMutationResult<Order, Error, { orderNumber: string; input: adminOrdersApiModule.AdminShipOrderInput }, unknown>);

    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/admin/pesanan/ORD-PROC-002']}>
          <Routes>
            <Route path="/admin/pesanan/:orderNumber" element={<AdminOrderDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Processing actions: "Kirim Pesanan (Input Resi)" & "Batalkan Pesanan" must exist
    expect(screen.getByRole('button', { name: /Kirim Pesanan \(Input Resi\)/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Batalkan Pesanan/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Proses Pesanan/i })).not.toBeInTheDocument();

    // Click "Kirim Pesanan" to open dialog
    fireEvent.click(screen.getByRole('button', { name: /Kirim Pesanan \(Input Resi\)/i }));

    expect(screen.getByRole('heading', { name: /Kirim Pesanan & Input Resi/i })).toBeInTheDocument();

    // Input tracking number
    const trackingInput = screen.getByLabelText(/Nomor Resi Pengiriman/i);
    fireEvent.change(trackingInput, { target: { value: 'SOCAG0123456789' } });

    // Submit ship form
    const submitBtn = screen.getByRole('button', { name: 'Kirim Pesanan' });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(shipMutateMock).toHaveBeenCalledWith({
        orderNumber: 'ORD-PROC-002',
        input: {
          courier: 'JNE',
          trackingNumber: 'SOCAG0123456789',
          note: undefined,
        },
      });
    });
  });

  it('AdminOrderDetailPage does NOT render action buttons for Completed order', async () => {
    vi.spyOn(adminOrdersApiModule, 'useAdminOrderDetail').mockReturnValue({
      data: mockOrderCompleted,
      isLoading: false,
    } as unknown as UseQueryResult<Order, Error>);

    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/admin/pesanan/ORD-COMP-004']}>
          <Routes>
            <Route path="/admin/pesanan/:orderNumber" element={<AdminOrderDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    // No mutation buttons should exist on completed order
    expect(screen.queryByRole('button', { name: /Proses Pesanan/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Kirim Pesanan/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Selesaikan Pesanan/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Batalkan Pesanan/i })).not.toBeInTheDocument();
  });
});
