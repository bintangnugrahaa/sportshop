import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import http from '@/lib/http';
import { useAuthStore } from '@/stores/authStore';

export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  phoneNumber?: string | null;
  roles: string[];
  createdAt: string;
}

export interface UpdateProfileInput {
  fullName: string;
  phoneNumber?: string | null;
}

export interface ChangePasswordInput {
  currentPassword: string;
  newPassword: string;
}

export const profileApi = {
  getProfile: async (): Promise<UserProfile> => {
    const res = await http.get<UserProfile>('/me');
    return res.data;
  },

  updateProfile: async (data: UpdateProfileInput): Promise<UserProfile> => {
    const res = await http.put<UserProfile>('/me', data);
    return res.data;
  },

  changePassword: async (data: ChangePasswordInput): Promise<{ message: string }> => {
    const res = await http.post<{ message: string }>('/me/change-password', data);
    return res.data;
  },
};

export function useProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: () => profileApi.getProfile(),
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  const { setUser, user } = useAuthStore();

  return useMutation({
    mutationFn: (data: UpdateProfileInput) => profileApi.updateProfile(data),
    onSuccess: (updatedProfile) => {
      queryClient.setQueryData(['profile'], updatedProfile);
      if (user) {
        setUser({
          ...user,
          fullName: updatedProfile.fullName,
          phoneNumber: updatedProfile.phoneNumber,
        });
      }
    },
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (data: ChangePasswordInput) => profileApi.changePassword(data),
  });
}
