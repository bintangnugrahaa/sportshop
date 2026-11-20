import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { HomePage } from './pages/HomePage';
import * as catalogApiModule from './api';
import { Product, PagedResult } from '@/types';

const mockHomeProduct: Product = {
  id: 'hp-1',
  categoryId: 'c1',
  categoryName: 'Sepatu',
  categorySlug: 'sepatu',
  brandId: 'b1',
  brandName: 'Adidas',
  brandSlug: 'adidas',
  name: 'Adidas Ultraboost Light',
  slug: 'adidas-ultraboost-light',
  description: 'Sepatu lari ultraringan dengan bantalan boost.',
  basePrice: 2800000,
  isActive: true,
  inStock: true,
};

function renderHomePage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('HomePage Component (T9-06)', () => {
  beforeEach(() => {
    vi.spyOn(catalogApiModule, 'useCategories').mockReturnValue({
      data: [
        { id: 'cat-1', name: 'Sepatu Olahraga', slug: 'sepatu', sortOrder: 1, isActive: true },
        { id: 'cat-2', name: 'Jersey Tim', slug: 'jersey', sortOrder: 2, isActive: true },
      ],
      isLoading: false,
      isError: false,
    } as unknown as ReturnType<typeof catalogApiModule.useCategories>);

    vi.spyOn(catalogApiModule, 'useBrands').mockReturnValue({
      data: [
        { id: 'b-1', name: 'Adidas', slug: 'adidas', isActive: true },
        { id: 'b-2', name: 'Puma', slug: 'puma', isActive: true },
      ],
      isLoading: false,
      isError: false,
    } as unknown as ReturnType<typeof catalogApiModule.useBrands>);

    const mockPagedResult: PagedResult<Product> = {
      items: [mockHomeProduct],
      page: 1,
      pageSize: 8,
      totalItems: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };

    vi.spyOn(catalogApiModule, 'useProducts').mockReturnValue({
      data: mockPagedResult,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as unknown as ReturnType<typeof catalogApiModule.useProducts>);
  });

  it('renders hero title, value props, categories, and latest products', () => {
    renderHomePage();

    // Hero title
    expect(
      screen.getByRole('heading', { level: 1, name: /Tingkatkan Performa Olahraga Anda/i }),
    ).toBeInTheDocument();

    // Value propositions
    expect(screen.getByText('100% Original')).toBeInTheDocument();
    expect(screen.getByText('Ongkir Flat Terjangkau')).toBeInTheDocument();
    expect(screen.getByText('Pembayaran Instan')).toBeInTheDocument();

    // Featured Categories
    expect(screen.getByText('Kategori Unggulan')).toBeInTheDocument();
    expect(screen.getByText('Sepatu Olahraga')).toBeInTheDocument();
    expect(screen.getByText('Jersey Tim')).toBeInTheDocument();

    // Latest Products
    expect(screen.getByRole('heading', { level: 2, name: 'Produk Terbaru' })).toBeInTheDocument();
    expect(screen.getByText('Adidas Ultraboost Light')).toBeInTheDocument();

    // Featured Brands
    expect(screen.getByText('Brand Olahraga Resmi')).toBeInTheDocument();
    expect(screen.getByText('Puma')).toBeInTheDocument();
  });
});
