import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import http from '@/lib/http';

export interface ShippingRate {
  provinceCode: string;
  provinceName: string;
  baseFee: number;
  perKgFee: number;
  estimatedDaysMin: number;
  estimatedDaysMax: number;
  isActive: boolean;
  updatedAt: string | null;
}

export interface UpdateShippingRateInput {
  baseFee: number;
  perKgFee: number;
  estimatedDaysMin: number;
  estimatedDaysMax: number;
  isActive: boolean;
}

export const adminShippingRatesApi = {
  listRates: async (): Promise<ShippingRate[]> => {
    const res = await http.get<ShippingRate[]>('/admin/shipping-rates');
    return res.data;
  },

  getRate: async (provinceCode: string): Promise<ShippingRate> => {
    const res = await http.get<ShippingRate>(`/admin/shipping-rates/${provinceCode}`);
    return res.data;
  },

  updateRate: async (provinceCode: string, input: UpdateShippingRateInput): Promise<ShippingRate> => {
    const res = await http.put<ShippingRate>(`/admin/shipping-rates/${provinceCode}`, input);
    return res.data;
  },
};

export const useAdminShippingRates = () => {
  return useQuery({
    queryKey: ['admin', 'shipping-rates'],
    queryFn: () => adminShippingRatesApi.listRates(),
  });
};

export const useUpdateShippingRateMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ provinceCode, input }: { provinceCode: string; input: UpdateShippingRateInput }) =>
      adminShippingRatesApi.updateRate(provinceCode, input),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin', 'shipping-rate', updated.provinceCode], updated);
      queryClient.invalidateQueries({ queryKey: ['admin', 'shipping-rates'] });
    },
  });
};
