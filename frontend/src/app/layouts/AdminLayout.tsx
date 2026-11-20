import React from 'react';
import { NavLink, Outlet, Link, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Package,
  Layers,
  Tag,
  Boxes,
  ShoppingBag,
  Truck,
  ExternalLink,
  LogOut,
  Menu,
  X,
} from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { Button } from '@/components/ui/button';

const navItems = [
  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/admin/produk', label: 'Produk & Varian', icon: Package },
  { to: '/admin/kategori', label: 'Kategori', icon: Layers },
  { to: '/admin/brand', label: 'Brand', icon: Tag },
  { to: '/admin/stok', label: 'Stok & Penyesuaian', icon: Boxes },
  { to: '/admin/pesanan', label: 'Pesanan', icon: ShoppingBag },
  { to: '/admin/ongkir', label: 'Tarif Ongkir', icon: Truck },
];

export const AdminLayout: React.FC = () => {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();
  const [isMobileNavOpen, setIsMobileNavOpen] = React.useState(false);

  const handleLogout = async () => {
    await logout();
    navigate('/');
  };

  return (
    <div className="min-h-screen bg-muted/20 flex flex-col antialiased">
      {/* Top Navbar */}
      <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur h-16 flex items-center justify-between px-4 sm:px-6">
        <div className="flex items-center gap-4">
          <button
            type="button"
            onClick={() => setIsMobileNavOpen(!isMobileNavOpen)}
            className="md:hidden inline-flex items-center justify-center p-2 rounded-md hover:bg-accent text-foreground"
            aria-label="Toggle Menu Admin"
          >
            {isMobileNavOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>

          <Link to="/admin" className="flex items-center gap-2 font-bold text-primary text-lg">
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-white font-black text-sm">
              S
            </span>
            <div className="flex flex-col">
              <span className="leading-tight text-foreground font-black">SportShop</span>
              <span className="text-[10px] text-muted-foreground font-medium uppercase tracking-wider">
                Admin Panel
              </span>
            </div>
          </Link>
        </div>

        {/* User Profile & Store Link */}
        <div className="flex items-center gap-3">
          <Link
            to="/"
            target="_blank"
            className="hidden sm:inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground font-medium transition-colors border rounded-md px-3 py-1.5"
          >
            <span>Lihat Toko</span>
            <ExternalLink className="h-3.5 w-3.5" />
          </Link>

          <div className="flex items-center gap-2 pl-2 border-l">
            <div className="hidden sm:flex flex-col text-right">
              <span className="text-xs font-semibold text-foreground truncate max-w-[120px]">
                {user?.fullName || 'Admin'}
              </span>
              <span className="text-[10px] text-primary font-bold">Administrator</span>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleLogout}
              className="text-xs h-8 px-2 text-muted-foreground hover:text-destructive"
              title="Keluar"
            >
              <LogOut className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </header>

      {/* Main Content with Sidebar */}
      <div className="flex-1 flex">
        {/* Desktop Sidebar */}
        <aside className="hidden md:flex w-64 flex-col border-r bg-background shrink-0">
          <nav className="p-4 space-y-1 text-sm font-medium">
            {navItems.map((item) => {
              const Icon = item.icon;
              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) =>
                    `flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors ${
                      isActive
                        ? 'bg-primary text-white font-semibold shadow-sm'
                        : 'text-muted-foreground hover:bg-accent hover:text-foreground'
                    }`
                  }
                >
                  <Icon className="h-4 w-4" />
                  <span>{item.label}</span>
                </NavLink>
              );
            })}
          </nav>
        </aside>

        {/* Mobile Sidebar Backdrop & Drawer */}
        {isMobileNavOpen && (
          <div className="fixed inset-0 z-50 md:hidden" role="dialog" aria-modal="true">
            <div
              className="fixed inset-0 bg-black/50 backdrop-blur-sm"
              onClick={() => setIsMobileNavOpen(false)}
            />
            <div className="relative z-50 w-64 h-full bg-background border-r p-4 space-y-2">
              <div className="flex items-center justify-between pb-3 border-b mb-3">
                <span className="font-bold text-sm">Menu Admin</span>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setIsMobileNavOpen(false)}
                  className="h-8 w-8 p-0"
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
              <nav className="space-y-1 text-sm font-medium">
                {navItems.map((item) => {
                  const Icon = item.icon;
                  return (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      end={item.end}
                      onClick={() => setIsMobileNavOpen(false)}
                      className={({ isActive }) =>
                        `flex items-center gap-3 px-3 py-2 rounded-lg transition-colors ${
                          isActive
                            ? 'bg-primary text-white font-semibold'
                            : 'text-muted-foreground hover:bg-accent hover:text-foreground'
                        }`
                      }
                    >
                      <Icon className="h-4 w-4" />
                      <span>{item.label}</span>
                    </NavLink>
                  );
                })}
              </nav>
            </div>
          </div>
        )}

        {/* Content Outlet */}
        <main className="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl w-full mx-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AdminLayout;
