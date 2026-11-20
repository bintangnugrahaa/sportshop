import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { RootLayout } from '@/app/layouts/RootLayout';
import { AuthLayout } from '@/app/layouts/AuthLayout';
import { RequireAuth, RequireAdmin, RequireGuest } from './guards';

// Lazy load feature components
const HomePage = React.lazy(() => import('@/features/catalog/pages/HomePage'));
const CatalogPage = React.lazy(() => import('@/features/catalog/pages/CatalogPage'));
const ProductDetailPage = React.lazy(() => import('@/features/catalog/pages/ProductDetailPage'));
const LoginPage = React.lazy(() => import('@/features/auth/pages/LoginPage'));
const RegisterPage = React.lazy(() => import('@/features/auth/pages/RegisterPage'));
const ForgotPasswordPage = React.lazy(() => import('@/features/auth/pages/ForgotPasswordPage'));
const ResetPasswordPage = React.lazy(() => import('@/features/auth/pages/ResetPasswordPage'));
const CartPage = React.lazy(() => import('@/features/cart/pages/CartPage'));

// Placeholder components for future phases
const CheckoutPage = () => <div className="container mx-auto p-8">Checkout</div>;
const PaymentStatusPage = () => <div className="container mx-auto p-8">Status Pembayaran</div>;
const MyOrdersPage = () => <div className="container mx-auto p-8">Pesanan Saya</div>;
const ProfilePage = () => <div className="container mx-auto p-8">Profil Pengguna</div>;
const AdminDashboard = () => <div className="container mx-auto p-8">Dashboard Admin</div>;
const NotFoundPage = () => (
  <div className="container mx-auto flex min-h-[60vh] flex-col items-center justify-center text-center">
    <h1 className="text-4xl font-extrabold text-primary">404</h1>
    <p className="mt-2 text-lg text-muted-foreground">Halaman tidak ditemukan.</p>
  </div>
);

export const AppRoutes: React.FC = () => {
  return (
    <React.Suspense
      fallback={
        <div className="flex min-h-[60vh] items-center justify-center">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      }
    >
      <Routes>
        {/* Main Customer Layout */}
        <Route element={<RootLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/produk" element={<CatalogPage />} />
          <Route path="/produk/:slug" element={<ProductDetailPage />} />

          {/* Authenticated Customer Routes */}
          <Route element={<RequireAuth />}>
            <Route path="/keranjang" element={<CartPage />} />
            <Route path="/checkout" element={<CheckoutPage />} />
            <Route path="/checkout/status/:orderNumber" element={<PaymentStatusPage />} />
            <Route path="/akun/pesanan" element={<MyOrdersPage />} />
            <Route path="/akun/profil" element={<ProfilePage />} />
          </Route>

          {/* Admin Routes */}
          <Route element={<RequireAdmin />}>
            <Route path="/admin" element={<AdminDashboard />} />
          </Route>

          {/* 404 */}
          <Route path="/404" element={<NotFoundPage />} />
          <Route path="*" element={<Navigate to="/404" replace />} />
        </Route>

        {/* Guest Auth Layout */}
        <Route element={<RequireGuest />}>
          <Route element={<AuthLayout />}>
            <Route path="/masuk" element={<LoginPage />} />
            <Route path="/daftar" element={<RegisterPage />} />
            <Route path="/lupa-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
          </Route>
        </Route>
      </Routes>
    </React.Suspense>
  );
};
