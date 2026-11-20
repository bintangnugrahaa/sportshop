import React from 'react';
import { Link, Outlet } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

export const AuthLayout: React.FC = () => {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-muted/30 px-4 py-12 sm:px-6 lg:px-8">
      <div className="w-full max-w-md">
        <div className="mb-6 flex items-center justify-between">
          <Link
            to="/"
            className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            Kembali ke Beranda
          </Link>
          <Link to="/" className="flex items-center gap-1.5 font-bold text-primary">
            <span className="flex h-6 w-6 items-center justify-center rounded bg-primary text-xs font-black text-white">
              S
            </span>
            <span>SportShop</span>
          </Link>
        </div>
        <Outlet />
      </div>
    </div>
  );
};
