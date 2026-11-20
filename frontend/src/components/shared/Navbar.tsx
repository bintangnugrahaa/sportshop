import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ShoppingBag, Search, User as UserIcon, Menu, X, LogOut, ShieldCheck, Package } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { useUiStore } from '@/stores/uiStore';
import { useCart } from '@/features/cart/api';
import { Button } from '@/components/ui/button';

export const Navbar: React.FC = () => {
  const navigate = useNavigate();
  const { user, isAuthenticated, logout } = useAuthStore();
  const { isMobileMenuOpen, toggleMobileMenu, closeMobileMenu, openCartDrawer } = useUiStore();
  const { data: cart } = useCart(isAuthenticated);
  const [searchQuery, setSearchQuery] = React.useState('');
  const [isUserMenuOpen, setIsUserMenuOpen] = React.useState(false);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      navigate(`/produk?q=${encodeURIComponent(searchQuery.trim())}`);
      closeMobileMenu();
    }
  };

  const handleLogout = async () => {
    setIsUserMenuOpen(false);
    await logout();
    navigate('/');
  };

  return (
    <header className="sticky top-0 z-40 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container mx-auto flex h-16 items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Brand Logo */}
        <div className="flex items-center gap-6">
          <Link
            to="/"
            onClick={closeMobileMenu}
            className="flex items-center gap-2 text-xl font-bold tracking-tight text-primary transition-colors hover:text-primary-hover"
          >
            <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-white font-black text-lg">
              S
            </span>
            <span>SportShop</span>
          </Link>

          {/* Desktop Navigation */}
          <nav className="hidden md:flex items-center gap-6 text-sm font-medium">
            <Link
              to="/produk"
              className="text-foreground/80 hover:text-foreground transition-colors"
            >
              Semua Produk
            </Link>
            <Link
              to="/produk?categorySlug=sepatu"
              className="text-foreground/80 hover:text-foreground transition-colors"
            >
              Sepatu
            </Link>
            <Link
              to="/produk?categorySlug=jersey"
              className="text-foreground/80 hover:text-foreground transition-colors"
            >
              Jersey
            </Link>
            <Link
              to="/produk?categorySlug=raket"
              className="text-foreground/80 hover:text-foreground transition-colors"
            >
              Raket
            </Link>
          </nav>
        </div>

        {/* Search Bar Desktop */}
        <form onSubmit={handleSearch} className="hidden lg:flex relative max-w-sm flex-1 mx-8">
          <input
            type="search"
            placeholder="Cari sepatu, jersey, raket..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full rounded-full border border-input bg-muted/50 px-4 py-2 pl-10 text-sm focus:bg-background focus:outline-none focus:ring-2 focus:ring-primary"
          />
          <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
        </form>

        {/* Action icons */}
        <div className="flex items-center gap-2 sm:gap-3">
          {/* Cart button */}
          <button
            type="button"
            onClick={() => {
              if (isAuthenticated) {
                openCartDrawer();
              } else {
                navigate('/keranjang');
              }
            }}
            className="relative inline-flex h-9 w-9 items-center justify-center rounded-md border border-input bg-background text-sm font-medium hover:bg-accent transition-colors cursor-pointer"
            aria-label="Keranjang Belanja"
          >
            <ShoppingBag className="h-4 w-4" />
            {cart && cart.totalItems > 0 && (
              <span className="absolute -top-1.5 -right-1.5 flex h-4 w-4 items-center justify-center rounded-full bg-primary text-[10px] font-bold text-white">
                {cart.totalItems > 99 ? '99+' : cart.totalItems}
              </span>
            )}
          </button>

          {/* User Account / Auth */}
          {isAuthenticated && user ? (
            <div className="relative">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
                className="flex items-center gap-2 text-xs font-medium"
              >
                <UserIcon className="h-4 w-4" />
                <span className="hidden sm:inline max-w-[100px] truncate">{user.fullName}</span>
              </Button>

              {isUserMenuOpen && (
                <div
                  className="absolute right-0 mt-2 w-48 rounded-md border bg-popover p-1 shadow-lg ring-1 ring-black/5 z-50 text-sm"
                  onClick={() => setIsUserMenuOpen(false)}
                >
                  <div className="px-3 py-2 border-b">
                    <p className="font-semibold text-xs truncate">{user.fullName}</p>
                    <p className="text-xs text-muted-foreground truncate">{user.email}</p>
                  </div>

                  {user.role === 'Admin' && (
                    <Link
                      to="/admin"
                      className="flex items-center gap-2 px-3 py-2 text-primary font-medium hover:bg-muted rounded"
                    >
                      <ShieldCheck className="h-4 w-4" />
                      Dashboard Admin
                    </Link>
                  )}

                  <Link
                    to="/akun/pesanan"
                    className="flex items-center gap-2 px-3 py-2 hover:bg-muted rounded"
                  >
                    <Package className="h-4 w-4" />
                    Pesanan Saya
                  </Link>

                  <Link
                    to="/akun/profil"
                    className="flex items-center gap-2 px-3 py-2 hover:bg-muted rounded"
                  >
                    <UserIcon className="h-4 w-4" />
                    Profil Akun
                  </Link>

                  <button
                    onClick={handleLogout}
                    className="flex w-full items-center gap-2 px-3 py-2 text-destructive hover:bg-destructive/10 rounded"
                  >
                    <LogOut className="h-4 w-4" />
                    Keluar
                  </button>
                </div>
              )}
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Link to="/masuk">
                <Button variant="ghost" size="sm">
                  Masuk
                </Button>
              </Link>
              <Link to="/daftar" className="hidden sm:inline-flex">
                <Button size="sm">Daftar</Button>
              </Link>
            </div>
          )}

          {/* Mobile hamburger */}
          <Button
            variant="ghost"
            size="icon"
            className="md:hidden"
            onClick={toggleMobileMenu}
            aria-label="Menu Navigasi"
          >
            {isMobileMenuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </Button>
        </div>
      </div>

      {/* Mobile Drawer/Menu */}
      {isMobileMenuOpen && (
        <div className="border-b bg-background px-4 py-4 md:hidden animate-in slide-in-from-top-2">
          <form onSubmit={handleSearch} className="relative mb-4">
            <input
              type="search"
              placeholder="Cari sepatu, jersey, raket..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full rounded-md border border-input bg-muted/50 px-4 py-2 pl-10 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            />
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
          </form>

          <nav className="flex flex-col space-y-2 text-sm font-medium">
            <Link
              to="/produk"
              onClick={closeMobileMenu}
              className="px-2 py-1.5 rounded-md hover:bg-accent"
            >
              Semua Produk
            </Link>
            <Link
              to="/produk?categorySlug=sepatu"
              onClick={closeMobileMenu}
              className="px-2 py-1.5 rounded-md hover:bg-accent"
            >
              Sepatu
            </Link>
            <Link
              to="/produk?categorySlug=jersey"
              onClick={closeMobileMenu}
              className="px-2 py-1.5 rounded-md hover:bg-accent"
            >
              Jersey
            </Link>
            <Link
              to="/produk?categorySlug=raket"
              onClick={closeMobileMenu}
              className="px-2 py-1.5 rounded-md hover:bg-accent"
            >
              Raket
            </Link>
            <Link
              to="/keranjang"
              onClick={closeMobileMenu}
              className="px-2 py-1.5 rounded-md hover:bg-accent"
            >
              Keranjang Belanja
            </Link>
          </nav>
        </div>
      )}
    </header>
  );
};
