# SportShop — Platform E-Commerce Perlengkapan Olahraga

SportShop adalah sistem toko online tunggal (*single-merchant*) untuk perlengkapan olahraga (sepatu, jersey, raket, bola, dan aksesori) yang dibangun dengan standar industri, **Clean Architecture**, keandalan transaksi stok atomik (*no overselling*), keamanan ketat, serta integrasi gateway pembayaran **Midtrans Snap**.

---

## Daftar Isi
- [Arsitektur & Tech Stack](#arsitektur--tech-stack)
- [Dokumentasi Perencanaan & Spesifikasi](#dokumentasi-perencanaan--spesifikasi)
- [Panduan Memulai Cepat (< 15 Menit)](#panduan-memulai-cepat--15-menit)
  - [Prasyarat Sistem](#prasyarat-sistem)
  - [Opsi 1: Menjalankan dengan Docker Compose (Direkomendasikan)](#opsi-1-menjalankan-dengan-docker-compose-direkomendasikan)
  - [Opsi 2: Menjalankan Secara Bare-Metal (Development)](#opsi-2-menjalankan-secara-bare-metal-development)
- [Variabel Lingkungan (Environment Variables)](#variabel-lingkungan-environment-variables)
- [Migrasi Database & Seeding Data](#migrasi-database--seeding-data)
- [Panduan Pengujian End-to-End Sandbox Midtrans](#panduan-pengujian-end-to-end-sandbox-midtrans)
  - [1. Konfigurasi Kunci API & Webhook Tunnel](#1-konfigurasi-kunci-api--webhook-tunnel)
  - [2. Simulator & Pembayaran Sandbox](#2-simulator--pembayaran-sandbox)
  - [3. Checklist Skenario Pengujian Midtrans](#3-checklist-skenario-pengujian-midtrans)
- [Menjalankan Pengujian Otomatis (Testing)](#menjalankan-pengujian-otomatis-testing)
  - [Testing Backend (.NET)](#testing-backend-net)
  - [Testing Frontend (React + Vitest)](#testing-frontend-react--vitest)
- [Observabilitas & Monitoring Lokal](#observabilitas--monitoring-lokal)
- [Panduan Rilis Produksi (Docker Compose)](#panduan-rilis-produksi-docker-compose)

---

## Arsitektur & Tech Stack

```
                  ┌─────────────────────────────────────────┐
                  │          React SPA (Vite + TS)          │
                  │   Tailwind CSS + shadcn/ui + TanStack   │
                  └────────────────────┬────────────────────┘
                                       │ HTTPS / JSON / JWT
                                       ▼
                  ┌─────────────────────────────────────────┐
                  │       ASP.NET Core Web API (.NET 8)     │
                  │        Clean Architecture Solution      │
                  └───┬──────────┬──────────┬───────────┬───┘
                      │          │          │           │
       PostgreSQL 17 ◄┘   Redis ◄┘    Seq ◄─┘  Midtrans ◄┘
         (EF Core)        (Cache)  (Logs/OTel)   (Snap)
```

- **Backend**: C# 12 / .NET 8, ASP.NET Core Web API, EF Core (Npgsql), PostgreSQL 17, ASP.NET Core Identity, JWT Bearer (rotasi & deteksi reuse), FluentValidation, Mapperly, Redis 7 (cache-aside), Hangfire (PostgreSQL storage), Polly HTTP resilience, OpenTelemetry & Serilog (Seq sink), HealthChecks.
- **Frontend**: React 18, TypeScript strict, Vite, Tailwind CSS, shadcn/ui, TanStack Query v5, Zustand, React Hook Form, Zod, Axios dengan interceptor auto-refresh token.
- **Infrastruktur**: Multi-stage non-root Dockerfile, Nginx reverse proxy unprivileged, Docker Compose (Dev & Prod), GitHub Actions CI/CD (GHCR).
- **Pengujian**: xUnit, Moq, FluentAssertions, WebApplicationFactory, Testcontainers (PostgreSQL & Redis), Vitest, React Testing Library.

---

## Dokumentasi Perencanaan & Spesifikasi

Arsitektur dan kontrak sistem secara detail terdokumentasi dalam dokumen berikut:
- [AGENTS.md](file:///d:/laragon/www/sportshop/AGENTS.md) — Aturan kerja tim dan coding agent, standar Clean Architecture, konvensi kode, dan Definition of Done.
- [PRD.md](file:///d:/laragon/www/sportshop/PRD.md) — Kebutuhan produk, batasan MVP, persona, diagram transisi status pesanan, dan Non-Functional Requirements.
- [DATABASE.md](file:///d:/laragon/www/sportshop/DATABASE.md) — Desain skema basis data PostgreSQL, relasi ERD, konvensi penamaan snake_case, dan optimistik konkurensi (xmin).
- [DESIGN.md](file:///d:/laragon/www/sportshop/DESIGN.md) — Kontrak API lengkap (RFC 7807 ProblemDetails), desain frontend UI, alur checkout Midtrans, dan DevOps.
- [TASKS.md](file:///d:/laragon/www/sportshop/TASKS.md) — Rincian tugas per fase pengerjaan dari Fase 0 hingga Fase 11.

---

## Panduan Memulai Cepat (< 15 Menit)

### Prasyarat Sistem
Pastikan perangkat Anda memiliki perangkat lunak berikut:
- **Docker Desktop** (atau Docker Engine + Compose v2)
- *.NET 8 SDK* (opsional jika hanya menjalankan via Docker)
- *Node.js 20+ & npm* (opsional jika hanya menjalankan via Docker)

---

### Opsi 1: Menjalankan dengan Docker Compose (Direkomendasikan)

Cara termudah dan tercepat untuk menjalankan seluruh ekosistem SportShop:

1. **Clone repository dan siapkan environment**:
   ```bash
   git clone <repo-url> sportshop
   cd sportshop
   cp .env.example .env
   ```

2. **Jalankan seluruh kontainer**:
   ```bash
   docker compose up -d --build
   ```

3. **Akses antarmuka layanan**:
   - **Frontend (Toko & Admin)**: `http://localhost:5173`
   - **Swagger / OpenAPI**: `http://localhost:5080/swagger`
   - **Hangfire Dashboard (Admin Only)**: `http://localhost:5080/hangfire`
   - **Seq Observability & Log Dashboard**: `http://localhost:5341`
   - **Mailpit Web Email Inbox**: `http://localhost:8025`

4. **Kredensial Bawaan (Default Seed Data)**:
   - **Akun Admin**: `admin@sportshop.dev` / `Admin123!`
   - **Database Postgres**: `localhost:5432` (User: `postgres`, Pass: `postgres`, DB: `sportshop`)
   - **Katalog Demo**: ±30 produk olahraga, varian ukuran/warna, kategori, dan brand sudah otomatis terisi saat pertama kali start.

---

### Opsi 2: Menjalankan Secara Bare-Metal (Development)

Jika ingin mengembangkan backend dan frontend secara aktif dengan hot-reload:

1. **Jalankan dependensi pihak ketiga via Docker**:
   ```bash
   docker compose up -d db redis seq mailpit
   ```

2. **Jalankan Backend API**:
   ```bash
   cd backend
   dotnet restore
   dotnet run --project src/SportShop.Api
   ```
   API akan mendengarkan pada `http://localhost:5080` (atau port profil yang dikonfigurasi).

3. **Jalankan Frontend React**:
   ```bash
   cd frontend
   npm ci
   npm run dev
   ```
   Vite dev server akan aktif di `http://localhost:5173`.

---

## Variabel Lingkungan (Environment Variables)

File `.env.example` telah disediakan di root repositori. Berikut penjelasan setiap parameter:

| Variabel | Deskripsi | Default Dev |
|---|---|---|
| `POSTGRES_USER` | Username PostgreSQL | `postgres` |
| `POSTGRES_PASSWORD` | Password PostgreSQL | `postgres` |
| `POSTGRES_DB` | Nama database | `sportshop` |
| `ConnectionStrings__Default` | ADO.NET Connection String | `Host=db;Port=5432;...` |
| `Redis__ConnectionString` | Host & port Redis cache | `redis:6379` |
| `Jwt__Key` | Kunci simetris penandatanganan JWT (min. 32 char) | *(Kunci rahasia dev)* |
| `Jwt__Issuer` | Issuer JWT | `SportShopApi` |
| `Jwt__Audience` | Audience JWT | `SportShopClient` |
| `Midtrans__ServerKey` | Server Key dari Midtrans Sandbox | `SB-Mid-server-...` |
| `Midtrans__ClientKey` | Client Key dari Midtrans Sandbox | `SB-Mid-client-...` |
| `Midtrans__IsProduction` | Flag mode produksi Midtrans | `false` |
| `Cors__Origins` | Origin frontend yang diizinkan (koma sebagai pemisah) | `http://localhost:5173,http://localhost:80` |
| `Seq__ServerUrl` | Endpoint UI Seq untuk sink Serilog | `http://seq:5341` |
| `Otel__OtlpEndpoint` | Endpoint OTLP HTTP/gRPC untuk trace EF & HTTP | `http://seq:5341/ingest/otlp` |
| `Smtp__Host` | Host SMTP untuk pengiriman email transaksional | `mailpit` |
| `Smtp__Port` | Port SMTP | `1025` |
| `Smtp__From` | Alamat pengirim email sistem | `no-reply@sportshop.dev` |
| `SEED_ADMIN_EMAIL` | Email akun administrator awal | `admin@sportshop.dev` |
| `SEED_ADMIN_PASSWORD` | Password akun administrator awal | `Admin123!` |
| `SEED_DEMO_DATA` | Flag otomatis mengisi data demo katalog | `true` (di override dev) |
| `Order__ExpiryHours` | Batas kedaluwarsa pesanan yang belum dibayar | `24` |

---

## Migrasi Database & Seeding Data

Basis data dikelola menggunakan Entity Framework Core migrations di dalam proyek `SportShop.Infrastructure`.

### Menjalankan Migrasi Manual
```bash
# Menambah migrasi baru (dari folder backend)
dotnet ef migrations add <NamaMigrasi> -p src/SportShop.Infrastructure -s src/SportShop.Api

# Mengaplikasikan migrasi ke basis data
dotnet ef database update -p src/SportShop.Infrastructure -s src/SportShop.Api
```

### Auto-Migration & Seeding
Secara default saat container dijalankan dengan environment `AutoMigrate=true`, aplikasi akan:
1. Memastikan migrasi EF Core terbaru terpasang di database secara otomatis saat booting.
2. Melakukan seed peran bawaan (`Admin`, `Customer`).
3. Membuat akun admin awal berdasarkan `SEED_ADMIN_EMAIL` dan `SEED_ADMIN_PASSWORD`.
4. Jika `SEED_DEMO_DATA=true`, sistem akan melakukan seed kategori hierarkis, brand olahraga ternama, dan puluhan varian produk beserta stok awalnya.

---

## Panduan Pengujian End-to-End Sandbox Midtrans

Platform SportShop telah terintegrasi penuh dengan Midtrans Snap API menggunakan alur pembayaran aman, verifikasi signature SHA512 waktu-konstan, dan penanganan webhook yang idempoten.

### 1. Konfigurasi Kunci API & Webhook Tunnel

1. **Dapatkan Kunci Sandbox**:
   - Masuk ke [Midtrans Sandbox MAP](https://dashboard.sandbox.midtrans.com/).
   - Buka menu **Settings** &rarr; **Access Keys**.
   - Salin **Client Key** dan **Server Key**.
   - Simpan pada `.env` atau jalankan via .NET User Secrets:
     ```bash
     dotnet user-secrets set "Midtrans:ServerKey" "SB-Mid-server-xxxx" -p backend/src/SportShop.Api
     dotnet user-secrets set "Midtrans:ClientKey" "SB-Mid-client-xxxx" -p backend/src/SportShop.Api
     ```

2. **Ekspos Webhook Lokal**:
   Gunakan Cloudflare Tunnel atau ngrok untuk menerima webhook callback dari server Midtrans:
   ```bash
   # Opsi A: Cloudflare Tunnel (bebas batasan sesi)
   cloudflared tunnel --url http://localhost:5080

   # Opsi B: ngrok
   ngrok http 5080
   ```

3. **Atur Notification URL di Midtrans**:
   Pada dashboard Midtrans **Settings** &rarr; **Configuration**:
   - **Payment Notification URL**: `https://<tunnel-anda>/api/v1/payments/midtrans/notification`
   - **Finish Redirect URL**: `http://localhost:5173/checkout/status/{order_id}`
   - **Unfinish Redirect URL**: `http://localhost:5173/checkout/status/{order_id}`
   - **Error Redirect URL**: `http://localhost:5173/checkout/status/{order_id}`

---

### 2. Simulator & Pembayaran Sandbox

Gunakan [Midtrans Payment Simulator](https://simulator.sandbox.midtrans.com/) untuk mensimulasikan berbagai metode bayar:
- **Virtual Account (BCA/BNI/BRI/Mandiri/Permata)**: Salin nomor VA dari popup Snap ke simulator lalu klik Inquire & Pay.
- **QRIS / GoPay**: Scan QR kode menggunakan simulator QRIS.
- **Kartu Kredit Testing**:
  - No Kartu: `4811 1111 1111 1114`
  - Masa Berlaku: Bulan/Tahun masa depan (mis. `12/28`)
  - CVV: `123`
  - OTP 3D-Secure: `112233`

---

### 3. Checklist Skenario Pengujian Midtrans

| Status | Skenario | Langkah Pengujian | Perilaku & Hasil yang Diharapkan |
|:---:|---|---|---|
| [x] | **Skenario 1: Pembayaran Sukses (Settlement)** | 1. Customer checkout order.<br>2. Aplikasi memanggil `POST /api/v1/orders/checkout` & menerima Snap token.<br>3. Bayar via simulator.<br>4. Webhook mengirim `settlement`. | - Signature SHA512 diverifikasi konstan waktu.<br>- Status pesanan beralih: `PendingPayment` &rarr; `Paid`.<br>- Stok produk tetap berkurang (reason `Sale`).<br>- Hangfire job `SendEmailJob (OrderPaid)` ter-enqueue & email terlihat di Mailpit. |
| [x] | **Skenario 2: Pembayaran Kedaluwarsa (Expire)** | 1. Customer checkout order.<br>2. Biarkan transaksi kedaluwarsa atau picu status `expire` di simulator.<br>3. Webhook menerima `expire` (atau `ExpireUnpaidOrdersJob` berjalan). | - Status pesanan beralih: `PendingPayment` &rarr; `Expired`.<br>- Stok semua varian item pesanan dikembalikan secara atomik (`RestoreStockAtomicAsync`).<br>- `InventoryMovement` tercatat dengan reason `Expire`. |
| [x] | **Skenario 3: Pembatalan Transaksi (Cancel)** | 1. Customer membatalkan pembayaran di Snap atau via Midtrans dashboard.<br>2. Webhook menerima `cancel`. | - Status pesanan beralih: `PendingPayment` &rarr; `Cancelled`.<br>- Stok dikembalikan secara atomik ke kuantitas semula.<br>- `InventoryMovement` tercatat dengan reason `Cancel`. |
| [x] | **Skenario 4: Idempotensi & Duplikasi Webhook** | 1. Simulasikan pengiriman webhook yang sama berulang kali (2x atau lebih). | - Dedup key mendeteksi notifikasi duplikat.<br>- Sistem mengembalikan HTTP 200 OK.<br>- Tidak ada perubahan status ganda atau pengiriman email ganda. |
| [x] | **Skenario 5: Rekonsiliasi Otomatis (ReconcilePaymentsJob)** | 1. Simulasikan webhook mati saat bayar di simulator.<br>2. Jalankan background job rekonsiliasi. | - Job memindai transaksi pending > 15 menit.<br>- Job memanggil Midtrans Status API (`GET /v2/{order_id}/status`).<br>- Status pesanan lokal otomatis diperbarui ke `Paid`. |
| [x] | **Skenario 6: Deteksi Mismatch Nominal** | 1. Kirim notifikasi manipulasi nominal (`gross_amount` berbeda). | - Terdeteksi selisih nominal > 0.01.<br>- Status pesanan tidak diubah.<br>- Insiden mismatch tercatat pada `payment_notifications` dan log Seq. |

---

## Menjalankan Pengujian Otomatis (Testing)

### Testing Backend (.NET)
Backend dilengkapi pengujian unit komprehensif pada Domain dan Application, serta pengujian integrasi end-to-end menggunakan **Testcontainers** (kontainer PostgreSQL dan Redis otomatis dibuat saat pengetesan).

```bash
# Masuk ke direktori backend
cd backend

# Jalankan seluruh suite pengujian
dotnet test --verbosity normal

# Menjalankan pengujian dengan kalkulasi code coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

Target cakupan kode minimum:
- Domain layer: &ge; 90%
- Application layer: &ge; 80%

### Testing Frontend (React + Vitest)
Frontend mencakup pengujian unit skema Zod, komponen UI kompleks (`VariantSelector`, keranjang optimis, form auth), dan interceptor Axios:

```bash
# Masuk ke direktori frontend
cd frontend

# Jalankan unit & integration tests Vitest
npm run test

# Jalankan typecheck TypeScript
npm run typecheck

# Jalankan linter ESLint
npm run lint

# Buat build rilis
npm run build
```

---

## Observabilitas & Monitoring Lokal

SportShop dilengkapi telemetri terpadu untuk monitoring performa dan debugging insiden:

1. **Seq Log & Trace Aggregator (`http://localhost:5341`)**:
   - Seluruh log terstruktur Serilog diperkaya dengan `CorrelationId`, `UserId`, `OrderNumber`, dan `ClientIp`.
   - Jejak terdistribusi OpenTelemetry menampilkan kueri EF Core, durasi panggilan database, dan pemanggilan HTTP Midtrans.

2. **Pemeriksaan Kesehatan (Health Checks)**:
   - Live endpoint: `GET /health/live` (mengecek proses aplikasi aktif).
   - Ready endpoint: `GET /health/ready` (memvalidasi konektivitas PostgreSQL, Redis, dan Hangfire Server).

3. **Hangfire Background Job Dashboard (`http://localhost:5080/hangfire`)**:
   - Dapat diakses oleh pengguna dengan role **Admin**.
   - Menampilkan status eksekusi:
     - `ExpireUnpaidOrdersJob`: Membatalkan pesanan kedaluwarsa setiap 15 menit.
     - `ReconcilePaymentsJob`: Menyelaraskan status pembayaran yang tertunda.
     - `CleanupJob`: Membersihkan token usang dan file sampah berkala.

---

## Panduan Rilis Produksi (Docker Compose)

Untuk menggelar aplikasi di lingkungan server produksi (*staging* / *production*):

1. **Siapkan konfigurasi produksi**:
   ```bash
   cp .env.production.example .env.production
   nano .env.production
   ```
   *Wajib mengisi password database kuat, secret JWT minimal 64 karakter, Midtrans Production Keys (`Midtrans__IsProduction=true`), domain CORS asli, dan konfigurasi SMTP produksi.*

2. **Jalankan tumpukan kontainer produksi**:
   ```bash
   docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build
   ```

3. **Verifikasi status kesehatan**:
   ```bash
   docker compose -f docker-compose.prod.yml ps
   curl -f http://localhost:80/healthz
   curl -f http://localhost:80/api/v1/health/ready
   ```

Aplikasi siap melayani trafik pelanggan dengan keamanan non-root container, reverse proxy Nginx yang teroptimasi, dan persistensi database yang tangguh.
