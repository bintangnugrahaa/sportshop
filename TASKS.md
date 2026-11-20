# TASKS.md — Rencana Pengerjaan SportShop

Cara pakai: kerjakan berurutan per fase; satu tugas = satu branch/PR (`feat/T3-02-...`). Centang `[x]` setelah memenuhi **Definition of Done** di `AGENTS.md`. Kolom **Dep** menunjukkan tugas yang harus selesai lebih dulu. Ukuran: **S** ≤ 0,5 hari, **M** ≈ 1 hari, **L** ≈ 2–3 hari.

Rekomendasi urutan besar: `F0 → F1 → F2 → F3 → F4 → F5 → F6 → F7 → F8 → F9 → F10 → F11`. Fase frontend (F9–F10) bisa berjalan paralel dengan backend setelah kontrak API fase terkait stabil.

---

## Fase 0 — Fondasi Repositori & Tooling

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T0-01 | Inisialisasi repo GitHub, `.gitignore`, `.editorconfig`, `.gitattributes`, lisensi, README awal, penempatan lima dokumen perencanaan | — | S | Repo dapat di-clone; branch `main` terproteksi |
| [x] | T0-02 | Solusi backend: 4 proyek src + 3 proyek test, referensi antar-layer sesuai arah dependensi, `Directory.Build.props` (nullable, warnaserror, analyzer), `Directory.Packages.props` (central package management) | T0-01 | M | `dotnet build` hijau; tes arsitektur sederhana memastikan Domain tak mereferensi layer lain |
| [x] | T0-03 | Scaffold frontend: Vite + React + TS strict, Tailwind, shadcn/ui (`components.json`), ESLint, Prettier, Vitest, alias path `@/` | T0-01 | M | `npm run dev/lint/typecheck/test/build` hijau |
| [x] | T0-04 | `docker-compose.yml`: db, redis, seq, mailpit, api, web + `.env.example` + Dockerfile backend & frontend | T0-02, T0-03 | M | `docker compose up` menjalankan semua service sehat |
| [x] | T0-05 | GitHub Actions CI backend & frontend, cache, laporan test; Dependabot | T0-02, T0-03 | M | PR percobaan menjalankan CI dan hijau |

## Fase 1 — Infrastruktur Backend Lintas-Fungsi

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T1-01 | `Program.cs` + modul DI; konfigurasi `IOptions` dengan `ValidateOnStart` | T0-02 | S | Aplikasi gagal start jika konfigurasi wajib kosong |
| [x] | T1-02 | Serilog (console + Seq), enrichment, request logging | T1-01 | S | Log muncul di Seq dengan `TraceId` |
| [x] | T1-03 | OpenTelemetry trace + metrik, export OTLP ke Seq | T1-02 | M | Trace request→EF Core terlihat di Seq |
| [x] | T1-04 | Middleware ProblemDetails (RFC 7807), `Result<T>`/`Error` → HTTP mapping, CorrelationId | T1-01 | M | Semua jenis error menghasilkan format di `DESIGN.md` B1 |
| [x] | T1-05 | Swagger/OpenAPI + skema Bearer + versi `v1` | T1-01 | S | `/swagger` tampil, tombol Authorize berfungsi |
| [x] | T1-06 | `AppDbContext`, konvensi snake_case, `IClock`, UnitOfWork, koneksi PostgreSQL, migrasi awal kosong | T1-01 | M | `database update` sukses di Postgres Docker |
| [x] | T1-07 | Redis `ICacheService` (cache-aside, fallback aman saat Redis mati) | T1-01 | S | Test: Redis down tidak membuat request gagal |
| [x] | T1-08 | HealthChecks `/health/live` & `/health/ready` (Npgsql, Redis, Hangfire) | T1-06, T1-07 | S | Ready = 503 bila DB mati |
| [x] | T1-09 | Hangfire (storage PostgreSQL), dashboard Admin-only | T1-06 | S | Job contoh berjalan |
| [x] | T1-10 | Rate limiting, CORS, security headers | T1-01 | S | Uji: >10 login/menit → 429 |
| [x] | T1-11 | Fondasi test: `WebApplicationFactory` + Testcontainers (Postgres, Redis), `CustomWebApplicationFactory`, helper auth test | T1-06 | M | Satu test integrasi `/health/ready` lulus di CI |

## Fase 2 — Identity & Autentikasi

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T2-01 | `ApplicationUser/Role`, Identity + migrasi, seed role & admin dari env | T1-06 | M | Admin awal terbuat sekali (idempoten) |
| [x] | T2-02 | `JwtTokenService`, konfigurasi JWT Bearer, policy `Admin` | T2-01 | M | Token valid diterima, kedaluwarsa/rusak → 401 |
| [x] | T2-03 | Register + FluentValidation + test | T2-01 | S | Email duplikat → 409/400 sesuai kontrak |
| [x] | T2-04 | Login + lockout + refresh token (hash, rotasi, family, reuse detection) + cookie HttpOnly | T2-02 | L | Reuse token lama mencabut seluruh family (test) |
| [x] | T2-05 | Logout, profil (`/me`), ganti password | T2-04 | S | Test integrasi |
| [x] | T2-06 | Abstraksi `IEmailSender` + `SmtpEmailSender` (Mailpit di dev), lalu lupa/reset password + konfirmasi email | T2-04, T1-09 | M | Email terlihat di Mailpit; token sekali pakai |
| [x] | T2-07 | Test keamanan: akses tanpa token, role salah, ownership | T2-04 | S | Matriks 401/403/404 lulus |

## Fase 3 — Katalog (Domain + API Publik)

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T3-01 | Entity Domain katalog + konfigurasi EF + migrasi (categories, brands, products, variants, images, inventory_movements) | T1-06 | L | Constraint & index sesuai `DATABASE.md` |
| [x] | T3-02 | Admin CRUD kategori & brand (validasi slug unik, kedalaman 2 tingkat) | T3-01, T2-02 | M | Test integrasi CRUD + otorisasi |
| [x] | T3-03 | Admin CRUD produk & varian (SKU unik, soft delete) | T3-01, T2-02 | L | Test validasi, aturan harga/berat/stok |
| [x] | T3-04 | Upload gambar produk (`IFileStorage` lokal, validasi MIME/ukuran, GUID) | T3-03 | M | File >2 MB atau tipe salah ditolak |
| [x] | T3-05 | Penyesuaian stok manual + `inventory_movements` + daftar stok menipis | T3-03 | M | Stok tak bisa negatif; riwayat tercatat |
| [x] | T3-06 | `GET /products` (filter, sortir, paginasi) dengan proyeksi & Mapperly | T3-03 | L | EXPLAIN memakai index; test untuk tiap filter |
| [x] | T3-07 | Pencarian full-text + trigram (`search_vector`, `pg_trgm`) | T3-06 | M | Cari "sepatu lari" dan salah ketik "sepaatu" memberi hasil relevan |
| [x] | T3-08 | `GET /products/{slug}`, `/categories`, `/brands` + cache Redis & invalidasi | T3-06, T1-07 | M | Cache hit terlihat di log; invalidasi saat admin ubah data |
| [x] | T3-09 | Seeder data demo (flag `SEED_DEMO_DATA`) | T3-03 | S | ±30 produk terisi di dev |

## Fase 4 — Keranjang, Alamat, Ongkir

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T4-01 | Domain + EF `carts`, `cart_items`; use case Get/Add/Update/Remove/Clear | T3-01, T2-02 | M | Jumlah > stok ditolak; satu keranjang per user |
| [x] | T4-02 | Respons keranjang menandai item yang harga/stok berubah atau varian nonaktif | T4-01 | S | Test skenario |
| [x] | T4-03 | Tabel `shipping_rates` + seed provinsi + `ShippingCalculator` (unit test aturan: min 1 kg, `ceil`) | T1-06 | M | Unit test rumus lulus, termasuk batas |
| [x] | T4-04 | Admin kelola tarif ongkir + cache `ship:rates:v1` | T4-03 | S | Perubahan tarif tercermin segera |
| [x] | T4-05 | CRUD alamat (ownership, default tunggal, soft delete), `GET /shipping/provinces`, `POST /shipping/quote` | T4-03, T2-02 | M | Alamat orang lain → 404 |

## Fase 5 — Pesanan & Stok (Inti Bisnis)

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T5-01 | Domain `Order` (aggregate) + transisi status + `OrderStatusHistory`; unit test seluruh tabel transisi di `PRD.md` 6.1 | T3-01 | L | Semua transisi ilegal melempar `DomainException` |
| [x] | T5-02 | EF + migrasi `orders`, `order_items`, `order_status_histories`, generator nomor pesanan | T5-01 | M | Constraint CHECK & index sesuai `DATABASE.md` |
| [x] | T5-03 | Use case **Checkout**: transaksi, pengurangan stok atomik, snapshot, kosongkan keranjang, idempotency key | T5-02, T4-01, T4-05 | L | Respons 201; stok berkurang; kunci sama → pesanan sama |
| [x] | T5-04 | **Uji konkurensi**: 50 checkout paralel, stok 10 → tepat 10 sukses, stok akhir 0 | T5-03 | M | Test integrasi stabil (dijalankan 10× tanpa flake) |
| [x] | T5-05 | Customer: daftar & detail pesanan, batal pesanan `PendingPayment` (stok kembali sekali) | T5-03 | M | Batal 2× tidak menggandakan stok |
| [x] | T5-06 | Job `ExpireUnpaidOrdersJob` | T5-05, T1-09 | M | Pesanan lewat `expires_at` → Expired, stok kembali, idempoten |
| [x] | T5-07 | Admin: daftar/detail pesanan, `process`, `ship` (kurir + resi wajib), `complete`, `cancel` | T5-01, T2-02 | L | Test transisi + otorisasi |

## Fase 6 — Pembayaran Midtrans

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T6-01 | Domain/EF `payments`, `payment_notifications`; `IPaymentGateway` + `MidtransClient` (Snap create, status check) | T5-02 | L | Dites dengan HTTP handler palsu |
| [x] | T6-02 | Polly: timeout, retry, circuit breaker pada HttpClient Midtrans | T6-01 | M | Test: 2× 503 lalu sukses → berhasil; breaker terbuka sesuai konfigurasi |
| [x] | T6-03 | Integrasi checkout → buat transaksi Snap → simpan payment; `POST /orders/{n}/pay` untuk melanjutkan | T6-01, T5-03 | M | Token tak tersedia ≠ pesanan hilang |
| [x] | T6-04 | **Webhook**: verifikasi signature (SHA512, constant-time), dedup, konfirmasi Status API, pemetaan status (`DESIGN.md` A5), idempoten & tahan urutan terbalik | T6-03 | L | Test: signature salah → 401/diabaikan; duplikat aman; `gross_amount` beda → ditandai mismatch |
| [x] | T6-05 | `SendEmailJob` (Hangfire, retry) + template email transaksional (pesanan dibuat, dibayar, dikirim) memakai `IEmailSender` dari T2-06 | T2-06 | M | Email muncul di Mailpit; retry saat SMTP gagal |
| [x] | T6-06 | `ReconcilePaymentsJob` untuk pembayaran `Pending` | T6-04, T1-09 | M | Webhook sengaja dilewatkan → status tetap tersinkron |
| [x] | T6-07 | Hubungkan event pesanan (Paid/Shipped) ke email | T6-05, T6-04 | S | Email terkirim sekali per event |
| [x] | T6-08 | Uji end-to-end sandbox Midtrans (manual, didokumentasikan di README) | T6-04 | S | Checklist skenario sukses/expire/cancel terisi |

## Fase 7 — Dashboard Admin (Backend) & Penyempurnaan API

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T7-01 | `GET /admin/dashboard/summary`: pesanan hari ini, omzet 7/30 hari, terlaris, stok menipis | T5-07 | M | Angka cocok dengan data seed di test |
| [x] | T7-02 | Metrik kustom OpenTelemetry (orders_created, payments_settled, stock_conflicts) | T1-03, T6-04 | S | Muncul di backend metrik |
| [x] | T7-03 | Job `CleanupJob` (token kedaluwarsa, notifikasi lama) | T1-09 | S | Test job |
| [x] | T7-04 | Review Swagger: contoh, kode respons, deskripsi untuk seluruh endpoint | semua API | M | Tidak ada endpoint tanpa dokumentasi |

## Fase 8 — Kualitas Backend & Hardening

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T8-01 | Laporan cakupan; capai Domain ≥ 90%, Application ≥ 80% | F2–F7 | M | CI menampilkan cakupan |
| [x] | T8-02 | Tinjauan keamanan sesuai `AGENTS.md` §9 (ownership, secret, header, log) + CodeQL | F2–F7 | M | Daftar temuan nol berkategori tinggi |
| [x] | T8-03 | Uji beban ringan katalog (k6/NBomber, 100 RPS) & perbaiki query lambat | T3-08 | M | p95 < 300 ms (NFR-01) |
| [x] | T8-04 | Review indeks & `EXPLAIN` untuk query utama | T8-03 | S | Tidak ada seq scan pada tabel besar untuk query utama |

## Fase 9 — Frontend: Fondasi, Auth, Katalog

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T9-01 | Layout, router, guard, provider (QueryClient, Toaster), theme token, komponen shared | T0-03 | M | Navigasi dan guard bekerja |
| [x] | T9-02 | Axios instance + interceptor JWT + refresh antrean tunggal; `authStore` | T9-01, T2-04 | M | Test: banyak 401 serentak → hanya satu refresh |
| [x] | T9-03 | Halaman Masuk, Daftar, Lupa/Reset Password (RHF + Zod) | T9-02 | M | Error server tampil per field |
| [x] | T9-04 | Katalog: daftar, filter, sortir, paginasi, pencarian (state di URL query) | T9-01, T3-06 | L | Refresh halaman mempertahankan filter |
| [x] | T9-05 | Detail produk: galeri, `VariantSelector`, tambah ke keranjang | T9-04, T3-08 | M | Varian stok 0 nonaktif |
| [x] | T9-06 | Beranda (kategori unggulan, produk terbaru) | T9-04 | S | — |

## Fase 10 — Frontend: Belanja, Akun, Admin

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [x] | T10-01 | Keranjang (drawer + halaman), optimistic update, peringatan perubahan harga/stok | T9-05, T4-01 | M | Rollback saat error |
| [x] | T10-02 | Alamat: daftar & form (pilih provinsi) | T9-03, T4-05 | M | — |
| [x] | T10-03 | Checkout: pilih alamat, ringkasan + ongkir, buat pesanan, buka Snap | T10-01, T10-02, T6-03 | L | Klik ganda tidak membuat pesanan ganda |
| [x] | T10-04 | Halaman status pembayaran (polling) + "Bayar Sekarang" ulang | T10-03 | M | Status berubah otomatis setelah webhook |
| [x] | T10-05 | Pesanan Saya: daftar, detail, timeline, batal | T10-04, T5-05 | M | — |
| [x] | T10-06 | Profil & ganti password | T9-03, T2-05 | S | — |
| [ ] | T10-07 | Admin: kerangka layout + dashboard | T9-01, T7-01 | M | Hanya role Admin |
| [ ] | T10-08 | Admin: produk, varian, gambar, kategori, brand | T10-07, T3-04 | L | Upload & validasi bekerja |
| [ ] | T10-09 | Admin: stok & penyesuaian, daftar stok menipis | T10-07, T3-05 | M | — |
| [ ] | T10-10 | Admin: pesanan (filter, detail, ubah status, input resi) | T10-07, T5-07 | L | Aksi hanya muncul sesuai status |
| [ ] | T10-11 | Admin: tarif ongkir | T10-07, T4-04 | S | — |
| [ ] | T10-12 | Aksesibilitas & responsif: audit Lighthouse, keyboard, kontras; code splitting Admin | T10-10 | M | Accessibility ≥ 90 (NFR-09); bundle awal < 200 KB gzip |
| [ ] | T10-13 | Test frontend: skema Zod, VariantSelector, form auth, interceptor | T9-02 | M | CI hijau |

## Fase 11 — Rilis & Dokumentasi

| ✓ | ID | Tugas | Dep | Ukuran | Kriteria penerimaan |
|---|---|---|---|---|---|
| [ ] | T11-01 | Dockerfile produksi (multi-stage, non-root), Nginx untuk frontend, compose produksi contoh | T0-04 | M | Image kecil; `docker compose -f ... up` jalan |
| [ ] | T11-02 | Workflow `docker.yml` push ke GHCR pada tag | T11-01, T0-05 | S | Tag `v0.1.0` menghasilkan image |
| [ ] | T11-03 | README lengkap: setup lokal, variabel env, akun Midtrans Sandbox + URL webhook (mis. ngrok/Cloudflare Tunnel), menjalankan test | semua | M | Developer baru berjalan < 15 menit |
| [ ] | T11-04 | UAT skenario penuh (sandbox): beli → bayar → proses → kirim → selesai; kedaluwarsa; batal; webhook duplikat | semua | M | Semua skenario lulus; hasil dicatat |
| [ ] | T11-05 | Checklist go-live: ganti key Midtrans produksi, `IsProduction=true`, secret, CORS, backup DB, monitoring Seq | T11-04 | S | Checklist ditandatangani |

---

## Pengujian Kunci yang Wajib Ada (ringkasan lintas-fase)

- [ ] Transisi status pesanan (tabel lengkap) — T5-01
- [ ] Tidak ada overselling pada 50 checkout paralel — T5-04
- [ ] Pembatalan/kedaluwarsa mengembalikan stok tepat sekali — T5-05, T5-06
- [ ] Signature webhook salah ditolak; notifikasi duplikat dan tak berurutan aman — T6-04
- [ ] Rotasi refresh token dan deteksi reuse — T2-04
- [ ] Customer tidak dapat mengakses data customer lain — T2-07, T4-05, T5-05
- [ ] Rumus ongkir (batas 1 kg, pembulatan ke atas) — T4-03
- [ ] Redis mati tidak mematikan API — T1-07

## Definisi Rilis MVP

MVP siap rilis bila semua kebutuhan **P0** di `PRD.md` tuntas (Fase 0–6, 9, 10 inti), NFR-02 dan NFR-07 terpenuhi, UAT (T11-04) lulus, dan checklist go-live (T11-05) selesai. Kebutuhan P1 yang belum selesai (mis. konfirmasi email, rekonsiliasi pembayaran, dashboard) boleh menyusul setelah rilis pertama; kebutuhan P2 dan seluruh item "di luar lingkup" tidak dikerjakan di v1.
