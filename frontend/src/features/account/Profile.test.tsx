import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult } from '@tanstack/react-query';
import { ProfilePage } from './pages/ProfilePage';
import { UserProfile } from './api/profileApi';
import * as profileApiModule from './api/profileApi';

const mockProfile: UserProfile = {
  id: 'user-1',
  email: 'budi@example.com',
  fullName: 'Budi Santoso',
  phoneNumber: '08123456789',
  roles: ['Customer'],
  createdAt: '2026-11-20T00:00:00Z',
};

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

describe('Profile & Change Password Feature (T10-06)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders profile details and submits updated profile', async () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(profileApiModule, 'useProfile').mockReturnValue({
      data: mockProfile,
      isLoading: false,
    } as unknown as UseQueryResult<UserProfile, Error>);

    const updateMock = vi.spyOn(profileApiModule.profileApi, 'updateProfile').mockResolvedValueOnce({
      ...mockProfile,
      fullName: 'Budi Hartono',
      phoneNumber: '08987654321',
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <ProfilePage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText('Profil & Keamanan Akun')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Budi Santoso')).toBeInTheDocument();
    expect(screen.getByDisplayValue('budi@example.com')).toBeInTheDocument();

    const user = userEvent.setup();
    const nameInput = screen.getByDisplayValue('Budi Santoso');
    await user.clear(nameInput);
    await user.type(nameInput, 'Budi Hartono');

    const phoneInput = screen.getByDisplayValue('08123456789');
    await user.clear(phoneInput);
    await user.type(phoneInput, '08987654321');

    const saveBtn = screen.getByRole('button', { name: /Simpan Perubahan Profil/i });
    await user.click(saveBtn);

    expect(updateMock).toHaveBeenCalledWith({
      fullName: 'Budi Hartono',
      phoneNumber: '08987654321',
    });
  });

  it('validates change password and submits with matching complex password', async () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(profileApiModule, 'useProfile').mockReturnValue({
      data: mockProfile,
      isLoading: false,
    } as unknown as UseQueryResult<UserProfile, Error>);

    const changePwMock = vi.spyOn(profileApiModule.profileApi, 'changePassword').mockResolvedValueOnce({
      message: 'Kata sandi berhasil diubah.',
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <ProfilePage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    const user = userEvent.setup();
    const currentPwInput = screen.getByLabelText(/Kata Sandi Saat Ini \*/i);
    const newPwInput = screen.getByLabelText(/^Kata Sandi Baru \*/i);
    const confirmPwInput = screen.getByLabelText(/Konfirmasi Kata Sandi Baru \*/i);
    const submitBtn = screen.getByRole('button', { name: /Perbarui Kata Sandi/i });

    // Try submitting with weak password
    await user.type(currentPwInput, 'OldPass123!');
    await user.type(newPwInput, 'weak');
    await user.type(confirmPwInput, 'weak');
    await user.click(submitBtn);

    expect(await screen.findByText(/Kata sandi baru minimal 8 karakter/i)).toBeInTheDocument();
    expect(changePwMock).not.toHaveBeenCalled();

    // Now type valid password but mismatched confirm
    await user.clear(newPwInput);
    await user.type(newPwInput, 'NewPass123!');
    await user.clear(confirmPwInput);
    await user.type(confirmPwInput, 'Different123!');
    await user.click(submitBtn);

    expect(await screen.findByText(/Konfirmasi kata sandi baru tidak cocok/i)).toBeInTheDocument();

    // Now correct confirm password
    await user.clear(confirmPwInput);
    await user.type(confirmPwInput, 'NewPass123!');
    await user.click(submitBtn);

    expect(changePwMock).toHaveBeenCalledWith({
      currentPassword: 'OldPass123!',
      newPassword: 'NewPass123!',
    });
  });
});
