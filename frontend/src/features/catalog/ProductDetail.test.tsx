import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ProductDetailPage } from './pages/ProductDetailPage';
import { Product } from '@/types';
import * as catalogApiModule from './api';
import * as cartApiModule from '@/features/cart/api';
import { useAuthStore } from '@/stores/authStore';

const detailedProduct: Product = {
  id: 'prod-123',
  categoryId: 'cat-1',
  categoryName: 'Sepatu Lari',
  categorySlug: 'sepatu-lari',
  brandId: 'brand-1',
  brandName: 'Nike',
  brandSlug: 'nike',
  name: 'Nike Zoom Fly 5',
  slug: 'nike-zoom-fly-5',
  description: 'Sepatu lari jarak jauh dengan pelat serat karbon.',
  basePrice: 2499000,
  isActive: true,
  inStock: true,
  images: [
    {
      id: 'img-1',
      productId: 'prod-123',
      url: 'https://example.com/zoomfly.jpg',
      altText: 'Nike Zoom Fly Front',
      sortOrder: 1,
      isPrimary: true,
    },
  ],
  variants: [
    {
      id: 'var-1',
      productId: 'prod-123',
      sku: 'ZF5-41',
      optionSize: '41',
      optionColor: 'Putih',
      price: 2499000,
      weightGram: 310,
      stockQuantity: 3,
      isActive: true,
    },
    {
      id: 'var-2',
      productId: 'prod-123',
      sku: 'ZF5-42',
      optionSize: '42',
      optionColor: 'Putih',
      price: 2499000,
      weightGram: 320,
      stockQuantity: 0, // Disabled
      isActive: true,
    },
  ],
};

function renderDetailPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/produk/nike-zoom-fly-5']}>
        <Routes>
          <Route path="/produk/:slug" element={<ProductDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ProductDetailPage Component (T9-05)', () => {
  beforeEach(() => {
    vi.spyOn(catalogApiModule, 'useProductBySlug').mockReturnValue({
      data: detailedProduct,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as unknown as ReturnType<typeof catalogApiModule.useProductBySlug>);
  });

  it('renders product information, gallery, and disabled state for out-of-stock variant', () => {
    renderDetailPage();

    expect(screen.getByRole('heading', { level: 1, name: 'Nike Zoom Fly 5' })).toBeInTheDocument();
    expect(screen.getByText(/Sepatu lari jarak jauh/i)).toBeInTheDocument();

    // Option 42 has stock 0 -> disabled
    const opt42 = screen.getByRole('button', { name: /42/i });
    expect(opt42).toBeDisabled();

    // Add to cart button exists
    expect(
      screen.getByRole('button', { name: /Tambah ke Keranjang/i }),
    ).toBeInTheDocument();
  });

  it('quantity stepper increments and decrements within stock limit', async () => {
    const user = userEvent.setup();
    renderDetailPage();

    const decBtn = screen.getByRole('button', { name: /Kurangi jumlah/i });
    const incBtn = screen.getByRole('button', { name: /Tambah jumlah/i });

    // Stock for selected variant var-1 is 3
    expect(decBtn).toBeDisabled(); // Already at 1

    await user.click(incBtn);
    expect(screen.getByText('2')).toBeInTheDocument();

    await user.click(incBtn);
    expect(screen.getByText('3')).toBeInTheDocument();

    // Reached maxStock 3 -> incBtn disabled
    expect(incBtn).toBeDisabled();

    await user.click(decBtn);
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('adds item to cart when authenticated', async () => {
    const user = userEvent.setup();
    useAuthStore.setState({
      isAuthenticated: true,
      user: { id: 'u1', email: 'test@mail.com', fullName: 'Tester', role: 'Customer' },
      token: 'jwt-token',
    });

    const mutateAsyncMock = vi.fn().mockResolvedValue({});
    vi.spyOn(cartApiModule, 'useAddToCart').mockReturnValue({
      mutateAsync: mutateAsyncMock,
      isPending: false,
    } as unknown as ReturnType<typeof cartApiModule.useAddToCart>);

    renderDetailPage();

    const addToCartBtn = screen.getByRole('button', { name: /Tambah ke Keranjang/i });
    await user.click(addToCartBtn);

    expect(mutateAsyncMock).toHaveBeenCalledWith({
      variantId: 'var-1',
      quantity: 1,
    });
  });
});
