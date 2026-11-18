import React from 'react';

export const App: React.FC = () => {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center p-4">
      <header className="text-center">
        <h1 className="text-3xl font-bold tracking-tight text-primary">SportShop</h1>
        <p className="mt-2 text-muted-foreground">
          Platform E-Commerce Perlengkapan Olahraga Sederhana &amp; Andal
        </p>
      </header>
    </div>
  );
};

export default App;
