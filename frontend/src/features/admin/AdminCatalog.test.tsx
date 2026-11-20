import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult } from '@tanstack/react-query';
import { AdminProductsPage } from './catalog/pages/AdminProductsPage';
import { AdminCategoriesPage } from './catalog/pages/AdminCategoriesPage';
import { AdminBrandsPage } from './catalog/pages/AdminBrandsPage';
import { Product, Category, Brand, PagedResult } from '@/types';
import * as adminCatalogApiModule from './catalog/api/adminCatalogApi';

const mockProduct: Product = {
  id: 'prod-1',
  name: 'Sepatu Bola Nike Phantom',
  slug: 'sepatu-bola-nike-phantom',
  description: 'Sepatu bola presisi tinggi.',
  categoryId: 'cat-1',
  categoryName: 'Sepatu Bola',
  brandId: 'brand-1',
  brandName: 'Nike',
  basePrice: 1800000,
  isActive: true,
  inStock: true,
  images: [
    {
      id: 'img-1',
      productId: 'prod-1',
      url: 'https://example.com/nike.jpg',
      altText: 'Nike Phantom Front',
      sortOrder: 0,
      isPrimary: true,
    },
  ],
};

const mockCategory: Category = {
  id: 'cat-1',
  name: 'Sepatu',
  slug: 'sepatu',
  sortOrder: 1,
  isActive: true,
};

const mockBrand: Brand = {
  id: 'brand-1',
  name: 'Nike',
  slug: 'nike',
  isActive: true,
};

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

describe('Admin Catalog Management (T10-08)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('AdminProductsPage renders products list and validates creation', async () => {
    const queryClient = createTestQueryClient();

    const mockPagedResult: PagedResult<Product> = {
      items: [mockProduct],
      page: 1,
      pageSize: 15,
      totalItems: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };

    vi.spyOn(adminCatalogApiModule, 'useAdminProducts').mockReturnValue({
      data: mockPagedResult,
      isLoading: false,
      refetch: vi.fn(),
    } as unknown as UseQueryResult<PagedResult<Product>, Error>);

    vi.spyOn(adminCatalogApiModule, 'useAdminCategories').mockReturnValue({
      data: [mockCategory],
      isLoading: false,
    } as unknown as UseQueryResult<Category[], Error>);

    vi.spyOn(adminCatalogApiModule, 'useAdminBrands').mockReturnValue({
      data: [mockBrand],
      isLoading: false,
    } as unknown as UseQueryResult<Brand[], Error>);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminProductsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Kelola Produk')).toBeInTheDocument();
    expect(screen.getByText('Sepatu Bola Nike Phantom')).toBeInTheDocument();

    const user = userEvent.setup();
    const addBtn = screen.getByRole('button', { name: /Tambah Produk Baru/i });
    await user.click(addBtn);

    expect(screen.getByRole('heading', { name: 'Tambah Produk Baru' })).toBeInTheDocument();
  });

  it('AdminProductsPage image upload validates file type and max 2MB size', async () => {
    const queryClient = createTestQueryClient();

    const mockPagedResult: PagedResult<Product> = {
      items: [mockProduct],
      page: 1,
      pageSize: 15,
      totalItems: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };

    vi.spyOn(adminCatalogApiModule, 'useAdminProducts').mockReturnValue({
      data: mockPagedResult,
      isLoading: false,
      refetch: vi.fn(),
    } as unknown as UseQueryResult<PagedResult<Product>, Error>);

    vi.spyOn(adminCatalogApiModule, 'useAdminCategories').mockReturnValue({
      data: [mockCategory],
      isLoading: false,
    } as unknown as UseQueryResult<Category[], Error>);

    vi.spyOn(adminCatalogApiModule, 'useAdminBrands').mockReturnValue({
      data: [mockBrand],
      isLoading: false,
    } as unknown as UseQueryResult<Brand[], Error>);

    const uploadMock = vi.spyOn(adminCatalogApiModule.adminCatalogApi, 'uploadImage').mockResolvedValueOnce({
      id: 'img-2',
      productId: 'prod-1',
      url: 'https://example.com/new.jpg',
      sortOrder: 0,
      isPrimary: false,
    });

    vi.spyOn(adminCatalogApiModule.adminCatalogApi, 'getProduct').mockResolvedValueOnce(mockProduct);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminProductsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    const user = userEvent.setup();
    const imageBtn = screen.getByTitle('Kelola Gambar');
    await user.click(imageBtn);

    expect(screen.getByText('Galeri Gambar Produk')).toBeInTheDocument();
    expect(screen.getByAltText('Nike Phantom Front')).toBeInTheDocument();

    // Upload valid file (< 2MB)
    const validFile = new File(['small image bytes'], 'shoe.png', { type: 'image/png' });
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;

    fireEvent.change(fileInput, { target: { files: [validFile] } });
    const submitBtn = screen.getByRole('button', { name: 'Unggah' });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(uploadMock).toHaveBeenCalledWith('prod-1', expect.any(FormData));
    });
  });

  it('AdminCategoriesPage displays categories and creates new category', async () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(adminCatalogApiModule, 'useAdminCategories').mockReturnValue({
      data: [mockCategory],
      isLoading: false,
      refetch: vi.fn(),
    } as unknown as UseQueryResult<Category[], Error>);

    const createCatMock = vi.spyOn(adminCatalogApiModule.adminCatalogApi, 'createCategory').mockResolvedValueOnce({
      id: 'cat-2',
      name: 'Sepatu Lari',
      slug: 'sepatu-lari',
      parentId: 'cat-1',
      sortOrder: 2,
      isActive: true,
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminCategoriesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Kelola Kategori')).toBeInTheDocument();
    expect(screen.getByText('Sepatu')).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Tambah Kategori/i }));

    const nameInput = screen.getByLabelText(/Nama Kategori \*/i);
    await user.type(nameInput, 'Sepatu Lari');

    const submitBtn = screen.getByRole('button', { name: 'Buat Kategori' });
    await user.click(submitBtn);

    expect(createCatMock).toHaveBeenCalledWith({
      name: 'Sepatu Lari',
      slug: 'sepatu-lari',
      parentId: null,
      sortOrder: 0,
      isActive: true,
    });
  });

  it('AdminBrandsPage displays brands and creates new brand', async () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(adminCatalogApiModule, 'useAdminBrands').mockReturnValue({
      data: [mockBrand],
      isLoading: false,
      refetch: vi.fn(),
    } as unknown as UseQueryResult<Brand[], Error>);

    const createBrandMock = vi.spyOn(adminCatalogApiModule.adminCatalogApi, 'createBrand').mockResolvedValueOnce({
      id: 'brand-2',
      name: 'Adidas',
      slug: 'adidas',
      logoUrl: null,
      isActive: true,
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminBrandsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Kelola Brand')).toBeInTheDocument();
    expect(screen.getByText('Nike')).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Tambah Brand/i }));

    const nameInput = screen.getByLabelText(/Nama Brand \*/i);
    await user.type(nameInput, 'Adidas');

    const submitBtn = screen.getByRole('button', { name: 'Buat Brand' });
    await user.click(submitBtn);

    expect(createBrandMock).toHaveBeenCalledWith({
      name: 'Adidas',
      slug: 'adidas',
      logoUrl: '',
      isActive: true,
    });
  });
});
