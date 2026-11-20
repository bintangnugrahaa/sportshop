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
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
    },
  });
}

export function useUpdateCartItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ variantId, quantity }: { variantId: string; quantity: number }) =>
      cartApi.updateItem(variantId, quantity),
    onMutate: async ({ variantId, quantity }) => {
      await queryClient.cancelQueries({ queryKey: ['cart'] });
      const previousCart = queryClient.getQueryData<Cart>(['cart']);

      if (previousCart) {
        const updatedItems = previousCart.items.map((item) => {
          if (item.variantId === variantId) {
            const lineTotal = item.unitPrice * quantity;
            const totalWeightGram = item.weightGram * quantity;
            return {
              ...item,
              quantity,
              lineTotal,
              totalWeightGram,
            };
          }
          return item;
        });

        const newSubtotal = updatedItems.reduce((acc, i) => acc + i.lineTotal, 0);
        const newTotalItems = updatedItems.reduce((acc, i) => acc + i.quantity, 0);
        const newTotalWeight = updatedItems.reduce((acc, i) => acc + i.totalWeightGram, 0);

        queryClient.setQueryData<Cart>(['cart'], {
          ...previousCart,
          items: updatedItems,
          subtotal: newSubtotal,
          totalItems: newTotalItems,
          totalWeightGram: newTotalWeight,
        });
      }

      return { previousCart };
    },
    onError: (_err, _variables, context) => {
      if (context?.previousCart) {
        queryClient.setQueryData(['cart'], context.previousCart);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
    },
  });
}

export function useRemoveCartItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variantId: string) => cartApi.removeItem(variantId),
    onMutate: async (variantId) => {
      await queryClient.cancelQueries({ queryKey: ['cart'] });
      const previousCart = queryClient.getQueryData<Cart>(['cart']);

      if (previousCart) {
        const updatedItems = previousCart.items.filter((item) => item.variantId !== variantId);
        const newSubtotal = updatedItems.reduce((acc, i) => acc + i.lineTotal, 0);
        const newTotalItems = updatedItems.reduce((acc, i) => acc + i.quantity, 0);
        const newTotalWeight = updatedItems.reduce((acc, i) => acc + i.totalWeightGram, 0);
        const hasIssues = updatedItems.some(
          (i) => i.isPriceChanged || i.isStockInsufficient || !i.isAvailable
        );

        queryClient.setQueryData<Cart>(['cart'], {
          ...previousCart,
          items: updatedItems,
          subtotal: newSubtotal,
          totalItems: newTotalItems,
          totalWeightGram: newTotalWeight,
          hasIssues,
        });
      }

      return { previousCart };
    },
    onError: (_err, _variables, context) => {
      if (context?.previousCart) {
        queryClient.setQueryData(['cart'], context.previousCart);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
    },
  });
}

export function useClearCart() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => cartApi.clearCart(),
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: ['cart'] });
      const previousCart = queryClient.getQueryData<Cart>(['cart']);

      if (previousCart) {
        queryClient.setQueryData<Cart>(['cart'], {
          ...previousCart,
          items: [],
          subtotal: 0,
          totalItems: 0,
          totalWeightGram: 0,
          hasIssues: false,
        });
      }

      return { previousCart };
    },
    onError: (_err, _variables, context) => {
      if (context?.previousCart) {
        queryClient.setQueryData(['cart'], context.previousCart);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
    },
  });
}
