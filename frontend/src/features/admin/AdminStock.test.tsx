import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult, UseMutationResult } from '@tanstack/react-query';
import { AdminStockPage } from './inventory/pages/AdminStockPage';
import * as adminInventoryApiModule from './inventory/api/adminInventoryApi';
import * as adminCatalogApiModule from './catalog/api/adminCatalogApi';
import type { Product, PagedResult } from '@/types';

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

const mockLowStockItems: adminInventoryApiModule.LowStockVariant[] = [
  {
    variantId: 'var-1',
    productId: 'prod-1',
    productName: 'Sepatu Running Pro',
    sku: 'RUN-RED-42',
    optionSize: '42',
    optionColor: 'Merah',
    stockQuantity: 2,
    price: 750000,
  },
];

const mockProductDetail: Product = {
  id: 'prod-1',
  name: 'Sepatu Running Pro',
  slug: 'sepatu-running-pro',
  description: 'Sepatu lari terbaik',
  categoryId: 'cat-1',
  categoryName: 'Sepatu',
  brandId: 'brand-1',
  brandName: 'Nike',
  basePrice: 750000,
  inStock: true,
  isActive: true,
  minPrice: 750000,
  maxPrice: 850000,
  images: [],
  variants: [
    {
      id: 'var-1',
      productId: 'prod-1',
      sku: 'RUN-RED-42',
      optionSize: '42',
      optionColor: 'Merah',
      price: 750000,
      weightGram: 800,
      stockQuantity: 2,
      isActive: true,
    },
  ],
};

const mockProductsPagedResult: PagedResult<Product> = {
  items: [mockProductDetail],
  page: 1,
  pageSize: 50,
  totalItems: 1,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

describe('Admin Stock & Inventory Management (T10-09)', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    vi.spyOn(adminInventoryApiModule, 'useLowStockVariants').mockReturnValue({
      data: mockLowStockItems,
      isLoading: false,
      refetch: vi.fn(),
    } as unknown as UseQueryResult<adminInventoryApiModule.LowStockVariant[], Error>);

    vi.spyOn(adminCatalogApiModule, 'useAdminProducts').mockReturnValue({
      data: mockProductsPagedResult,
      isLoading: false,
      refetch: vi.fn(),
    } as unknown as UseQueryResult<PagedResult<Product>, Error>);
  });

  it('renders low stock items and triggers threshold change', async () => {
    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminStockPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(await screen.findByText('Peringatan Stok Menipis')).toBeInTheDocument();
    expect(screen.getAllByText('Sepatu Running Pro').length).toBeGreaterThan(0);
    expect(screen.getByText('RUN-RED-42')).toBeInTheDocument();
    expect(screen.getByText('2 unit')).toBeInTheDocument();

    // Click threshold ≤ 10
    const threshold10Btn = screen.getByRole('button', { name: '≤ 10' });
    fireEvent.click(threshold10Btn);
    expect(threshold10Btn).toHaveClass('bg-amber-500');
  });

  it('opens adjust stock dialog and submits stock increment successfully', async () => {
    const mutateAsyncMock = vi.fn().mockResolvedValue({
      id: 'mov-1',
      variantId: 'var-1',
      changeQuantity: 5,
      reason: 'ManualAdjust',
      orderId: null,
      note: 'Restok gudang batch 3',
      createdBy: null,
      createdAt: '2026-11-20T10:00:00Z',
    });

    vi.spyOn(adminInventoryApiModule, 'useAdjustStockMutation').mockReturnValue({
      mutateAsync: mutateAsyncMock,
      isPending: false,
    } as unknown as UseMutationResult<
      adminInventoryApiModule.InventoryMovement,
      Error,
      { variantId: string; input: adminInventoryApiModule.AdjustStockInput },
      unknown
    >);

    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminStockPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Click "Sesuaikan Stok" in low stock table
    const adjustButtons = await screen.findAllByRole('button', { name: /Sesuaikan Stok/i });
    expect(adjustButtons.length).toBeGreaterThan(0);
    fireEvent.click(adjustButtons[0]!);

    // Verify modal opened
    expect(screen.getByRole('heading', { name: 'Penyesuaian Stok Manual' })).toBeInTheDocument();
    expect(screen.getByText('Stok Saat Ini: 2')).toBeInTheDocument();

    // Fill amount and note
    const amountInput = screen.getByLabelText(/Jumlah Penambahan/i);
    fireEvent.change(amountInput, { target: { value: '5' } });

    const noteInput = screen.getByLabelText(/Alasan Penyesuaian/i);
    fireEvent.change(noteInput, { target: { value: 'Restok gudang batch 3' } });

    // Submit
    const submitBtn = screen.getByRole('button', { name: 'Simpan Penyesuaian' });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mutateAsyncMock).toHaveBeenCalledWith({
        variantId: 'var-1',
        input: {
          changeQuantity: 5,
          note: 'Restok gudang batch 3',
        },
      });
    });
  });

  it('validates and rejects subtracting more than current stock', async () => {
    const mutateAsyncMock = vi.fn();
    vi.spyOn(adminInventoryApiModule, 'useAdjustStockMutation').mockReturnValue({
      mutateAsync: mutateAsyncMock,
      isPending: false,
    } as unknown as UseMutationResult<
      adminInventoryApiModule.InventoryMovement,
      Error,
      { variantId: string; input: adminInventoryApiModule.AdjustStockInput },
      unknown
    >);

    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminStockPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    const adjustButtons = await screen.findAllByRole('button', { name: /Sesuaikan Stok/i });
    expect(adjustButtons.length).toBeGreaterThan(0);
    fireEvent.click(adjustButtons[0]!);

    // Select subtract
    const subtractTab = screen.getByRole('button', { name: /Kurangi Stok/i });
    fireEvent.click(subtractTab);

    // Enter amount 10 (exceeds stock 2)
    const amountInput = screen.getByLabelText(/Jumlah Pengurangan/i);
    fireEvent.change(amountInput, { target: { value: '10' } });

    const noteInput = screen.getByLabelText(/Alasan Penyesuaian/i);
    fireEvent.change(noteInput, { target: { value: 'Barang rusak' } });

    const submitBtn = screen.getByRole('button', { name: 'Simpan Penyesuaian' });
    expect(submitBtn).toBeDisabled();

    expect(mutateAsyncMock).not.toHaveBeenCalled();
  });

  it('expands product catalog and opens variant stock adjustment', async () => {
    vi.spyOn(adminCatalogApiModule.adminCatalogApi, 'getProduct').mockResolvedValue(mockProductDetail);

    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminStockPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Find "Lihat Varian ▼"
    const expandBtn = await screen.findByRole('button', { name: /Lihat Varian/i });
    fireEvent.click(expandBtn);

    await waitFor(() => {
      expect(adminCatalogApiModule.adminCatalogApi.getProduct).toHaveBeenCalledWith('prod-1');
    });

    // Variant table appears
    expect(await screen.findByRole('button', { name: /Ubah Stok/i })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Ubah Stok/i }));

    expect(screen.getByRole('heading', { name: 'Penyesuaian Stok Manual' })).toBeInTheDocument();
  });
});
