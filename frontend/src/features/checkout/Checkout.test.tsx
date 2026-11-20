import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult } from '@tanstack/react-query';
import { CheckoutPage } from './pages/CheckoutPage';
import { Cart, Address, ShippingRate, ShippingQuote, Order } from '@/types';
import * as cartApiModule from '@/features/cart/api';
import * as addressesApiModule from '@/features/account/api/addressesApi';
import * as checkoutApiModule from './api/checkoutApi';

const mockCart: Cart = {
  id: 'cart-1',
  userId: 'user-1',
  items: [
    {
      id: 'item-1',
      variantId: 'var-1',
      productId: 'prod-1',
      productName: 'Sepatu Bola Adidas Predator',
      productSlug: 'sepatu-bola-adidas-predator',
      sku: 'AD-PRED-42',
      optionSize: '42',
      optionColor: 'Hitam',
      unitPrice: 1200000,
      priceAtAdd: 1200000,
      quantity: 1,
      lineTotal: 1200000,
      weightGram: 800,
      totalWeightGram: 800,
      stockQuantity: 10,
      isActive: true,
      isPriceChanged: false,
      isStockInsufficient: false,
      isAvailable: true,
    },
  ],
  totalItems: 1,
  totalWeightGram: 800,
  subtotal: 1200000,
  hasIssues: false,
};

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

const mockQuote: ShippingQuote = {
  addressId: 'addr-1',
  recipientName: 'Budi Santoso',
  provinceCode: 'DKI',
  provinceName: 'DKI Jakarta',
  city: 'Jakarta Selatan',
  postalCode: '12810',
  totalWeightGram: 800,
  billableWeightKg: 1,
  baseFee: 10000,
  perKgFee: 5000,
  shippingFee: 15000,
  estimatedDaysMin: 1,
  estimatedDaysMax: 2,
  cartSubtotal: 1200000,
  estimatedTotal: 1215000,
};

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
  items: [],
  statusHistories: [],
  snapToken: 'snap-token-123',
};

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

describe('Checkout Feature (T10-03)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders address selection, items and calculates shipping quote', async () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(cartApiModule, 'useCart').mockReturnValue({
      data: mockCart,
      isLoading: false,
    } as unknown as UseQueryResult<Cart, Error>);

    vi.spyOn(addressesApiModule, 'useAddresses').mockReturnValue({
      data: [mockAddress],
      isLoading: false,
    } as unknown as UseQueryResult<Address[], Error>);

    vi.spyOn(addressesApiModule, 'useShippingProvinces').mockReturnValue({
      data: [],
      isLoading: false,
    } as unknown as UseQueryResult<ShippingRate[], Error>);

    vi.spyOn(checkoutApiModule, 'useShippingQuote').mockReturnValue({
      data: mockQuote,
      isLoading: false,
      error: null,
    } as unknown as UseQueryResult<ShippingQuote, Error>);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <CheckoutPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Checkout Pembayaran')).toBeInTheDocument();
    expect(screen.getByText('Budi Santoso')).toBeInTheDocument();
    expect(screen.getByText('Sepatu Bola Adidas Predator')).toBeInTheDocument();
    expect(screen.getByText('Ringkasan Pembayaran')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Buat Pesanan & Bayar' })).not.toBeDisabled();
  });

  it('prevents double click checkout and triggers Midtrans Snap', async () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(cartApiModule, 'useCart').mockReturnValue({
      data: mockCart,
      isLoading: false,
    } as unknown as UseQueryResult<Cart, Error>);

    vi.spyOn(addressesApiModule, 'useAddresses').mockReturnValue({
      data: [mockAddress],
      isLoading: false,
    } as unknown as UseQueryResult<Address[], Error>);

    vi.spyOn(addressesApiModule, 'useShippingProvinces').mockReturnValue({
      data: [],
      isLoading: false,
    } as unknown as UseQueryResult<ShippingRate[], Error>);

    vi.spyOn(checkoutApiModule, 'useShippingQuote').mockReturnValue({
      data: mockQuote,
      isLoading: false,
      error: null,
    } as unknown as UseQueryResult<ShippingQuote, Error>);

    const snapPayMock = vi.fn();
    window.snap = {
      pay: snapPayMock,
    };

    let resolveCheckout: (value: Order) => void;
    const checkoutPromise = new Promise<Order>((res) => {
      resolveCheckout = res;
    });

    const checkoutMock = vi.spyOn(checkoutApiModule.checkoutApi, 'checkout').mockImplementation(() => checkoutPromise);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <CheckoutPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    const user = userEvent.setup();
    const submitBtn = screen.getByTestId('checkout-submit-btn');

    // Double click the button
    await user.click(submitBtn);
    await user.click(submitBtn);

    // Only called once!
    expect(checkoutMock).toHaveBeenCalledTimes(1);
    expect(screen.getByText('Memproses Pesanan...')).toBeInTheDocument();
    expect(submitBtn).toBeDisabled();

    // Resolve the checkout
    resolveCheckout!(mockOrder);

    await waitFor(() => {
      expect(snapPayMock).toHaveBeenCalledWith('snap-token-123', expect.any(Object));
    });
  });
});
