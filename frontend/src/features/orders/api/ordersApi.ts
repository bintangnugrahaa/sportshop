import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import http from '@/lib/http';
import { Order, OrderStatus, PagedResult } from '@/types';

export interface ListOrdersParams {
  status?: OrderStatus;
  page?: number;
  pageSize?: number;
}

export const ordersApi = {
  listOrders: async (params: ListOrdersParams = {}): Promise<PagedResult<Order>> => {
    const res = await http.get<PagedResult<Order>>('/orders', { params });
    return res.data;
  },

  getOrder: async (orderNumber: string): Promise<Order> => {
    const res = await http.get<Order>(`/orders/${orderNumber}`);
    return res.data;
  },

  cancelOrder: async (orderNumber: string, reason?: string): Promise<Order> => {
    const res = await http.post<Order>(`/orders/${orderNumber}/cancel`, { reason });
    return res.data;
  },
};

export function useOrders(params: ListOrdersParams = {}) {
  return useQuery({
    queryKey: ['orders', params],
    queryFn: () => ordersApi.listOrders(params),
    staleTime: 0,
  });
}

export function useOrderDetail(orderNumber: string) {
  return useQuery({
    queryKey: ['orders', 'detail', orderNumber],
    queryFn: () => ordersApi.getOrder(orderNumber),
    enabled: Boolean(orderNumber),
    staleTime: 0,
  });
}

export function useCancelOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ orderNumber, reason }: { orderNumber: string; reason?: string }) =>
      ordersApi.cancelOrder(orderNumber, reason),
    onSuccess: (order) => {
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      queryClient.setQueryData(['orders', 'detail', order.orderNumber], order);
    },
  });
}
