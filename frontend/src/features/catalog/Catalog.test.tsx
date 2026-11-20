import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClientProvider, QueryClient } from '@tanstack/react-query';
import { ProductCard } from './components/ProductCard';
import { CatalogPage } from './pages/CatalogPage';
import { Product, PagedResult } from '@/types';
import * as catalogApiModule from './api';

const sampleProduct: Product = {
  id: 'p1',
  categoryId: 'c1',
  categoryName: 'Sepatu Lari',
  categorySlug: 'sepatu-lari',
  brandId: 'b1',
  brandName: 'Nike',
  brandSlug: 'nike',
  name: 'Nike Air Zoom Pegasus 40',
  slug: 'nike-air-zoom-pegasus-40',
  description: 'Sepatu lari andalan dengan bantalan responsif.',
  basePrice: 1899000,
  isActive: true,
  inStock: true,
  minPrice: 1899000,
  maxPrice: 2099000,
  totalStock: 15,
};

const sampleOutOfStockProduct: Product = {
  ...sampleProduct,
  id: 'p2',
  name: 'Nike Vaporfly Next% 3',
  slug: 'nike-vaporfly-next-3',
  inStock: false,
  totalStock: 0,
};

function renderWithProviders(ui: React.ReactElement, initialRoute = '/produk') {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialRoute]}>
        <Routes>
          <Route path="/produk" element={ui} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Catalog Features (T9-04)', () => {
  beforeEach(() => {
    vi.spyOn(catalogApiModule, 'useCategories').mockReturnValue({
      data: [{ id: 'c1', name: 'Sepatu Lari', slug: 'sepatu-lari', sortOrder: 1, isActive: true }],
      isLoading: false,
      isError: false,
    } as unknown as ReturnType<typeof catalogApiModule.useCategories>);

    vi.spyOn(catalogApiModule, 'useBrands').mockReturnValue({
      data: [{ id: 'b1', name: 'Nike', slug: 'nike', isActive: true }],
      isLoading: false,
      isError: false,
    } as unknown as ReturnType<typeof catalogApiModule.useBrands>);
  });

  describe('ProductCard Component', () => {
    it('renders product details and price correctly', () => {
      render(
        <MemoryRouter>
          <ProductCard product={sampleProduct} />
        </MemoryRouter>,
      );

      expect(screen.getByText('Nike Air Zoom Pegasus 40')).toBeInTheDocument();
      expect(screen.getByText('Nike')).toBeInTheDocument();
      expect(screen.getByText('Sepatu Lari')).toBeInTheDocument();
      expect(screen.getByText(/1\.899\.000/)).toBeInTheDocument();
      expect(screen.getByText(/2\.099\.000/)).toBeInTheDocument();
      expect(screen.queryByText('Habis')).not.toBeInTheDocument();
    });

    it('renders Habis badge when product is out of stock', () => {
      render(
        <MemoryRouter>
          <ProductCard product={sampleOutOfStockProduct} />
        </MemoryRouter>,
      );

      expect(screen.getByText('Habis')).toBeInTheDocument();
    });
  });

  describe('CatalogPage URL Query State Persistence', () => {
    it('initializes filters from URL search params on page load/refresh', async () => {
      const mockPagedResult: PagedResult<Product> = {
        items: [sampleProduct],
        page: 2,
        pageSize: 12,
        totalItems: 25,
        totalPages: 3,
        hasPreviousPage: true,
        hasNextPage: true,
      };

      const useProductsSpy = vi.spyOn(catalogApiModule, 'useProducts').mockReturnValue({
        data: mockPagedResult,
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      } as unknown as ReturnType<typeof catalogApiModule.useProducts>);

      renderWithProviders(
        <CatalogPage />,
        '/produk?categorySlug=sepatu-lari&brandSlug=nike&sort=price_asc&inStock=true&page=2',
      );

      // Verify hook received filter params extracted from URL
      expect(useProductsSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          categorySlug: 'sepatu-lari',
          brandSlug: 'nike',
          sort: 'price_asc',
          inStock: true,
          page: 2,
        }),
      );

      // Verify active filter chips are rendered from URL
      expect(screen.getByText(/Kategori: sepatu-lari/)).toBeInTheDocument();
      expect(screen.getByText(/Brand: nike/)).toBeInTheDocument();
      expect(screen.getByText(/Tersedia Saja/)).toBeInTheDocument();

      // Verify product rendered
      expect(screen.getByText('Nike Air Zoom Pegasus 40')).toBeInTheDocument();
      expect(screen.getByText(/Menampilkan 1 dari 25 produk/)).toBeInTheDocument();
    });
  });
});
