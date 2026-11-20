import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult } from '@tanstack/react-query';
import { PaymentStatusPage } from './pages/PaymentStatusPage';
import { Order } from '@/types';
import * as checkoutApiModule from './api/checkoutApi';

const mockOrderPending: Order = {
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
  items: [],
  statusHistories: [],
  snapToken: 'snap-token-123',
};

const mockOrderPaid: Order = {
  ...mockOrderPending,
  status: 'Paid',
  paidAt: '2026-11-20T00:05:00Z',
};

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

describe('Payment Status Feature (T10-04)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders pending status and allows clicking Bayar Sekarang', async () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(checkoutApiModule, 'useOrder').mockReturnValue({
      data: mockOrderPending,
      isLoading: false,
      isRefetching: false,
      refetch: vi.fn(),
      error: null,
    } as unknown as UseQueryResult<Order, Error>);

    const snapPayMock = vi.fn();
    window.snap = { pay: snapPayMock };

    vi.spyOn(checkoutApiModule.checkoutApi, 'payOrder').mockResolvedValueOnce({
      orderNumber: 'SPT-20261120-000001',
      snapToken: 'new-snap-token-456',
      snapRedirectUrl: null,
      paymentStatus: 'pending',
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/checkout/status/SPT-20261120-000001']}>
          <Routes>
            <Route path="/checkout/status/:orderNumber" element={<PaymentStatusPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByRole('heading', { name: 'Menunggu Pembayaran' })).toBeInTheDocument();
    expect(screen.getByText('SPT-20261120-000001')).toBeInTheDocument();

    const payBtn = screen.getByTestId('pay-now-btn');
    expect(payBtn).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(payBtn);

    expect(checkoutApiModule.checkoutApi.payOrder).toHaveBeenCalledWith('SPT-20261120-000001');
    expect(snapPayMock).toHaveBeenCalledWith('new-snap-token-456', expect.any(Object));
  });

  it('automatically displays Pembayaran Berhasil when status transitions to Paid', () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(checkoutApiModule, 'useOrder').mockReturnValue({
      data: mockOrderPaid,
      isLoading: false,
      isRefetching: false,
      refetch: vi.fn(),
      error: null,
    } as unknown as UseQueryResult<Order, Error>);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/checkout/status/SPT-20261120-000001']}>
          <Routes>
            <Route path="/checkout/status/:orderNumber" element={<PaymentStatusPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Pembayaran Berhasil!')).toBeInTheDocument();
    expect(screen.queryByTestId('pay-now-btn')).not.toBeInTheDocument();
    expect(screen.getByText('Lihat Detail Pesanan')).toBeInTheDocument();
  });
});
