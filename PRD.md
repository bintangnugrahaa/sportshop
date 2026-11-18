# PRD — Sistem E-Commerce Perlengkapan Olahraga Sederhana (SportShop)

| | |
|---|---|
| Versi | 1.0 (draft) |
| Tanggal | 18 November 2026 |
| Status | Siap dikerjakan |

## 1. Latar Belakang & Tujuan

Pemilik toko perlengkapan olahraga membutuhkan toko online sederhana agar pelanggan bisa menelusuri produk, memilih varian (ukuran/warna), membayar secara online, dan melacak pesanan. Admin membutuhkan alat untuk mengelola katalog, stok, dan pesanan tanpa spreadsheet manual.

**Tujuan produk**
1. Pelanggan dapat menyelesaikan pembelian (cari → keranjang → checkout → bayar) dalam kurang dari 3 menit.
2. Stok akurat: tidak ada penjualan melebihi stok (no overselling).
3. Pembayaran otomatis terkonfirmasi lewat Midtrans tanpa intervensi manual.
4. Admin dapat memproses pesanan dari "dibayar" sampai "selesai" lewat dashboard.

**Tujuan teknis**: Clean Architecture, teruji (unit + integrasi), dapat diamati (log, trace, health), dan mudah dideploy dengan Docker.

## 2. Ruang Lingkup

### Dalam lingkup (MVP)
- Registrasi, login, refresh token, logout, lupa/reset password (email), profil.
- Katalog: kategori, brand, produk, varian, gambar, pencarian, filter, sortir, paginasi.
- Keranjang belanja (wajib login, tersimpan di server).
- Buku alamat pengiriman.
- Checkout: ringkasan, ongkir flat per wilayah, pembuatan pesanan, reservasi stok.
- Pembayaran Midtrans Snap + webhook notifikasi + kedaluwarsa otomatis.
- Riwayat dan detail pesanan, pembatalan oleh customer (selama belum dibayar).
- Admin: CRUD kategori/brand/produk/varian, kelola stok, kelola tarif ongkir, kelola pesanan (proses, kirim + input nomor resi, selesai, batal), dashboard ringkasan.
- Email transaksional dasar (konfirmasi pesanan, pembayaran berhasil, pesanan dikirim).

### Di luar lingkup (v1)
Multi-vendor/seller, kupon/voucher, review & rating, wishlist, integrasi API ekspedisi (RajaOngkir/Biteship), refund otomatis lewat Midtrans, multi-bahasa, multi-mata uang, aplikasi mobile native, keranjang tamu (guest), login sosial (Google/Facebook), program loyalitas.

## 3. Persona

| Persona | Deskripsi | Kebutuhan utama |
|---|---|---|
| **Customer** (mis. Rani, 24, pelari) | Belanja lewat HP, ingin cepat dan aman | Cari sepatu ukuran 39, bayar via QRIS/transfer, lacak pesanan |
| **Admin** (mis. Budi, pemilik toko) | Mengelola toko sehari-hari | Update stok, lihat pesanan baru, input resi, lihat omzet harian |

## 4. Kebutuhan Fungsional

Prioritas: **P0** = wajib MVP, **P1** = sebaiknya ada, **P2** = nanti.

### 4.1 Autentikasi & Akun
| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-AUTH-01 | Customer dapat mendaftar dengan nama, email, password, nomor telepon | P0 |
| FR-AUTH-02 | Login menghasilkan access token (JWT, 15 menit) dan refresh token (7 hari) | P0 |
| FR-AUTH-03 | Refresh token dirotasi; reuse token lama mencabut seluruh sesi pengguna | P0 |
| FR-AUTH-04 | Logout mencabut refresh token | P0 |
| FR-AUTH-05 | Lupa & reset password lewat email | P1 |
| FR-AUTH-06 | Konfirmasi email saat pendaftaran | P1 |
| FR-AUTH-07 | Lockout akun setelah 5 kali gagal login (15 menit) | P0 |
| FR-AUTH-08 | Customer dapat melihat dan mengubah profil serta mengganti password | P0 |

### 4.2 Katalog
| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-CAT-01 | Daftar produk dengan paginasi, filter (kategori, brand, rentang harga, ketersediaan), sortir (terbaru, harga naik/turun, nama) | P0 |
| FR-CAT-02 | Pencarian produk berdasarkan nama/deskripsi (full-text PostgreSQL) | P0 |
| FR-CAT-03 | Halaman detail produk: galeri gambar, deskripsi, pilihan varian, harga & stok per varian | P0 |
| FR-CAT-04 | Kategori berhierarki dua tingkat (mis. Sepatu → Sepatu Lari) | P0 |
| FR-CAT-05 | Hasil daftar kategori/brand dan produk populer di-cache di Redis | P1 |

### 4.3 Keranjang
| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-CART-01 | Tambah, ubah jumlah, dan hapus item (per varian) | P0 |
| FR-CART-02 | Jumlah tidak boleh melebihi stok tersedia saat ditambahkan | P0 |
| FR-CART-03 | Keranjang menampilkan perubahan harga/stok sejak item ditambahkan | P1 |
| FR-CART-04 | Satu keranjang aktif per pengguna, dipertahankan antar sesi/perangkat | P0 |

### 4.4 Alamat & Pengiriman
| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-ADDR-01 | CRUD alamat pengiriman; satu alamat bisa ditandai utama | P0 |
| FR-SHIP-01 | Ongkir = `base_fee + ceil(total_berat_kg) * per_kg_fee` berdasarkan provinsi tujuan | P0 |
| FR-SHIP-02 | Admin mengelola tarif per provinsi | P0 |
| FR-SHIP-03 | Berat total dihitung dari berat varian × jumlah | P0 |

### 4.5 Checkout & Pesanan
| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-ORD-01 | Checkout dari keranjang dengan memilih alamat; server menghitung subtotal, ongkir, total | P0 |
| FR-ORD-02 | Saat pesanan dibuat, stok varian dikurangi atomik (reservasi); gagal jika stok tidak cukup | P0 |
| FR-ORD-03 | Harga dan nama produk di-snapshot ke item pesanan | P0 |
| FR-ORD-04 | Nomor pesanan unik berformat `SPT-YYYYMMDD-XXXXXX` | P0 |
| FR-ORD-05 | Customer melihat daftar & detail pesanannya, termasuk riwayat status | P0 |
| FR-ORD-06 | Customer dapat membatalkan pesanan berstatus `PendingPayment` | P0 |
| FR-ORD-07 | Pesanan `PendingPayment` otomatis `Expired` setelah 24 jam; stok dikembalikan | P0 |
| FR-ORD-08 | Checkout idempoten: header `Idempotency-Key` mencegah pesanan ganda saat klik berulang | P1 |

### 4.5b Pembayaran (Midtrans)
| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-PAY-01 | Backend membuat transaksi Snap dan mengembalikan `snap_token` + `redirect_url` | P0 |
| FR-PAY-02 | Frontend membuka Snap (popup) atau redirect; halaman hasil menampilkan status dari server | P0 |
| FR-PAY-03 | Endpoint webhook `POST /api/v1/payments/midtrans/notification` memverifikasi signature dan memperbarui pembayaran + pesanan secara idempoten | P0 |
| FR-PAY-04 | Status pembayaran dikonfirmasi lewat Midtrans Status API sebelum pesanan diubah ke `Paid` | P0 |
| FR-PAY-05 | Customer dapat melanjutkan pembayaran pesanan `PendingPayment` (token Snap dipakai ulang selama valid) | P1 |
| FR-PAY-06 | Job berkala (Hangfire) merekonsiliasi pembayaran `Pending` yang webhook-nya tidak diterima | P1 |
| FR-PAY-07 | Panggilan ke Midtrans memakai retry + circuit breaker + timeout (Polly) | P0 |

### 4.6 Admin
| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-ADM-01 | CRUD kategori, brand | P0 |
| FR-ADM-02 | CRUD produk dan varian, upload gambar, aktif/nonaktif (soft delete) | P0 |
| FR-ADM-03 | Penyesuaian stok manual dengan alasan; tercatat di riwayat pergerakan stok | P0 |
| FR-ADM-04 | Daftar pesanan dengan filter status/tanggal/nomor pesanan | P0 |
| FR-ADM-05 | Ubah status: `Paid → Processing → Shipped` (input kurir + nomor resi) `→ Completed`; batalkan pesanan sebelum dikirim | P0 |
| FR-ADM-06 | Kelola tarif ongkir per provinsi | P0 |
| FR-ADM-07 | Dashboard: pesanan hari ini, omzet 7/30 hari, produk terlaris, stok menipis (≤ 5) | P1 |
| FR-ADM-08 | Daftar pengguna (lihat saja, nonaktifkan akun) | P2 |

### 4.7 Notifikasi
| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-NTF-01 | Email: konfirmasi pesanan dibuat, pembayaran diterima, pesanan dikirim (dikirim via job Hangfire) | P1 |

## 5. Kebutuhan Non-Fungsional

| ID | Kategori | Kebutuhan |
|---|---|---|
| NFR-01 | Performa | p95 endpoint baca katalog < 300 ms pada 100 RPS (data seed 5.000 produk) |
| NFR-02 | Keandalan | Tidak ada overselling meski 50 checkout paralel pada stok 10 (diuji) |
| NFR-03 | Ketersediaan | `/health/live` dan `/health/ready` (cek PostgreSQL, Redis, Hangfire) |
| NFR-04 | Keamanan | Sesuai OWASP ASVS dasar: hash password, rate limit, validasi input, otorisasi ownership, secret di luar repo |
| NFR-05 | Observabilitas | Log terstruktur ke Seq, trace + metrik OpenTelemetry, `TraceId` dikembalikan di ProblemDetails |
| NFR-06 | Resiliensi | Polly: retry eksponensial (3x), circuit breaker, timeout 10 detik pada Midtrans |
| NFR-07 | Kualitas | Cakupan test Domain ≥ 90%, Application ≥ 80%; CI wajib hijau |
| NFR-08 | Portabilitas | `docker compose up` menjalankan seluruh stack (API, DB, Redis, Seq, frontend) |
| NFR-09 | Aksesibilitas & UX | Responsif (mobile-first), skor Lighthouse Accessibility ≥ 90 |
| NFR-10 | Data | Migrasi EF Core terversi; backup database harian (dokumentasi, di luar kode) |

## 6. Aturan Bisnis

### 6.1 Siklus status pesanan

```
PendingPayment ──(bayar sukses)──► Paid ──► Processing ──► Shipped ──► Completed
      │                              │            │
      ├──(customer batal)──► Cancelled ◄──(admin batal)──┘
      └──(24 jam / Midtrans expire)──► Expired
```

| Dari | Ke | Dipicu oleh | Efek samping |
|---|---|---|---|
| PendingPayment | Paid | Webhook/status Midtrans `settlement` (atau `capture` dengan fraud `accept`) | Email pembayaran diterima |
| PendingPayment | Cancelled | Customer | Stok dikembalikan; transaksi Midtrans dibatalkan (best effort) |
| PendingPayment | Expired | Job Hangfire atau Midtrans `expire` | Stok dikembalikan |
| Paid | Processing | Admin | — |
| Paid / Processing | Cancelled | Admin | Stok dikembalikan; refund diproses manual (di luar sistem) dan dicatat di catatan |
| Processing | Shipped | Admin (wajib kurir + nomor resi) | Email pesanan dikirim |
| Shipped | Completed | Admin (atau otomatis 7 hari setelah dikirim, P2) | — |

Transisi lain ditolak dengan kode error `order.invalid_transition`.

### 6.2 Stok
- `stock_quantity` per varian. Checkout mengurangi stok; pembatalan/kedaluwarsa mengembalikan stok (sekali, dijaga oleh status pesanan dan `inventory_movements`).
- Setiap perubahan stok tercatat di `inventory_movements` (alasan: `Sale`, `Cancel`, `Expire`, `ManualAdjust`, `Initial`).

### 6.3 Harga & ongkir
- Harga dalam IDR bulat. `subtotal = Σ(harga_varian × qty)`, `total = subtotal + ongkir`.
- Ongkir: `base_fee + ceil(berat_total_gram / 1000) * per_kg_fee` per provinsi. Provinsi tanpa tarif → checkout ditolak (`shipping.rate_not_found`).
- Minimum berat dihitung 1 kg.

### 6.4 Pembayaran
- Satu pesanan memiliki satu pembayaran aktif. `order_id` yang dikirim ke Midtrans = nomor pesanan.
- Notifikasi duplikat atau datang tidak berurutan tidak boleh merusak status (status akhir tidak bisa mundur).
- Jumlah `gross_amount` dari Midtrans harus sama dengan total pesanan; jika berbeda, tandai `PaymentMismatch` dan kirim alert (log level Error).

### 6.5 Akun
- Email unik (case-insensitive). Admin pertama dibuat lewat seed (kredensial dari environment).

## 7. Alur Pengguna Utama

**Pembelian**: Beranda/Katalog → Detail produk (pilih varian) → Tambah ke keranjang → Keranjang → Checkout (pilih alamat, lihat ongkir) → Buat pesanan → Snap Midtrans → Halaman status pesanan (polling status sampai `Paid`/gagal).

**Pemenuhan (Admin)**: Dashboard → Daftar pesanan `Paid` → Proses → Input kurir & resi → Dikirim → Selesai.

## 8. Ringkasan Halaman

**Customer**: Beranda, Katalog (filter/sort), Detail Produk, Keranjang, Checkout, Status Pembayaran, Pesanan Saya, Detail Pesanan, Alamat, Profil, Login, Daftar, Lupa/Reset Password, 404.
**Admin**: Dashboard, Produk (daftar/form/varian), Kategori, Brand, Stok, Pesanan (daftar/detail), Tarif Ongkir.

## 9. Metrik Keberhasilan

| Metrik | Target |
|---|---|
| Rasio checkout → pembayaran sukses (UAT) | ≥ 90% pada skenario sandbox |
| Kasus overselling di uji konkurensi | 0 |
| Webhook diproses benar (termasuk duplikat) | 100% pada test integrasi |
| Waktu build + test CI | < 10 menit |
| Cakupan test | sesuai NFR-07 |

## 10. Asumsi, Risiko, Ketergantungan

| Item | Keterangan | Mitigasi |
|---|---|---|
| Asumsi | Pasar Indonesia, mata uang IDR, satu gudang | — |
| Asumsi | Midtrans memakai akun Sandbox selama pengembangan | Ganti key lewat konfigurasi saat produksi |
| Risiko | Webhook Midtrans gagal terkirim | Job rekonsiliasi (FR-PAY-06) |
| Risiko | Race condition stok | Update atomik + uji konkurensi |
| Risiko | Penyimpanan gambar di disk container hilang | Volume Docker; abstraksi `IFileStorage` agar mudah pindah ke object storage |
| Risiko | Email belum punya penyedia | Gunakan SMTP (MailHog/Mailpit di dev); abstraksi `IEmailSender` |
| Ketergantungan | Midtrans Snap & Status API, SMTP | Polly + health check opsional |

## 11. Pertanyaan Terbuka (bisa diputuskan saat pengerjaan)
1. Penyimpanan gambar produk: disk lokal (default MVP) atau object storage (S3/MinIO) di fase berikutnya?
2. Penyedia SMTP produksi yang akan dipakai?
3. Apakah selesai otomatis `Shipped → Completed` setelah N hari diperlukan di MVP?
4. Daftar provinsi memakai data statis (34/38 provinsi) yang di-seed — setuju?
