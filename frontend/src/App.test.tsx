import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('App', () => {
  it('renders SportShop brand title', async () => {
    render(<App />);
    const brandElements = await screen.findAllByText('SportShop');
    expect(brandElements.length).toBeGreaterThanOrEqual(1);
  });
});
