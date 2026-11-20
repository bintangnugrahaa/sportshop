import { useQuery } from '@tanstack/react-query';
import http from '@/lib/http';
import { Product, ProductVariant, ProductImage, Category, Brand, PagedResult } from '@/types';

export interface CreateProductInput {
  categoryId: string;
  brandId: string;
  name: string;
  slug: string;
  description: string;
  basePrice: number;
  isActive: boolean;
  variants?: {
    sku: string;
    optionSize?: string | null;
    optionColor?: string | null;
    price: number;
    weightGram: number;
    stockQuantity: number;
    isActive: boolean;
  }[];
}

export interface UpdateProductInput {
  categoryId: string;
  brandId: string;
  name: string;
  slug: string;
  description: string;
  isActive: boolean;
}

export interface CreateVariantInput {
  sku: string;
  optionSize?: string | null;
  optionColor?: string | null;
  price: number;
  weightGram: number;
  stockQuantity: number;
  isActive: boolean;
}

export interface UpdateVariantInput {
  sku: string;
  optionSize?: string | null;
  optionColor?: string | null;
  price: number;
  weightGram: number;
  isActive: boolean;
}

export interface CreateCategoryInput {
  name: string;
  slug: string;
  parentId?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface CreateBrandInput {
  name: string;
  slug: string;
  logoUrl?: string | null;
  isActive: boolean;
}

export const adminCatalogApi = {
  // Products
  listProducts: async (params?: { page?: number; pageSize?: number; categoryId?: string; brandId?: string }) => {
    const res = await http.get<PagedResult<Product>>('/admin/products', { params });
    return res.data;
  },

  getProduct: async (id: string) => {
    const res = await http.get<Product>(`/admin/products/${id}`);
    return res.data;
  },

  createProduct: async (data: CreateProductInput) => {
    const res = await http.post<Product>('/admin/products', data);
    return res.data;
  },

  updateProduct: async (id: string, data: UpdateProductInput) => {
    const res = await http.put<Product>(`/admin/products/${id}`, data);
    return res.data;
  },

  deleteProduct: async (id: string) => {
    await http.delete(`/admin/products/${id}`);
  },

  // Variants
  createVariant: async (productId: string, data: CreateVariantInput) => {
    const res = await http.post<ProductVariant>(`/admin/products/${productId}/variants`, data);
    return res.data;
  },

  updateVariant: async (variantId: string, data: UpdateVariantInput) => {
    const res = await http.put<ProductVariant>(`/admin/variants/${variantId}`, data);
    return res.data;
  },

  deleteVariant: async (variantId: string) => {
    await http.delete(`/admin/variants/${variantId}`);
  },

  // Images
  uploadImage: async (productId: string, formData: FormData) => {
    const res = await http.post<ProductImage>(`/admin/products/${productId}/images`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return res.data;
  },

  deleteImage: async (productId: string, imageId: string) => {
    await http.delete(`/admin/products/${productId}/images/${imageId}`);
  },

  // Categories
  listCategories: async (includeInactive = true) => {
    const res = await http.get<Category[]>('/admin/categories', {
      params: { includeInactive, treeOnly: false },
    });
    return res.data;
  },

  createCategory: async (data: CreateCategoryInput) => {
    const res = await http.post<Category>('/admin/categories', data);
    return res.data;
  },

  updateCategory: async (id: string, data: CreateCategoryInput) => {
    const res = await http.put<Category>(`/admin/categories/${id}`, data);
    return res.data;
  },

  deleteCategory: async (id: string) => {
    await http.delete(`/admin/categories/${id}`);
  },

  // Brands
  listBrands: async (includeInactive = true) => {
    const res = await http.get<Brand[]>('/admin/brands', {
      params: { includeInactive },
    });
    return res.data;
  },

  createBrand: async (data: CreateBrandInput) => {
    const res = await http.post<Brand>('/admin/brands', data);
    return res.data;
  },

  updateBrand: async (id: string, data: CreateBrandInput) => {
    const res = await http.put<Brand>(`/admin/brands/${id}`, data);
    return res.data;
  },

  deleteBrand: async (id: string) => {
    await http.delete(`/admin/brands/${id}`);
  },
};

export function useAdminProducts(params?: { page?: number; pageSize?: number; categoryId?: string; brandId?: string }) {
  return useQuery({
    queryKey: ['admin', 'products', params],
    queryFn: () => adminCatalogApi.listProducts(params),
  });
}

export function useAdminCategories() {
  return useQuery({
    queryKey: ['admin', 'categories'],
    queryFn: () => adminCatalogApi.listCategories(),
  });
}

export function useAdminBrands() {
  return useQuery({
    queryKey: ['admin', 'brands'],
    queryFn: () => adminCatalogApi.listBrands(),
  });
}
