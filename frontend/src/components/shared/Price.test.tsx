import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Price } from './Price';

describe('Price Component', () => {
  it('renders single price formatted in IDR', () => {
    render(<Price price={150000} />);
    expect(screen.getByText(/150\.000/)).toBeInTheDocument();
  });

  it('renders price range when min and max differ', () => {
    render(<Price minPrice={100000} maxPrice={200000} />);
    expect(screen.getByText(/100\.000/)).toBeInTheDocument();
    expect(screen.getByText(/200\.000/)).toBeInTheDocument();
  });

  it('renders original price strikethrough if higher', () => {
    render(<Price price={120000} originalPrice={150000} />);
    expect(screen.getByText(/120\.000/)).toBeInTheDocument();
    expect(screen.getByText(/150\.000/)).toBeInTheDocument();
  });
});
