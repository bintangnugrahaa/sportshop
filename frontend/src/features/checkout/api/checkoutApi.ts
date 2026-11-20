import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import http from '@/lib/http';
import { Order, ShippingQuote, PayOrderResponse } from '@/types';

export const checkoutApi = {
  getQuote: async (addressId: string): Promise<ShippingQuote> => {
    const res = await http.post<ShippingQuote>('/shipping/quote', { addressId });
    return res.data;
  },

  checkout: async (addressId: string, idempotencyKey: string): Promise<Order> => {
    const res = await http.post<Order>(
      '/orders/checkout',
      { addressId },
      {
        headers: {
          'Idempotency-Key': idempotencyKey,
        },
      }
    );
    return res.data;
  },

  payOrder: async (orderNumber: string): Promise<PayOrderResponse> => {
    const res = await http.post<PayOrderResponse>(`/orders/${orderNumber}/pay`);
    return res.data;
  },
};

export function useShippingQuote(addressId?: string | null) {
  return useQuery({
    queryKey: ['shipping', 'quote', addressId],
    queryFn: () => checkoutApi.getQuote(addressId!),
    enabled: Boolean(addressId),
    staleTime: 1000 * 30, // 30s
  });
}

export function useCheckout() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ addressId, idempotencyKey }: { addressId: string; idempotencyKey: string }) =>
      checkoutApi.checkout(addressId, idempotencyKey),
    onSuccess: () => {
      // Cart has been emptied on backend
      queryClient.invalidateQueries({ queryKey: ['cart'] });
      queryClient.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}
