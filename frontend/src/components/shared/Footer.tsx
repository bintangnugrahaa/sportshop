import React from 'react';
import { Link } from 'react-router-dom';

export const Footer: React.FC = () => {
  return (
    <footer className="border-t bg-muted/40 text-muted-foreground">
      <div className="container mx-auto px-4 py-12 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 gap-8 md:grid-cols-4">
          <div className="space-y-3">
            <div className="flex items-center gap-2 text-lg font-bold text-foreground">
              <span className="flex h-7 w-7 items-center justify-center rounded bg-primary text-white text-sm font-black">
                S
              </span>
              SportShop
            </div>
            <p className="text-sm">
              Toko online perlengkapan olahraga terpercaya untuk sepatu, jersey, raket, bola, dan aksesori olahraga.
            </p>
          </div>

          <div>
            <h4 className="font-semibold text-foreground text-sm mb-3">Kategori</h4>
            <ul className="space-y-2 text-sm">
              <li>
                <Link to="/produk?categorySlug=sepatu" className="hover:text-foreground">
                  Sepatu Olahraga
                </Link>
              </li>
              <li>
                <Link to="/produk?categorySlug=jersey" className="hover:text-foreground">
                  Jersey &amp; Pakaian
                </Link>
              </li>
              <li>
                <Link to="/produk?categorySlug=raket" className="hover:text-foreground">
                  Raket Badminton &amp; Tenis
                </Link>
              </li>
              <li>
                <Link to="/produk?categorySlug=aksesori" className="hover:text-foreground">
                  Aksesori &amp; Bola
                </Link>
              </li>
            </ul>
          </div>

          <div>
            <h4 className="font-semibold text-foreground text-sm mb-3">Bantuan &amp; Info</h4>
            <ul className="space-y-2 text-sm">
              <li>
                <Link to="/produk" className="hover:text-foreground">
                  Cara Belanja
                </Link>
              </li>
              <li>
                <Link to="/produk" className="hover:text-foreground">
                  Tarif Ongkos Kirim
                </Link>
              </li>
              <li>
                <Link to="/produk" className="hover:text-foreground">
                  Kebijakan Garansi
                </Link>
              </li>
              <li>
                <Link to="/produk" className="hover:text-foreground">
                  Hubungi Kami
                </Link>
              </li>
            </ul>
          </div>

          <div>
            <h4 className="font-semibold text-foreground text-sm mb-3">Metode Pembayaran</h4>
            <p className="text-sm leading-relaxed">
              Mendukung QRIS, GoPay, ShopeePay, Virtual Account BCA/BNI/BRI/Mandiri via Midtrans Snap yang aman dan instan.
            </p>
          </div>
        </div>

        <div className="mt-8 border-t pt-8 flex flex-col sm:flex-row items-center justify-between text-xs">
          <p>&copy; {new Date().getFullYear()} SportShop. Seluruh hak cipta dilindungi.</p>
          <p className="mt-2 sm:mt-0">Dibuat dengan dedikasi untuk atlet dan pecinta olahraga Indonesia.</p>
        </div>
      </div>
    </footer>
  );
};
