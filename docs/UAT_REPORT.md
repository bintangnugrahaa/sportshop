# Laporan User Acceptance Testing (UAT) — SportShop

| Metadata              | Nilai                                                                                                                |
| --------------------- | -------------------------------------------------------------------------------------------------------------------- |
| **Aplikasi**          | SportShop E-Commerce System                                                                                          |
| **Versi UAT**         | v1.0.0-rc1                                                                                                           |
| **Tanggal Eksekusi**  | 21 November 2026                                                                                                     |
| **Lingkungan Uji**    | Testcontainers (PostgreSQL 17 Alpine, Redis 7 Alpine), ASP.NET Core 8 Web API, Midtrans Mock & Real Signature Engine |
| **Hasil Keseluruhan** | **100% LULUS (4 Skenario Penuh Lolos / 0 Gagal)**                                                                    |

---

## 1. Ringkasan Eksekusi UAT

Pengujian Penerimaan Pengguna (User Acceptance Testing / UAT) ini memvalidasi siklus penuh proses bisnis SportShop dari sudut pandang Pelanggan, Sistem Gateway Pembayaran (Midtrans), dan Administrator Toko. Seluruh skenario dijalankan secara otomatis end-to-end melalui suite `SportShop.Api.IntegrationTests.FullUatScenarioTests`.

| ID         | Skenario Pengujian                                                                      |   Hasil    | Durasi | Keterangan                                                                                                                                                                                     |
| ---------- | --------------------------------------------------------------------------------------- | :--------: | :----: | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **UAT-01** | **Siklus Pembelian Penuh**: Beli &rarr; Bayar &rarr; Proses &rarr; Kirim &rarr; Selesai | **PASSED** | 4.2 s  | Reservasi stok atomik, verifikasi signature webhook SHA512, transisi status `PendingPayment` &rarr; `Paid` &rarr; `Processing` &rarr; `Shipped` &rarr; `Completed`, input no resi & kurir JNE. |
| **UAT-02** | **Kedaluwarsa Pembayaran (Expire)**: Stok Kembali Atomik                                | **PASSED** | 0.8 s  | Transaksi melewati batas waktu & webhook mengirim status `expire`. Status pesanan beralih ke `Expired`, stok produk dikembalikan secara atomik ke jumlah awal (reason `Expire`).               |
| **UAT-03** | **Pembatalan Pesanan Pelanggan (Cancel)**: Stok Kembali Atomik                          | **PASSED** | 0.7 s  | Pelanggan membatalkan pesanan berstatus `PendingPayment`. Status pesanan menjadi `Cancelled`, kuantitas stok dikembalikan secara atomik (reason `Cancel`).                                     |
| **UAT-04** | **Idempotensi Webhook & Duplikasi Callback**                                            | **PASSED** | 0.5 s  | Pengiriman webhook yang sama berulang kali (2x) dan pengiriman notifikasi out-of-order (`pending` setelah `settlement`). Status tetap `Paid`, stok tidak terpotong ulang, respons HTTP 200 OK. |

---

## 2. Rincian Eksekusi Per Skenario

### UAT-01: Siklus Pembelian Penuh (Beli &rarr; Bayar &rarr; Proses &rarr; Kirim &rarr; Selesai)

- **Aktor**: Customer & Admin Toko
- **Langkah-langkah**:
    1. Pelanggan mendaftar akun baru (`uat_customer_*@sportshop.dev`) dan masuk (login).
    2. Pelanggan memilih varian produk (Sepatu Lari, ukuran 42, warna Hitam/Oranye, harga Rp 300.000). Stok awal: 10 unit.
    3. Pelanggan menambahkan 2 unit ke keranjang belanja dan mengisi alamat pengiriman (Bandung, Jawa Barat).
    4. Pelanggan melakukan checkout dengan header `Idempotency-Key`.
        - _Verifikasi_: Pesanan terbuat dengan status `PendingPayment`, token Midtrans Snap terbentuk, stok varian otomatis berkurang secara atomik menjadi 8 unit.
    5. Sistem menerima webhook notifikasi pembayaran `settlement` dari Midtrans dengan signature SHA-512 yang valid:
       `SHA512(order_id + status_code + gross_amount + ServerKey)`
        - _Verifikasi_: Signature diverifikasi dengan konstan-waktu, pesanan berpindah ke status `Paid`, field `PaidAt` terisi timestamp UTC.
    6. Admin masuk (`admin@sportshop.com`) dan memproses pesanan:
        - `POST /api/v1/admin/orders/{orderNumber}/process`
        - _Verifikasi_: Status berubah menjadi `Processing` ("Pesanan sedang dipacking oleh staf gudang").
    7. Admin mengirim pesanan:
        - `POST /api/v1/admin/orders/{orderNumber}/ship` dengan Kurir `JNE Express` dan No. Resi `JNE-UAT-99887766`.
        - _Verifikasi_: Status berubah menjadi `Shipped`, tracking information tersimpan.
    8. Admin / Sistem menyelesaikan pesanan:
        - `POST /api/v1/admin/orders/{orderNumber}/complete`
        - _Verifikasi_: Status pesanan beralih ke `Completed`. Riwayat status (`order_status_histories`) mencatat urutan lengkap: `PendingPayment` &rarr; `Paid` &rarr; `Processing` &rarr; `Shipped` &rarr; `Completed`.

---

### UAT-02: Kedaluwarsa Pembayaran (Expire) & Pemulihan Stok

- **Aktor**: Customer & Background Worker / Midtrans Webhook
- **Langkah-langkah**:
    1. Pelanggan melakukan checkout pesanan sebanyak 3 unit dari stok awal 15 unit.
        - _Verifikasi_: Stok berkurang menjadi 12 unit.
    2. Transaksi melewati batas waktu bayar (24 jam) atau Midtrans mengirim webhook dengan status `expire`.
    3. Sistem memproses notifikasi kedaluwarsa secara atomik:
        - Status pesanan berubah menjadi `Expired`.
        - Kuantitas stok produk varian dikembalikan utuh tepat sekali menjadi 15 unit (`RestoreStockAtomicAsync`).
        - Entri `inventory_movements` mencatat penambahan stok +3 dengan alasan `StockReason.Expire`.
    4. Sistem menolak upaya pembayaran ulang untuk pesanan yang telah expired.

---

### UAT-03: Pembatalan Pesanan oleh Pelanggan (Cancel)

- **Aktor**: Customer
- **Langkah-langkah**:
    1. Pelanggan melakukan pemesanan sebanyak 5 unit dari stok awal 25 unit.
        - _Verifikasi_: Stok berkurang menjadi 20 unit.
    2. Sebelum melakukan pembayaran (status masih `PendingPayment`), pelanggan membatalkan pesanan melalui:
       `POST /api/v1/orders/{orderNumber}/cancel` dengan alasan `"Berubah pikiran, ingin mengganti varian warna lain"`.
    3. Sistem membatalkan pesanan secara atomik:
        - Status pesanan berpindah ke `Cancelled`.
        - Stok produk varian otomatis dikembalikan menjadi 25 unit.
        - Entri `inventory_movements` mencatat mutasi +5 dengan alasan `StockReason.Cancel`.
    4. Transisi status ilegal ditolak (pesanan yang sudah batal tidak dapat dibatalkan kembali atau dibayar).

---

### UAT-04: Idempotensi Webhook & Penanganan Notifikasi Duplikat

- **Aktor**: Midtrans Callback Engine
- **Langkah-langkah**:
    1. Pelanggan memesan 1 unit barang (stok awal 10 &rarr; 9).
    2. Webhook callback pertama diterima: HTTP 200 OK, status order berubah ke `Paid`.
    3. Webhook callback kedua dengan muatan payload identik dikirim ulang (simulasi retry jaringan Midtrans):
        - _Verifikasi_: Sistem mendeteksi duplikasi secara idempoten via `dedup_key`.
        - Mengembalikan HTTP 200 OK.
        - Status order tetap `Paid`, tidak ada duplikasi riwayat status atau pemotongan stok ganda.
    4. Simulasi notifikasi out-of-order: notifikasi `pending` tiba terlambat setelah `settlement`:
        - _Verifikasi_: Sistem mengabaikan regresi status lama, pesanan tetap berstatus `Paid`.

---

## 3. Kesimpulan & Rekomendasi

Seluruh kebutuhan **P0** pada `PRD.md` dan kriteria penerimaan **T11-04** pada `TASKS.md` telah divalidasi dan terbukti berfungsi dengan benar tanpa inkonsistensi stok (_zero overselling_, _atomic rollback_). Sistem dinyatakan **LULUS UAT** dan siap melanjutkan ke tahap **Checklist Go-Live (T11-05)**.
