import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { LoginPage } from './LoginPage';
import { RegisterPage } from './RegisterPage';
import { useAuthStore } from '@/stores/authStore';

describe('Auth Pages & Server Error Mapping (T9-03)', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,
    });
  });

  it('LoginPage renders fields and displays client validation errors', async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    expect(screen.getByLabelText(/Email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Kata Sandi/i)).toBeInTheDocument();

    const submitBtn = screen.getByRole('button', { name: /Masuk/i });
    await user.click(submitBtn);

    expect(await screen.findByText('Email wajib diisi.')).toBeInTheDocument();
    expect(await screen.findByText('Kata sandi wajib diisi.')).toBeInTheDocument();
  });

  it('LoginPage displays server validation error per field', async () => {
    const user = userEvent.setup();

    // Mock login failure with field error from ProblemDetails
    const loginMock = vi.fn().mockImplementation(() => {
      const axiosError = new AxiosError('Bad Request', 'ERR_BAD_REQUEST', undefined, null, {
        status: 400,
        statusText: 'Bad Request',
        headers: {},
        config: {} as unknown as InternalAxiosRequestConfig,
        data: {
          title: 'Validasi gagal',
          status: 400,
          errors: {
            email: ['Akun dengan email ini belum terdaftar.'],
          },
        },
      });
      return Promise.reject(axiosError);
    });

    useAuthStore.setState({ login: loginMock });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    await user.type(screen.getByLabelText(/Email/i), 'unregistered@sportshop.dev');
    await user.type(screen.getByLabelText(/Kata Sandi/i), 'Password123');
    await user.click(screen.getByRole('button', { name: /Masuk/i }));

    // Acceptance criteria: Error server tampil per field
    await waitFor(() => {
      expect(screen.getByText('Akun dengan email ini belum terdaftar.')).toBeInTheDocument();
    });
  });

  it('RegisterPage displays server conflict error on email field', async () => {
    const user = userEvent.setup();

    const registerMock = vi.fn().mockImplementation(() => {
      const axiosError = new AxiosError('Conflict', 'ERR_CONFLICT', undefined, null, {
        status: 409,
        statusText: 'Conflict',
        headers: {},
        config: {} as unknown as InternalAxiosRequestConfig,
        data: {
          title: 'Email sudah terdaftar',
          status: 409,
          errors: {
            email: ['Alamat email sudah digunakan oleh pengguna lain.'],
          },
        },
      });
      return Promise.reject(axiosError);
    });

    useAuthStore.setState({ register: registerMock });

    render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>,
    );

    await user.type(screen.getByLabelText(/Nama Lengkap/i), 'Budi Santoso');
    await user.type(screen.getByLabelText(/Alamat Email/i), 'budi@sportshop.dev');
    await user.type(screen.getByLabelText(/^Kata Sandi/i), 'Password123');
    await user.type(screen.getByLabelText(/Konfirmasi Kata Sandi/i), 'Password123');

    await user.click(screen.getByRole('button', { name: /Daftar Sekarang/i }));

    // Error server tampil per field pada form registrasi
    await waitFor(() => {
      expect(screen.getByText('Alamat email sudah digunakan oleh pengguna lain.')).toBeInTheDocument();
    });
  });
});
