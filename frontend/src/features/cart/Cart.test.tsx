import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult } from '@tanstack/react-query';
import { CartItemCard } from './components/CartItemCard';
import { CartPage } from './pages/CartPage';
import { CartDrawer } from './components/CartDrawer';
import { Cart, CartItem } from '@/types';
import * as cartApiModule from './api';
import { useUiStore } from '@/stores/uiStore';

const mockItem: CartItem = {
  id: 'item-1',
  variantId: 'var-1',
  productId: 'prod-1',
  productName: 'Sepatu Lari Nike Air Zoom',
  productSlug: 'sepatu-lari-nike-air-zoom',
  sku: 'NK-AZ-42',
  optionSize: '42',
  optionColor: 'Hitam',
  unitPrice: 1500000,
  priceAtAdd: 1400000,
  quantity: 2,
  lineTotal: 3000000,
  weightGram: 800,
  totalWeightGram: 1600,
  stockQuantity: 10,
  isActive: true,
  isPriceChanged: true,
  isStockInsufficient: false,
  isAvailable: true,
};

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

describe('Cart Features (T10-01)', () => {
  it('CartItemCard renders warnings for price change and insufficient stock', () => {
    const onUpdate = vi.fn();
    const onRemove = vi.fn();

    const { rerender } = render(
      <MemoryRouter>
        <CartItemCard
          item={mockItem}
          onUpdateQuantity={onUpdate}
          onRemove={onRemove}
        />
      </MemoryRouter>
    );

    expect(screen.getByText('Sepatu Lari Nike Air Zoom')).toBeInTheDocument();
    expect(screen.getByText(/Harga berubah dari/i)).toBeInTheDocument();

    // Rerender with stock insufficient
    const itemWithStockIssue: CartItem = {
      ...mockItem,
      isPriceChanged: false,
      isStockInsufficient: true,
      stockQuantity: 1,
    };

    rerender(
      <MemoryRouter>
        <CartItemCard
          item={itemWithStockIssue}
          onUpdateQuantity={onUpdate}
          onRemove={onRemove}
        />
      </MemoryRouter>
    );

    expect(screen.getByText(/Stok tidak mencukupi! Tersisa hanya 1/i)).toBeInTheDocument();
  });

  it('CartDrawer displays cart items, hasIssues warning and can navigate to checkout', () => {
    useUiStore.setState({ isCartDrawerOpen: true });
    const queryClient = createTestQueryClient();

    const mockCart: Cart = {
      id: 'cart-1',
      userId: 'user-1',
      items: [mockItem],
      totalItems: 2,
      totalWeightGram: 1600,
      subtotal: 3000000,
      hasIssues: true,
    };

    vi.spyOn(cartApiModule, 'useCart').mockReturnValue({
      data: mockCart,
      isLoading: false,
    } as unknown as UseQueryResult<Cart, Error>);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <CartDrawer />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText(/Keranjang \(2\)/i)).toBeInTheDocument();
    expect(
      screen.getByText(/Beberapa item mengalami perubahan harga atau ketersediaan stok/i)
    ).toBeInTheDocument();
    // Checkout button should be disabled when hasIssues is true
    const checkoutBtn = screen.getByRole('button', { name: /Lanjut ke Checkout/i });
    expect(checkoutBtn).toBeDisabled();
  });

  it('CartPage renders items and summary with correct subtotal and weight', () => {
    const queryClient = createTestQueryClient();
    const mockCart: Cart = {
      id: 'cart-1',
      userId: 'user-1',
      items: [{ ...mockItem, isPriceChanged: false }],
      totalItems: 2,
      totalWeightGram: 1600,
      subtotal: 3000000,
      hasIssues: false,
    };

    vi.spyOn(cartApiModule, 'useCart').mockReturnValue({
      data: mockCart,
      isLoading: false,
    } as unknown as UseQueryResult<Cart, Error>);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <CartPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Keranjang Belanja')).toBeInTheDocument();
    expect(screen.getByText('Ringkasan Belanja')).toBeInTheDocument();
    const checkoutBtn = screen.getByRole('button', { name: /Lanjut ke Checkout/i });
    expect(checkoutBtn).not.toBeDisabled();
  });

  it('optimistic update hook rolls back cart state when mutation fails', async () => {
    const queryClient = createTestQueryClient();

    const initialCart: Cart = {
      id: 'cart-1',
      userId: 'user-1',
      items: [{ ...mockItem, quantity: 2, lineTotal: 3000000, totalWeightGram: 1600 }],
      totalItems: 2,
      totalWeightGram: 1600,
      subtotal: 3000000,
      hasIssues: false,
    };

    queryClient.setQueryData(['cart'], initialCart);

    // Mock cartApi.updateItem to reject
    vi.spyOn(cartApiModule.cartApi, 'updateItem').mockRejectedValueOnce(new Error('Server error'));

    let mutateFn: (() => Promise<unknown>) | null = null;
    const TestComponent = () => {
      const mutation = cartApiModule.useUpdateCartItem();
      mutateFn = () => mutation.mutateAsync({ variantId: 'var-1', quantity: 5 });
      return (
        <button
          onClick={() => {
            void mutation.mutateAsync({ variantId: 'var-1', quantity: 5 }).catch(() => {});
          }}
        >
          Update
        </button>
      );
    };

    render(
      <QueryClientProvider client={queryClient}>
        <TestComponent />
      </QueryClientProvider>
    );

    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: 'Update' }));

    // Also directly trigger mutation to ensure completion
    if (mutateFn) {
      const run = mutateFn as () => Promise<unknown>;
      await expect(run()).rejects.toThrow();
    }

    // Cache should be rolled back to initialCart
    const cachedCart = queryClient.getQueryData<Cart>(['cart']);
    expect(cachedCart?.totalItems).toBe(2);
    expect(cachedCart?.items[0]?.quantity).toBe(2);
  });
});
