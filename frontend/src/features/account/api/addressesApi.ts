import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import http from '@/lib/http';
import { Address, CreateAddressInput, UpdateAddressInput, ShippingRate } from '@/types';

export const addressesApi = {
  getAddresses: async (): Promise<Address[]> => {
    const res = await http.get<Address[]>('/addresses');
    return res.data;
  },

  getAddressById: async (id: string): Promise<Address> => {
    const res = await http.get<Address>(`/addresses/${id}`);
    return res.data;
  },

  createAddress: async (data: CreateAddressInput): Promise<Address> => {
    const res = await http.post<Address>('/addresses', data);
    return res.data;
  },

  updateAddress: async (id: string, data: UpdateAddressInput): Promise<Address> => {
    const res = await http.put<Address>(`/addresses/${id}`, data);
    return res.data;
  },

  deleteAddress: async (id: string): Promise<void> => {
    await http.delete(`/addresses/${id}`);
  },

  setDefaultAddress: async (id: string): Promise<void> => {
    await http.post(`/addresses/${id}/set-default`);
  },

  getProvinces: async (): Promise<ShippingRate[]> => {
    const res = await http.get<ShippingRate[]>('/shipping/provinces');
    return res.data;
  },
};

export function useAddresses() {
  return useQuery({
    queryKey: ['addresses'],
    queryFn: () => addressesApi.getAddresses(),
  });
}

export function useShippingProvinces() {
  return useQuery({
    queryKey: ['shipping', 'provinces'],
    queryFn: () => addressesApi.getProvinces(),
    staleTime: 1000 * 60 * 10, // 10 minutes cache
  });
}

export function useCreateAddress() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateAddressInput) => addressesApi.createAddress(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['addresses'] });
    },
  });
}

export function useUpdateAddress() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAddressInput }) =>
      addressesApi.updateAddress(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['addresses'] });
    },
  });
}

export function useDeleteAddress() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => addressesApi.deleteAddress(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['addresses'] });
    },
  });
}

export function useSetDefaultAddress() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => addressesApi.setDefaultAddress(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['addresses'] });
    },
  });
}
