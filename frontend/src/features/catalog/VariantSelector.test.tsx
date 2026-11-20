import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { VariantSelector } from './components/VariantSelector';
import { ProductVariant } from '@/types';

const variantsWithStockAndZero: ProductVariant[] = [
  {
    id: 'v-39',
    productId: 'p1',
    sku: 'NIKE-PEG-39',
    optionSize: '39',
    optionColor: 'Hitam',
    price: 1899000,
    weightGram: 300,
    stockQuantity: 5,
    isActive: true,
  },
  {
    id: 'v-40',
    productId: 'p1',
    sku: 'NIKE-PEG-40',
    optionSize: '40',
    optionColor: 'Hitam',
    price: 1899000,
    weightGram: 310,
    stockQuantity: 0, // Out of stock!
    isActive: true,
  },
  {
    id: 'v-41',
    productId: 'p1',
    sku: 'NIKE-PEG-41',
    optionSize: '41',
    optionColor: 'Hitam',
    price: 1999000,
    weightGram: 320,
    stockQuantity: 10,
    isActive: true,
  },
];

describe('VariantSelector Component (T9-05)', () => {
  it('disables variant buttons when stock is 0 (acceptance criteria: Varian stok 0 nonaktif)', () => {
    const onSelectMock = vi.fn();

    render(
      <VariantSelector
        variants={variantsWithStockAndZero}
        selectedVariant={variantsWithStockAndZero[0] || null}
        onSelectVariant={onSelectMock}
      />,
    );

    // Option 39 has stock 5 -> should be enabled
    const btn39 = screen.getByRole('button', { name: /39/i });
    expect(btn39).toBeEnabled();

    // Option 40 has stock 0 -> MUST BE DISABLED
    const btn40 = screen.getByRole('button', { name: /40/i });
    expect(btn40).toBeDisabled();
    expect(btn40).toHaveClass('line-through');

    // Option 41 has stock 10 -> should be enabled
    const btn41 = screen.getByRole('button', { name: /41/i });
    expect(btn41).toBeEnabled();
  });

  it('clicking an in-stock variant triggers onSelectVariant callback', async () => {
    const user = userEvent.setup();
    const onSelectMock = vi.fn();

    render(
      <VariantSelector
        variants={variantsWithStockAndZero}
        selectedVariant={variantsWithStockAndZero[0] || null}
        onSelectVariant={onSelectMock}
      />,
    );

    const btn41 = screen.getByRole('button', { name: /41/i });
    await user.click(btn41);

    expect(onSelectMock).toHaveBeenCalledWith(variantsWithStockAndZero[2]);
  });

  it('displays SKU, weight in gram, and stock count for selected variant', () => {
    render(
      <VariantSelector
        variants={variantsWithStockAndZero}
        selectedVariant={variantsWithStockAndZero[0] || null}
        onSelectVariant={vi.fn()}
      />,
    );

    expect(screen.getByText('NIKE-PEG-39')).toBeInTheDocument();
    expect(screen.getByText('300 g')).toBeInTheDocument();
    expect(screen.getByText(/Sisa 5 unit/i)).toBeInTheDocument();
  });
});
