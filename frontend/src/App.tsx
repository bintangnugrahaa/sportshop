import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { Providers } from '@/app/providers';
import { AppRoutes } from '@/routes';

export const App: React.FC = () => {
  return (
    <Providers>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </Providers>
  );
};

export default App;
