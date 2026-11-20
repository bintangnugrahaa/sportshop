import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult } from '@tanstack/react-query';
import { MyOrdersPage } from './pages/MyOrdersPage';
import { OrderDetailPage } from './pages/OrderDetailPage';
import { Order, PagedResult } from '@/types';
import * as ordersApiModule from './api/ordersApi';

const mockOrder: Order = {
  id: 'order-1',
  orderNumber: 'SPT-20261120-000001',
  userId: 'user-1',
  status: 'PendingPayment',
  subtotal: 1200000,
  shippingFee: 15000,
  total: 1215000,
  shippingAddress: {
    recipientName: 'Budi Santoso',
    phone: '08123456789',
    provinceCode: 'DKI',
    provinceName: 'DKI Jakarta',
    city: 'Jakarta Selatan',
    postalCode: '12810',
    street: 'Jl. Tebet Barat No. 10',
  },
  totalWeightGram: 800,
  expiresAt: '2026-11-21T00:00:00Z',
  createdAt: '2026-11-20T00:00:00Z',
  items: [
    {
      id: 'item-1',
      variantId: 'var-1',
      productName: 'Sepatu Bola Adidas Predator',
      variantLabel: '42 / Hitam',
      sku: 'AD-PRED-42',
      unitPrice: 1200000,
      quantity: 1,
      lineTotal: 1200000,
    },
  ],
  statusHistories: [
    {
      id: 'hist-1',
      toStatus: 'PendingPayment',
      note: 'Pesanan berhasil dibuat, menunggu pembayaran Snap.',
      createdAt: '2026-11-20T00:00:00Z',
    },
  ],
};

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

describe('Customer Orders Feature (T10-05)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('MyOrdersPage renders order list and status filter tabs', () => {
    const queryClient = createTestQueryClient();

    const mockPagedResult: PagedResult<Order> = {
      items: [mockOrder],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };

    vi.spyOn(ordersApiModule, 'useOrders').mockReturnValue({
      data: mockPagedResult,
      isLoading: false,
    } as unknown as UseQueryResult<PagedResult<Order>, Error>);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <MyOrdersPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Pesanan Saya')).toBeInTheDocument();
    expect(screen.getByText('Semua Status')).toBeInTheDocument();
    expect(screen.getByText('SPT-20261120-000001')).toBeInTheDocument();
    expect(screen.getByText('Sepatu Bola Adidas Predator')).toBeInTheDocument();
  });

  it('OrderDetailPage renders items, tracking info, and OrderTimeline', () => {
    const queryClient = createTestQueryClient();

    const shippedOrder: Order = {
      ...mockOrder,
      status: 'Shipped',
      courier: 'JNE Reguler',
      trackingNumber: 'JNE123456789ID',
      statusHistories: [
        ...mockOrder.statusHistories,
        {
          id: 'hist-2',
          toStatus: 'Paid',
          note: 'Pembayaran settlement diterima.',
          createdAt: '2026-11-20T01:00:00Z',
        },
        {
          id: 'hist-3',
          toStatus: 'Shipped',
          note: 'Pesanan telah diserahkan ke kurir.',
          createdAt: '2026-11-20T04:00:00Z',
        },
      ],
    };

    vi.spyOn(ordersApiModule, 'useOrderDetail').mockReturnValue({
      data: shippedOrder,
      isLoading: false,
      error: null,
    } as unknown as UseQueryResult<Order, Error>);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/akun/pesanan/SPT-20261120-000001']}>
          <Routes>
            <Route path="/akun/pesanan/:orderNumber" element={<OrderDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByRole('heading', { name: 'SPT-20261120-000001' })).toBeInTheDocument();
    expect(screen.getByText('JNE123456789ID')).toBeInTheDocument();
    expect(screen.getByText('Pelacakan Status')).toBeInTheDocument();
    expect(screen.getByText('Pesanan Dikirim')).toBeInTheDocument();
  });

  it('allows cancelling a PendingPayment order', async () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(ordersApiModule, 'useOrderDetail').mockReturnValue({
      data: mockOrder,
      isLoading: false,
      error: null,
    } as unknown as UseQueryResult<Order, Error>);

    const cancelMock = vi.spyOn(ordersApiModule.ordersApi, 'cancelOrder').mockResolvedValueOnce({
      ...mockOrder,
      status: 'Cancelled',
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/akun/pesanan/SPT-20261120-000001']}>
          <Routes>
            <Route path="/akun/pesanan/:orderNumber" element={<OrderDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    const user = userEvent.setup();
    const cancelBtn = screen.getByRole('button', { name: /Batalkan Pesanan/i });
    await user.click(cancelBtn);

    // Modal should be visible
    expect(screen.getByText(/Apakah Anda yakin ingin membatalkan pesanan/i)).toBeInTheDocument();

    // Fill reason and confirm
    const reasonInput = screen.getByPlaceholderText(/Contoh: Ingin mengubah ukuran varian/i);
    await user.type(reasonInput, 'Salah pilih ukuran');

    const confirmButtons = screen.getAllByRole('button', { name: /Batalkan Pesanan/i });
    expect(confirmButtons.length).toBeGreaterThan(1);
    await user.click(confirmButtons[1]!);

    expect(cancelMock).toHaveBeenCalledWith('SPT-20261120-000001', 'Salah pilih ukuran');
  });
});
