import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import http from '@/lib/http';
import { Cart } from '@/types';

export interface AddCartItemParams {
  variantId: string;
  quantity: number;
}

export const cartApi = {
  getCart: async (): Promise<Cart> => {
    const res = await http.get<Cart>('/cart');
    return res.data;
  },

  addItem: async (params: AddCartItemParams): Promise<Cart> => {
    const res = await http.post<Cart>('/cart/items', params);
    return res.data;
  },

  updateItem: async (variantId: string, quantity: number): Promise<Cart> => {
    const res = await http.put<Cart>(`/cart/items/${variantId}`, { quantity });
    return res.data;
  },

  removeItem: async (variantId: string): Promise<Cart> => {
    const res = await http.delete<Cart>(`/cart/items/${variantId}`);
    return res.data;
  },

  clearCart: async (): Promise<{ message: string }> => {
    const res = await http.delete<{ message: string }>('/cart');
    return res.data;
  },
};

export function useCart(enabled = true) {
  return useQuery({
    queryKey: ['cart'],
    queryFn: () => cartApi.getCart(),
    enabled,
    staleTime: 0,
  });
}

export function useAddToCart() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: AddCartItemParams) => cartApi.addItem(params),
    onSuccess: (updatedCart) => {
      queryClient.setQueryData(['cart'], updatedCart);
    },
  });
}
