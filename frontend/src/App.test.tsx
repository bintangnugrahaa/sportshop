import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('App', () => {
  it('renders SportShop brand title', () => {
    render(<App />);
    expect(screen.getByText('SportShop')).toBeInTheDocument();
  });
});
