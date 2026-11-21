import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider, UseQueryResult } from '@tanstack/react-query';
import { AdminLayout } from '@/app/layouts/AdminLayout';
import { AdminDashboardPage } from './dashboard/pages/AdminDashboardPage';
import { DashboardSummary } from './dashboard/api/adminDashboardApi';
import * as adminDashboardApiModule from './dashboard/api/adminDashboardApi';
import { useAuthStore } from '@/stores/authStore';

const mockSummary: DashboardSummary = {
  kpi: {
    ordersToday: 8,
    revenue7Days: 24500000,
    revenue30Days: 98000000,
    pendingProcessingOrders: 3,
    lowStockCount: 2,
  },
  revenueChart: [
    { date: '2026-11-19', revenue: 3500000, ordersCount: 2 },
    { date: '2026-11-20', revenue: 4200000, ordersCount: 3 },
  ],
  topSellingProducts: [
    {
      productId: 'prod-1',
      productName: 'Sepatu Lari Nike Air Zoom',
      sku: 'NK-AZ-42',
      totalQuantitySold: 15,
      totalRevenue: 22500000,
      imageUrl: null,
    },
  ],
  lowStockProducts: [
    {
      variantId: 'var-1',
      productId: 'prod-2',
      productName: 'Jersey Timnas Indonesia 2026',
      sku: 'INA-JRS-L',
      optionSize: 'L',
      optionColor: 'Merah',
      stockQuantity: 2,
      price: 450000,
    },
  ],
  recentOrders: [
    {
      id: 'ord-1',
      orderNumber: 'SPT-20261120-000001',
      customerName: 'Budi Santoso',
      total: 1215000,
      status: 'Paid',
      createdAt: '2026-11-20T00:00:00Z',
      itemsCount: 1,
    },
  ],
};

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

describe('Admin Framework & Dashboard (T10-07)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      user: {
        id: 'admin-1',
        email: 'admin@sportshop.com',
        fullName: 'Administrator Toko',
        role: 'Admin',
      },
      isAuthenticated: true,
      token: 'admin-token',
    });
  });

  it('AdminLayout renders navigation links and admin panel title', () => {
    render(
      <MemoryRouter>
        <AdminLayout />
      </MemoryRouter>,
    );

    expect(screen.getByText('Admin Panel')).toBeInTheDocument();
    expect(screen.getByText('Produk & Varian')).toBeInTheDocument();
    expect(screen.getByText('Stok & Penyesuaian')).toBeInTheDocument();
    expect(screen.getByText('Administrator Toko')).toBeInTheDocument();
  });

  it('AdminDashboardPage displays KPI cards, products, alerts, and recent orders', () => {
    const queryClient = createTestQueryClient();

    vi.spyOn(adminDashboardApiModule, 'useAdminDashboard').mockReturnValue({
      data: mockSummary,
      isLoading: false,
      error: null,
    } as unknown as UseQueryResult<DashboardSummary, Error>);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AdminDashboardPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByText('Dashboard Ringkasan')).toBeInTheDocument();
    expect(screen.getByText('Pesanan Hari Ini')).toBeInTheDocument();
    expect(screen.getByText('8')).toBeInTheDocument();
    expect(screen.getByText('Sepatu Lari Nike Air Zoom')).toBeInTheDocument();
    expect(screen.getByText('Jersey Timnas Indonesia 2026')).toBeInTheDocument();
    expect(screen.getByText('Sisa 2')).toBeInTheDocument();
    expect(screen.getByText('SPT-20261120-000001')).toBeInTheDocument();
  });
});
