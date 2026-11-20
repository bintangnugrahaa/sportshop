import { useQuery } from '@tanstack/react-query';
import http from '@/lib/http';

export interface DashboardKpi {
  ordersToday: number;
  revenue7Days: number;
  revenue30Days: number;
  pendingProcessingOrders: number;
  lowStockCount: number;
}

export interface DailyRevenue {
  date: string;
  revenue: number;
  ordersCount: number;
}

export interface TopSellingProduct {
  productId: string;
  productName: string;
  sku: string;
  totalQuantitySold: number;
  totalRevenue: number;
  imageUrl?: string | null;
}

export interface LowStockProduct {
  variantId: string;
  productId: string;
  productName: string;
  sku: string;
  optionSize?: string | null;
  optionColor?: string | null;
  stockQuantity: number;
  price: number;
}

export interface RecentOrder {
  id: string;
  orderNumber: string;
  customerName: string;
  total: number;
  status: string;
  createdAt: string;
  itemsCount: number;
}

export interface DashboardSummary {
  kpi: DashboardKpi;
  revenueChart: DailyRevenue[];
  topSellingProducts: TopSellingProduct[];
  lowStockProducts: LowStockProduct[];
  recentOrders: RecentOrder[];
}

export const adminDashboardApi = {
  getSummary: async (): Promise<DashboardSummary> => {
    const res = await http.get<DashboardSummary>('/admin/dashboard/summary');
    return res.data;
  },
};

export function useAdminDashboard() {
  return useQuery({
    queryKey: ['admin', 'dashboard', 'summary'],
    queryFn: () => adminDashboardApi.getSummary(),
    refetchInterval: 1000 * 60, // Refresh every 1 minute
  });
}
