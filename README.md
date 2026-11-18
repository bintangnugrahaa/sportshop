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
- Seq (Logging): `http://localhost:5341`
- Mailpit: `http://localhost:8025`
