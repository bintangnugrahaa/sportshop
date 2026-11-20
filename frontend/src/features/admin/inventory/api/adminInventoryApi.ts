import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import http from '@/lib/http';

export interface LowStockVariant {
  variantId: string;
  productId: string;
  productName: string;
  sku: string;
  optionSize: string | null;
  optionColor: string | null;
  stockQuantity: number;
  price: number;
}

export interface InventoryMovement {
  id: string;
  variantId: string;
  changeQuantity: number;
  reason: string;
  orderId: string | null;
  note: string | null;
  createdBy: string | null;
  createdAt: string;
}

export interface AdjustStockInput {
  changeQuantity: number;
  note: string;
}

export const adminInventoryApi = {
  getLowStock: async (threshold = 5): Promise<LowStockVariant[]> => {
    const res = await http.get<LowStockVariant[]>('/admin/inventory/low-stock', {
      params: { threshold },
    });
    return res.data;
  },

  adjustStock: async (variantId: string, input: AdjustStockInput): Promise<InventoryMovement> => {
    const res = await http.post<InventoryMovement>(
      `/admin/variants/${variantId}/stock-adjustments`,
      input
    );
    return res.data;
  },
};

export const useLowStockVariants = (threshold = 5) => {
  return useQuery({
    queryKey: ['admin', 'low-stock', threshold],
    queryFn: () => adminInventoryApi.getLowStock(threshold),
  });
};

export const useAdjustStockMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ variantId, input }: { variantId: string; input: AdjustStockInput }) =>
      adminInventoryApi.adjustStock(variantId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'low-stock'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'dashboard-summary'] });
    },
  });
};
