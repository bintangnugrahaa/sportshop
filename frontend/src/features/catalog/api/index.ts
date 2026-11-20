import { useQuery } from '@tanstack/react-query';
import http from '@/lib/http';
import { Product, Category, Brand, PagedResult } from '@/types';

export interface ProductListParams {
  q?: string;
  categorySlug?: string;
  brandSlug?: string;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export const catalogApi = {
  getProducts: async (params: ProductListParams = {}): Promise<PagedResult<Product>> => {
    const searchParams = new URLSearchParams();
    if (params.q) searchParams.set('q', params.q);
    if (params.categorySlug) searchParams.set('categorySlug', params.categorySlug);
    if (params.brandSlug) searchParams.set('brandSlug', params.brandSlug);
    if (params.minPrice !== undefined && params.minPrice !== null)
      searchParams.set('minPrice', params.minPrice.toString());
    if (params.maxPrice !== undefined && params.maxPrice !== null)
      searchParams.set('maxPrice', params.maxPrice.toString());
    if (params.inStock !== undefined && params.inStock !== null)
      searchParams.set('inStock', params.inStock.toString());
    if (params.sort) searchParams.set('sort', params.sort);
    if (params.page) searchParams.set('page', params.page.toString());
    if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString());

    const queryString = searchParams.toString();
    const url = queryString ? `/products?${queryString}` : '/products';
    const res = await http.get<PagedResult<Product>>(url);
    return res.data;
  },

  getProductBySlug: async (slug: string): Promise<Product> => {
    const res = await http.get<Product>(`/products/${encodeURIComponent(slug)}`);
    return res.data;
  },

  getCategories: async (): Promise<Category[]> => {
    const res = await http.get<Category[]>('/categories');
    return res.data;
  },

  getBrands: async (): Promise<Brand[]> => {
    const res = await http.get<Brand[]>('/brands');
    return res.data;
  },
};

export function useProducts(params: ProductListParams) {
  return useQuery({
    queryKey: ['products', params],
    queryFn: () => catalogApi.getProducts(params),
  });
}

export function useProductBySlug(slug: string) {
  return useQuery({
    queryKey: ['product', slug],
    queryFn: () => catalogApi.getProductBySlug(slug),
    enabled: !!slug,
  });
}

export function useCategories() {
  return useQuery({
    queryKey: ['categories'],
    queryFn: () => catalogApi.getCategories(),
    staleTime: 5 * 60 * 1000,
  });
}

export function useBrands() {
  return useQuery({
    queryKey: ['brands'],
    queryFn: () => catalogApi.getBrands(),
    staleTime: 5 * 60 * 1000,
  });
}
