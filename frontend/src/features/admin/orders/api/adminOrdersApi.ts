import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import http from '@/lib/http';
import { Order, OrderStatus, PagedResult } from '@/types';

export interface ListAdminOrdersParams {
  status?: OrderStatus;
  from?: string;
  to?: string;
  q?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminProcessOrderInput {
  note?: string;
}

export interface AdminShipOrderInput {
  courier: string;
  trackingNumber: string;
  note?: string;
}

export interface AdminCompleteOrderInput {
  note?: string;
}

export interface AdminCancelOrderInput {
  reason?: string;
}

export const adminOrdersApi = {
  listOrders: async (params?: ListAdminOrdersParams): Promise<PagedResult<Order>> => {
    const res = await http.get<PagedResult<Order>>('/admin/orders', { params });
    return res.data;
  },

  getOrderByNumber: async (orderNumber: string): Promise<Order> => {
    const res = await http.get<Order>(`/admin/orders/${orderNumber}`);
    return res.data;
  },

  processOrder: async (orderNumber: string, input?: AdminProcessOrderInput): Promise<Order> => {
    const res = await http.post<Order>(`/admin/orders/${orderNumber}/process`, input ?? {});
    return res.data;
  },

  shipOrder: async (orderNumber: string, input: AdminShipOrderInput): Promise<Order> => {
    const res = await http.post<Order>(`/admin/orders/${orderNumber}/ship`, input);
    return res.data;
  },

  completeOrder: async (orderNumber: string, input?: AdminCompleteOrderInput): Promise<Order> => {
    const res = await http.post<Order>(`/admin/orders/${orderNumber}/complete`, input ?? {});
    return res.data;
  },

  cancelOrder: async (orderNumber: string, input?: AdminCancelOrderInput): Promise<Order> => {
    const res = await http.post<Order>(`/admin/orders/${orderNumber}/cancel`, input ?? {});
    return res.data;
  },
};

export const useAdminOrders = (params?: ListAdminOrdersParams) => {
  return useQuery({
    queryKey: ['admin', 'orders', params],
    queryFn: () => adminOrdersApi.listOrders(params),
  });
};

export const useAdminOrderDetail = (orderNumber: string) => {
  return useQuery({
    queryKey: ['admin', 'order', orderNumber],
    queryFn: () => adminOrdersApi.getOrderByNumber(orderNumber),
    enabled: !!orderNumber,
  });
};

export const useAdminProcessOrderMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ orderNumber, input }: { orderNumber: string; input?: AdminProcessOrderInput }) =>
      adminOrdersApi.processOrder(orderNumber, input),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin', 'order', updated.orderNumber], updated);
      queryClient.invalidateQueries({ queryKey: ['admin', 'orders'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'dashboard-summary'] });
    },
  });
};

export const useAdminShipOrderMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ orderNumber, input }: { orderNumber: string; input: AdminShipOrderInput }) =>
      adminOrdersApi.shipOrder(orderNumber, input),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin', 'order', updated.orderNumber], updated);
      queryClient.invalidateQueries({ queryKey: ['admin', 'orders'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'dashboard-summary'] });
    },
  });
};

export const useAdminCompleteOrderMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ orderNumber, input }: { orderNumber: string; input?: AdminCompleteOrderInput }) =>
      adminOrdersApi.completeOrder(orderNumber, input),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin', 'order', updated.orderNumber], updated);
      queryClient.invalidateQueries({ queryKey: ['admin', 'orders'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'dashboard-summary'] });
    },
  });
};

export const useAdminCancelOrderMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ orderNumber, input }: { orderNumber: string; input?: AdminCancelOrderInput }) =>
      adminOrdersApi.cancelOrder(orderNumber, input),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin', 'order', updated.orderNumber], updated);
      queryClient.invalidateQueries({ queryKey: ['admin', 'orders'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'dashboard-summary'] });
    },
  });
};
