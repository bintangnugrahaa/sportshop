# SportShop

SportShop adalah platform e-commerce perlengkapan olahraga (sepatu, jersey, raket, bola, dan aksesori) yang dibangun dengan pendekatan Clean Architecture, mengedepankan keandalan transaksi stok, keamanan, dan kemudahan pemeliharaan.

## Arsitektur & Teknologi

- **Backend**: .NET (C#), ASP.NET Core Web API, EF Core (PostgreSQL / Npgsql), ASP.NET Core Identity, JWT Bearer, FluentValidation, Mapperly, Redis, Hangfire, Polly, OpenTelemetry, Serilog.
- **Frontend**: React, TypeScript, Vite, Tailwind CSS, shadcn/ui, TanStack Query, Zustand, React Hook Form, Zod.
- **Infrastruktur**: Docker & Docker Compose (PostgreSQL, Redis, Seq, Mailpit).
- **Pengujian**: xUnit, Moq, FluentAssertions, WebApplicationFactory, Testcontainers, Vitest.

## Dokumentasi Proyek

Panduan arsitektur dan spesifikasi lengkap tersedia dalam dokumen berikut:
- [AGENTS.md](file:///d:/laragon/www/sportshop/AGENTS.md) — Panduan pengembang dan aturan kerja coding agent.
- [PRD.md](file:///d:/laragon/www/sportshop/PRD.md) — Product Requirement Document dan kebutuhan sistem.
- [DATABASE.md](file:///d:/laragon/www/sportshop/DATABASE.md) — Skema database PostgreSQL dan aturan data.
- [DESIGN.md](file:///d:/laragon/www/sportshop/DESIGN.md) — Desain teknis arsitektur, API, DevOps, dan UI.
- [TASKS.md](file:///d:/laragon/www/sportshop/TASKS.md) — Roadmap pengerjaan berurutan per fase.

## Panduan Memulai Cepat

### Prasyarat
- .NET 8 / 10 SDK
- Node.js (>= 20) & npm
- Docker Desktop

### Menjalankan Seluruh Stack
```bash
docker compose up -d
```
Layanan lokal:
- Frontend: `http://localhost:5173`
- API & Swagger: `http://localhost:5080/swagger`
- Hangfire Dashboard: `http://localhost:5080/hangfire` (Admin only)
- Seq (Logging): `http://localhost:5341`
- Mailpit: `http://localhost:8025`

## Panduan Pengujian End-to-End Sandbox Midtrans

Bagian ini mendokumentasikan prosedur pengujian manual dan otomatis untuk alur pembayaran Midtrans Snap di lingkungan Sandbox.

### 1. Konfigurasi Lingkungan

Konfigurasikan kunci rahasia Midtrans Sandbox pada `appsettings.Development.json` atau .NET User Secrets:

```json
{
  "Midtrans": {
    "ServerKey": "SB-Mid-server-xxxxxxxxxxxxxx",
    "ClientKey": "SB-Mid-client-xxxxxxxxxxxxxx",
    "IsProduction": false,
    "SnapUrl": "https://app.sandbox.midtrans.com/snap/v1/transactions",
    "ApiUrl": "https://api.sandbox.midtrans.com/v2"
  }
}
```

> [!NOTE]
> Jangan pernah melakukan commit file konfigurasi yang berisi Server Key asli. Gunakan .NET User Secrets di lokal: `dotnet user-secrets set "Midtrans:ServerKey" "<key>" -p src/SportShop.Api`.

### 2. Pengaturan Webhook URL & Tunnel Lokal

Karena Midtrans memerlukan endpoint publik untuk mengirim notifikasi HTTP POST webhook, gunakan Cloudflare Tunnel atau ngrok untuk meneruskan panggilan ke localhost:

```bash
# Menggunakan Cloudflare Tunnel
cloudflared tunnel --url http://localhost:5080

# Atau menggunakan ngrok
ngrok http 5080
```

Salin URL HTTPS publik yang dihasilkan, lalu atur pada **Midtrans Sandbox Dashboard**:
- Masuk ke [Midtrans MAP Sandbox Dashboard](https://dashboard.sandbox.midtrans.com/)
- Buka menu **Settings** -> **Configuration**:
  - **Payment Notification URL**: `https://<public-tunnel-domain>/api/v1/payments/midtrans/notification`
  - **Finish Redirect URL**: `http://localhost:5173/orders/{order_id}/success`
  - **Unfinish Redirect URL**: `http://localhost:5173/orders/{order_id}/pending`
  - **Error Redirect URL**: `http://localhost:5173/orders/{order_id}/error`

### 3. Alat & Simulator Pengujian

- **Simulator Pembayaran**: [Midtrans Payment Simulator](https://simulator.sandbox.midtrans.com/)
  - Virtual Account: BCA, Mandiri (Bill Payment), BNI, BRI, Permata.
  - QRIS / GoPay: Gunakan tombol bayar/simulator QRIS.
  - Kartu Kredit Sandbox:
    - Nomor: `4811 1111 1111 1114`
    - Exp: `12/28`, CVV: `123`, OTP 3DS: `112233`
- **Mailpit**: Periksa email konfirmasi transaksi pada `http://localhost:8025`.
- **Hangfire Dashboard**: Monitor status background jobs & recurring jobs di `http://localhost:5080/hangfire`.

---

### 4. Checklist Skenario Pengujian End-to-End

| Status | Skenario | Langkah Pengujian | Perilaku & Hasil yang Diharapkan |
|:---:|---|---|---|
| [x] | **Skenario 1: Pembayaran Sukses (Settlement)** | 1. Customer melakukan checkout.<br>2. Aplikasi memanggil `POST /api/v1/orders/checkout` & menerima Snap token.<br>3. Bayar via Snap popup / Midtrans Simulator.<br>4. Webhook mengirim `settlement`. | - Signature SHA512 diverifikasi konstan waktu.<br>- Pesanan beralih: `PendingPayment` &rarr; `Paid`.<br>- Stok produk tetap berkurang (reason `Sale`).<br>- Hangfire job `SendEmailJob (OrderPaid)` ter-enqueue & email terkirim di Mailpit. |
| [x] | **Skenario 2: Pembayaran Kedaluwarsa (Expire)** | 1. Customer checkout order.<br>2. Biarkan transaksi melewati batas waktu atau picu status `expire` di simulator.<br>3. Webhook menerima notifikasi `expire` (atau `ReconcilePaymentsJob` berjalan). | - Pesanan beralih: `PendingPayment` &rarr; `Expired`.<br>- Stok semua varian item pesanan dikembalikan secara atomik (`RestoreStockAtomicAsync`).<br>- `InventoryMovement` tercatat dengan reason `Expire`. |
| [x] | **Skenario 3: Pembatalan Transaksi (Cancel)** | 1. Customer membatalkan transaksi pada Snap popup atau dibatalkan via dashboard Midtrans.<br>2. Webhook menerima notifikasi `cancel`. | - Pesanan beralih: `PendingPayment` &rarr; `Cancelled`.<br>- Stok dikembalikan secara atomik ke kuantitas semula.<br>- `InventoryMovement` tercatat dengan reason `Cancel`. |
| [x] | **Skenario 4: Idempotensi & Duplikasi Webhook** | 1. Simulasikan pengiriman webhook yang sama berulang kali (2x atau lebih). | - Dedup key mendeteksi notifikasi duplikat.<br>- Sistem mengembalikan HTTP 200 OK.<br>- Tidak ada perubahan status ganda atau pengiriman email berulang. |
| [x] | **Skenario 5: Rekonsiliasi Otomatis (ReconcilePaymentsJob)** | 1. Matikan tunnel webhook (simulasikan webhook gagal/terlewat).<br>2. Selesaikan pembayaran di simulator.<br>3. Tunggu atau jalankan job `ReconcilePaymentsJob` di Hangfire. | - Job memindai transaksi pending > 15 menit.<br>- Job memanggil Midtrans Status API (`GET /v2/{order_id}/status`).<br>- Status lokal otomatis tersinkronkan ke `Paid` & email notifikasi dikirimkan. |
| [x] | **Skenario 6: Deteksi Amount Mismatch** | 1. Kirim notifikasi palsu dengan nominal (`gross_amount`) berbeda dari total pesanan. | - Sistem mendeteksi selisih `Math.Abs(order.Total - grossAmount) > 0.01`.<br>- Status pesanan tidak diubah.<br>- Insiden mismatch disimpan di tabel `payment_notifications` & log peringatan dicatat. |

