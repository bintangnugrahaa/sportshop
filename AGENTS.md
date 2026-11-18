# AGENTS.md — Panduan untuk AI Coding Agent

Dokumen ini adalah aturan kerja untuk agent (Claude Code, Copilot, Codex, dll.) dan developer yang mengerjakan **SportShop**, sistem e-commerce perlengkapan olahraga sederhana. Baca bersama `PRD.md`, `DATABASE.md`, `DESIGN.md`, dan `TASKS.md` sebelum menulis kode.

## 1. Ringkasan Proyek

SportShop adalah toko online tunggal (bukan marketplace) untuk perlengkapan olahraga: sepatu, jersey, raket, bola, aksesori. Peran pengguna: **Customer** dan **Admin**. Pembayaran memakai **Midtrans Snap**, ongkos kirim memakai **tarif flat per wilayah** (tabel sendiri, tanpa API ekspedisi).

Bahasa antarmuka pengguna: Indonesia. Bahasa kode, nama class, nama tabel, dan komentar kode: Inggris. Dokumentasi: Indonesia.

## 2. Tech Stack

**Backend**: C# 14, .NET 10, ASP.NET Core Web API, EF Core (Npgsql), PostgreSQL, ASP.NET Core Identity, JWT Bearer, FluentValidation, Mapperly, Serilog (sink Seq), Swagger/OpenAPI, Redis (cache), Hangfire (storage PostgreSQL), Polly (via `Microsoft.Extensions.Http.Resilience`), HealthChecks, OpenTelemetry (OTLP ke Seq).

**Testing**: xUnit, Moq, FluentAssertions, `WebApplicationFactory`, Testcontainers (PostgreSQL + Redis).

**Frontend**: React, TypeScript, Vite, React Router, TanStack Query, Tailwind CSS, shadcn/ui, React Hook Form, Zod, Axios, Zustand.

**DevOps**: Git, GitHub, GitHub Actions, Docker, docker-compose.

> Jangan menambah library di luar daftar ini tanpa alasan tertulis di PR. Tidak memakai MediatR atau AutoMapper (pakai use case handler sendiri dan Mapperly).

## 3. Struktur Repositori (Monorepo)

```
sportshop/
├── AGENTS.md  PRD.md  DATABASE.md  DESIGN.md  TASKS.md
├── docker-compose.yml
├── docker-compose.override.yml        # dev
├── .github/workflows/                 # ci-backend.yml, ci-frontend.yml, docker.yml
├── backend/
│   ├── SportShop.sln
│   ├── src/
│   │   ├── SportShop.Domain/          # Entity, ValueObject, Enum, DomainEvent, aturan bisnis
│   │   ├── SportShop.Application/     # Use case, DTO, interface, validator, Mapperly mapper
│   │   ├── SportShop.Infrastructure/  # EF Core, Identity, Redis, Midtrans, Hangfire, Serilog
│   │   └── SportShop.Api/             # Controller, middleware, DI, Program.cs
│   └── tests/
│       ├── SportShop.Domain.Tests/
│       ├── SportShop.Application.Tests/
│       └── SportShop.Api.IntegrationTests/
└── frontend/
    ├── package.json  vite.config.ts  tailwind.config.ts  components.json
    └── src/ (app, features, components, lib, stores, routes)
```

## 4. Perintah Penting

```bash
# Backend (dari /backend)
dotnet restore
dotnet build -warnaserror
dotnet test                                   # butuh Docker aktif (Testcontainers)
dotnet ef migrations add <Nama> -p src/SportShop.Infrastructure -s src/SportShop.Api
dotnet ef database update -p src/SportShop.Infrastructure -s src/SportShop.Api
dotnet format --verify-no-changes

# Frontend (dari /frontend)
npm ci
npm run dev
npm run lint && npm run typecheck && npm run test && npm run build

# Seluruh stack (dari root)
docker compose up -d --build
```

URL lokal default: API `http://localhost:5080`, Swagger `/swagger`, Hangfire `/hangfire` (Admin only), Seq `http://localhost:5341`, Frontend `http://localhost:5173`.

## 5. Aturan Arsitektur (Clean Architecture)

Arah dependensi: `Api → Infrastructure → Application → Domain`. `Api` juga boleh mereferensi `Application`.

| Layer | Boleh berisi | Dilarang |
|---|---|---|
| Domain | Entity, value object, enum, domain exception, aturan bisnis murni | Referensi ke EF Core, ASP.NET, library eksternal apa pun |
| Application | Use case handler, DTO, interface (`IProductRepository`, `IPaymentGateway`, `IClock`, `ICacheService`), FluentValidation, Mapperly mapper | Referensi ke EF Core `DbContext`, HttpClient, Redis client, Identity langsung |
| Infrastructure | Implementasi interface, `AppDbContext`, konfigurasi entity, migrasi, Midtrans client, Hangfire job, Redis | Logika bisnis |
| Api | Controller tipis, middleware, auth, DI composition, Swagger | Logika bisnis, akses `DbContext` langsung |

Aturan tambahan:
- Controller hanya: bind request → panggil use case → petakan hasil ke HTTP response.
- Satu use case = satu class handler dengan method `HandleAsync(command/query, CancellationToken)`. Daftarkan lewat DI.
- Gunakan `Result<T>` (success/failure dengan error code) untuk kegagalan yang *diharapkan*; exception hanya untuk kondisi tak terduga.
- Semua method async menerima `CancellationToken` dan meneruskannya.
- Waktu selalu lewat `IClock` / `TimeProvider`, tidak memakai `DateTime.UtcNow` langsung.
- Semua timestamp disimpan UTC (`timestamptz`).

## 6. Konvensi Kode Backend

- `nullable` enabled, `TreatWarningsAsErrors` true, `ImplicitUsings` enabled, file-scoped namespace.
- Gunakan `record` untuk DTO/command/query, `sealed` untuk class yang tidak diwariskan.
- Penamaan: `PascalCase` untuk tipe/method, `_camelCase` untuk field privat, interface diawali `I`, method async berakhiran `Async`.
- Endpoint REST: kata benda jamak, huruf kecil, kebab-case (`/api/v1/product-variants`). Versi di path.
- Error response mengikuti **RFC 7807 ProblemDetails** (lihat `DESIGN.md`).
- Validasi input HANYA lewat FluentValidation (satu validator per command/request). Jangan validasi manual di controller.
- Mapping memakai **Mapperly** (`[Mapper]` partial class). Jangan menulis mapping manual untuk objek besar; jangan pakai refleksi.
- EF Core: konfigurasi lewat `IEntityTypeConfiguration<T>`, bukan data annotation di entity. Query baca memakai `AsNoTracking()` dan proyeksi ke DTO. Hindari N+1 (gunakan `Include` / proyeksi / split query secara sadar).
- Logging memakai Serilog structured logging: `_logger.LogInformation("Order {OrderNumber} paid", number)`. Jangan interpolasi string di pesan log. Jangan log password, token, server key, atau data kartu.
- Konfigurasi lewat `IOptions<T>` dengan validasi `ValidateOnStart()`.

## 7. Konvensi Frontend

- TypeScript `strict: true`. Dilarang memakai `any` (gunakan `unknown` + Zod parse).
- Struktur berbasis fitur: `src/features/<fitur>/{api,components,hooks,schemas,pages}`.
- **Server state** = TanStack Query. **Client state** (auth session, UI cart drawer, filter sementara) = Zustand. Jangan menyalin data server ke Zustand.
- Semua form memakai React Hook Form + Zod resolver. Skema Zod menjadi sumber tipe (`z.infer`).
- Semua panggilan HTTP lewat satu instance Axios (`src/lib/http.ts`) dengan interceptor untuk JWT dan refresh token.
- Komponen UI dari shadcn/ui di `src/components/ui`. Jangan edit file generated secara besar-besaran; bungkus dengan komponen sendiri.
- Tailwind: gunakan design token dari `DESIGN.md`. Tidak ada CSS inline kecuali dinamis.
- Aksesibilitas: label pada input, fokus terlihat, kontras cukup, navigasi keyboard berfungsi.
- Teks UI dalam Bahasa Indonesia; format uang dengan `Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 })`.

## 8. Testing

| Jenis | Tools | Cakupan |
|---|---|---|
| Unit | xUnit + Moq + FluentAssertions | Domain (aturan stok, transisi status) dan handler Application |
| Integrasi | `WebApplicationFactory` + Testcontainers (PostgreSQL, Redis) | Endpoint end-to-end dengan database nyata, auth, webhook Midtrans (signature) |
| Frontend | Vitest + Testing Library | Komponen kritis, schema Zod, hook |

Aturan:
- Pola `Arrange / Act / Assert`, nama test: `Method_Kondisi_HasilDiharapkan`.
- Jangan memakai SQLite/InMemory provider untuk menggantikan PostgreSQL; pakai Testcontainers.
- Midtrans dimock di tingkat `IPaymentGateway`; webhook diuji dengan signature asli yang dihitung di test.
- Target cakupan: Domain ≥ 90%, Application ≥ 80%. PR yang menurunkan cakupan harus dijelaskan.
- Setiap bug fix wajib disertai test yang gagal sebelum perbaikan.

## 9. Keamanan (Wajib)

- Password ditangani ASP.NET Core Identity (min. 8 karakter, huruf besar, kecil, angka). Lockout aktif setelah 5 kali gagal.
- JWT access token berumur 15 menit; refresh token 7 hari, disimpan **hash**-nya di database, dirotasi setiap dipakai, dan dicabut saat logout atau terdeteksi reuse.
- Otorisasi berbasis role (`Customer`, `Admin`) dan **resource ownership**: customer hanya boleh mengakses cart, alamat, dan pesanan miliknya (cek `UserId`, kembalikan 404 bila bukan miliknya).
- **Harga selalu dihitung ulang di server.** Jangan percaya harga/total dari klien.
- Webhook Midtrans wajib diverifikasi: `SHA512(order_id + status_code + gross_amount + ServerKey)` dibandingkan dengan `signature_key` (constant-time compare). Setelah itu, konfirmasi status ke Midtrans Status API sebelum mengubah pesanan. Proses harus **idempoten**.
- Rahasia (`Midtrans:ServerKey`, `Jwt:Key`, connection string) hanya dari environment variable / user-secrets / GitHub Secrets. **Dilarang commit** `.env` atau rahasia apa pun.
- CORS hanya untuk origin frontend yang dikonfigurasi. Aktifkan rate limiting (`AddRateLimiter`) pada endpoint auth dan checkout. Tambahkan security header (HSTS, `X-Content-Type-Options`, dsb.).
- Dashboard Hangfire dan endpoint health detail hanya untuk Admin atau jaringan internal.
- Upload gambar: validasi tipe MIME dan ukuran (maks 2 MB), ganti nama file dengan GUID.

## 10. Git & Pull Request

- Branch: `main` (protected), `feat/<id-task>-<slug>`, `fix/<slug>`, `chore/<slug>`. Contoh: `feat/T3-02-product-crud`.
- Commit mengikuti **Conventional Commits**: `feat(orders): reserve stock on checkout`.
- PR kecil dan fokus pada satu tugas dari `TASKS.md`; cantumkan ID tugas di judul.
- CI harus hijau (build, format, test, lint, typecheck) sebelum merge. Squash merge.
- Jangan force-push ke `main`. Jangan commit file besar/biner/generated (`bin/`, `obj/`, `node_modules/`, `dist/`).

## 11. Definition of Done

Sebuah tugas selesai jika:
1. Kriteria penerimaan di `TASKS.md` terpenuhi.
2. Unit test dan/atau integration test ditambahkan dan lulus.
3. Build tanpa warning, format dan lint bersih.
4. Migrasi EF Core (jika ada) sudah dibuat, bisa naik dan bisa diperiksa isinya.
5. Swagger terdokumentasi (summary, response type, kode error) untuk endpoint baru.
6. Tidak ada rahasia atau data pribadi di kode/log.
7. Dokumen terkait (`DATABASE.md` / `DESIGN.md`) diperbarui bila skema atau kontrak berubah.

## 12. Cara Agent Bekerja

1. Baca `TASKS.md`, pilih tugas yang dependensinya sudah selesai.
2. Jika spesifikasi ambigu atau bertentangan antar dokumen, **berhenti dan tanyakan**; jangan menebak aturan bisnis (stok, harga, status pesanan, pembayaran).
3. Kerjakan perubahan terkecil yang memenuhi kriteria. Jangan refactor di luar lingkup tugas.
4. Jalankan build + test sebelum menyatakan selesai; laporkan hasil sebenarnya, termasuk test yang gagal atau dilewati.
5. Jangan menghapus atau melemahkan test agar lulus.
6. Jangan menjalankan perintah destruktif (drop database, `git reset --hard`, hapus volume Docker) tanpa persetujuan eksplisit.

## 13. Aturan Bisnis yang Tidak Boleh Dilanggar

- Stok tidak boleh negatif. Pengurangan stok dilakukan atomik di database (`UPDATE ... WHERE stock_quantity >= @qty`) dalam transaksi yang sama dengan pembuatan pesanan.
- Pesanan yang kedaluwarsa atau dibatalkan **mengembalikan stok** tepat satu kali.
- Transisi status pesanan hanya sesuai diagram di `PRD.md` (bagian 6). Transisi ilegal ditolak oleh Domain.
- Harga dan nama produk disalin ke `order_items` (snapshot) saat checkout; perubahan produk tidak boleh mengubah pesanan lama.
- Uang memakai `decimal` (C#) dan `numeric(18,2)` (PostgreSQL); dibulatkan ke rupiah penuh sebelum dikirim ke Midtrans.
