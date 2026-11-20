import React from 'react';
import { Outlet } from 'react-router-dom';
import { Navbar } from '@/components/shared/Navbar';
import { Footer } from '@/components/shared/Footer';

export const RootLayout: React.FC = () => {
  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground antialiased">
      <Navbar />
      <main className="flex-1">
        <Outlet />
      </main>
      <Footer />
    </div>
  );
};
