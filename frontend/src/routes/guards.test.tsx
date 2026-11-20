import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { RequireAuth, RequireAdmin, RequireGuest } from './guards';
import { useAuthStore } from '@/stores/authStore';

describe('Route Guards', () => {
  it('RequireAuth redirects unauthenticated user to /masuk', () => {
    useAuthStore.setState({ isAuthenticated: false, user: null, token: null });

    render(
      <MemoryRouter initialEntries={['/keranjang']}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route path="/keranjang" element={<div>Halaman Keranjang Rahasia</div>} />
          </Route>
          <Route path="/masuk" element={<div>Halaman Masuk</div>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.queryByText('Halaman Keranjang Rahasia')).not.toBeInTheDocument();
    expect(screen.getByText('Halaman Masuk')).toBeInTheDocument();
  });

  it('RequireAuth renders content when authenticated', () => {
    useAuthStore.setState({
      isAuthenticated: true,
      user: { id: 'u1', email: 'test@sportshop.dev', fullName: 'Tester', role: 'Customer' },
      token: 'valid-jwt',
    });

    render(
      <MemoryRouter initialEntries={['/keranjang']}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route path="/keranjang" element={<div>Halaman Keranjang Rahasia</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Halaman Keranjang Rahasia')).toBeInTheDocument();
  });

  it('RequireAdmin redirects Customer to /', () => {
    useAuthStore.setState({
      isAuthenticated: true,
      user: { id: 'u1', email: 'test@sportshop.dev', fullName: 'Tester', role: 'Customer' },
      token: 'valid-jwt',
    });

    render(
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route element={<RequireAdmin />}>
            <Route path="/admin" element={<div>Halaman Admin</div>} />
          </Route>
          <Route path="/" element={<div>Halaman Beranda Publik</div>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.queryByText('Halaman Admin')).not.toBeInTheDocument();
    expect(screen.getByText('Halaman Beranda Publik')).toBeInTheDocument();
  });

  it('RequireAdmin renders content for Admin role', () => {
    useAuthStore.setState({
      isAuthenticated: true,
      user: { id: 'u1', email: 'admin@sportshop.dev', fullName: 'Admin', role: 'Admin' },
      token: 'valid-jwt',
    });

    render(
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route element={<RequireAdmin />}>
            <Route path="/admin" element={<div>Halaman Admin</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Halaman Admin')).toBeInTheDocument();
  });

  it('RequireGuest redirects authenticated user to /', () => {
    useAuthStore.setState({
      isAuthenticated: true,
      user: { id: 'u1', email: 'test@sportshop.dev', fullName: 'Tester', role: 'Customer' },
      token: 'valid-jwt',
    });

    render(
      <MemoryRouter initialEntries={['/masuk']}>
        <Routes>
          <Route element={<RequireGuest />}>
            <Route path="/masuk" element={<div>Halaman Masuk Form</div>} />
          </Route>
          <Route path="/" element={<div>Beranda</div>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.queryByText('Halaman Masuk Form')).not.toBeInTheDocument();
    expect(screen.getByText('Beranda')).toBeInTheDocument();
  });
});
