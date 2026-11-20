import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StatusBadge } from './StatusBadge';

describe('StatusBadge Component', () => {
  it('renders Indonesian label for PendingPayment', () => {
    render(<StatusBadge status="PendingPayment" />);
    expect(screen.getByText('Menunggu Pembayaran')).toBeInTheDocument();
  });

  it('renders Indonesian label for Paid', () => {
    render(<StatusBadge status="Paid" />);
    expect(screen.getByText('Dibayar')).toBeInTheDocument();
  });

  it('renders Indonesian label for Completed', () => {
    render(<StatusBadge status="Completed" />);
    expect(screen.getByText('Selesai')).toBeInTheDocument();
  });
});
