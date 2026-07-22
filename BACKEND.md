# LoginAnomaly — Dokumentasi Backend

> Catatan struktur backend sebelum mulai garap front-end.
> Terakhir diperbarui: 2026-06-29

Sistem deteksi anomali login: setiap percobaan login diproses lewat **dua sumbu** yang dipisah tegas:

- **Sumbu 1 — Kredensial** (`LoginSucceeded`): apakah username + password cocok.
- **Sumbu 2 — Risiko** (`RiskScore` + `Decision`): seberapa mencurigakan login itu, terlepas dari benar/salahnya password.

Dua sumbu ini independen. Login bisa sukses tapi tetap di-*challenge*/-*block* karena skor risiko tinggi (mis. impossible travel), dan login gagal pun tetap dicatat & diberi skor (mis. brute force).

---

## 1. Tech Stack

| Bagian | Pilihan |
|---|---|
| Runtime | .NET 8 (SDK pinned `8.0.422` via `global.json`) |
| Framework | ASP.NET Core Web API (controllers) |
| ORM | Entity Framework Core 8 |
| Database | SQL Server (`localhost,1433`, db `LoginAnomalyDb`) |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Realtime | SignalR (`MonitoringHub` di `/hubs/monitoring`) |
| Hashing | BCrypt (`BCrypt.Net-Next` 4.2.0) |
| Dokumentasi API | Swagger / Swashbuckle 6.6.2 (hanya di Development) |
| Arsitektur | Clean-ish layering: **Domain → Infrastructure → Api** |

---

## 2. Struktur Solution

Solution `LoginAnomaly.sln` berisi 3 project, dependensi mengalir satu arah (Domain tidak tahu apa-apa soal Api/Infrastructure):

```
LoginAnomaly.sln
└── src/
    ├── LoginAnomaly.Domain/          # Inti bisnis murni, tanpa dependensi framework
    │   ├── Entities/                 # Model data
    │   │   ├── User.cs
    │   │   ├── LoginEvent.cs
    │   │   ├── Alert.cs
    │   │   ├── RuleHit.cs
    │   │   └── KnownDevice.cs
    │   ├── Enums/
    │   │   └── RiskEnums.cs           # RiskDecision, AlertSeverity
    │   └── Detection/                # Mesin deteksi (rule engine)
    │       ├── IDetectionRule.cs
    │       ├── RuleResult.cs
    │       ├── LoginContext.cs
    │       ├── RiskScorer.cs
    │       └── Rules/
    │           ├── BruteForceRule.cs
    │           ├── VelocityRule.cs
    │           ├── ImpossibleTravelRule.cs
    │           ├── NewDeviceRule.cs
    │           └── UnusualTimeRule.cs
    │
    ├── LoginAnomaly.Infrastructure/  # Akses data (EF Core)
    │   ├── Persistence/
    │   │   └── AppDbContext.cs
    │   ├── Configurations/
    │   │   └── EntityConfigurations.cs
    │   └── Migrations/                # 20260628063716_InitialCreate
    │
    └── LoginAnomaly.Api/             # Entry point HTTP
        ├── Program.cs                # Composition root / DI / pipeline
        ├── appsettings.json
        ├── Auth/
        │   ├── IPasswordHasher.cs / BcryptPasswordHasher.cs
        │   ├── IJwtTokenService.cs / JwtTokenService.cs
        │   ├── LoginPipelineService.cs   # ★ orkestrator inti
        │   ├── AttackSimulator.cs
        │   └── Dtos/ (RegisterRequest, LoginRequest)
        ├── Hubs/
        │   └── MonitoringHub.cs          # SignalR hub (push-only) → /hubs/monitoring
        └── Controllers/
            ├── AuthController.cs
            └── SimulatorController.cs
```

**Aturan dependensi:** `Api` → `Infrastructure` → `Domain`. `Domain` berdiri sendiri (rule engine bisa di-unit-test tanpa DB/HTTP).

---

## 3. Layer Domain

### 3.1 Entities

| Entity | PK | Field penting | Catatan |
|---|---|---|---|
| `User` | `int Id` | `Username` (unik), `PasswordHash`, `CreatedAtUtc` | Punya banyak `LoginEvent` & `KnownDevice` |
| `LoginEvent` | `long Id` | `UserId?` (nullable), `Username`, `IpAddress`, `Lat/Lng?`, `DeviceFingerprint`, `LoginSucceeded`, `RiskScore`, `Decision`, `IsSimulated`, `TimestampUtc` | Catatan tiap percobaan login. `UserId` nullable supaya login ke username tak-dikenal tetap tercatat |
| `RuleHit` | `long Id` | `LoginEventId`, `RuleName`, `Score`, `Reason?` | Satu baris per rule yang "menyala" pada sebuah event |
| `Alert` | `long Id` | `LoginEventId` (unik), `Severity`, `Summary`, `IsAcknowledged`, `AcknowledgedAtUtc?` | Relasi 1:0..1 dgn LoginEvent; dibuat hanya jika skor ≥ 25 |
| `KnownDevice` | `int Id` | `UserId`, `DeviceFingerprint`, `IpAddress?`, `FirstSeenUtc`, `LastSeenUtc` | Whitelist device per user; unik per `(UserId, DeviceFingerprint)` |

> File entity masih menyimpan versi lama dalam komentar — bisa dibersihkan kapan-kapan.

### 3.2 Enums (`RiskEnums.cs`)

```
RiskDecision  : Allow = 0, Challenge = 1, Block = 2
AlertSeverity : Low = 0,   Medium = 1,    High = 2
```

### 3.3 Mesin Deteksi (Detection)

Pola **Strategy**: tiap aturan implementasi `IDetectionRule`, di-loop oleh `RiskScorer`.

```csharp
interface IDetectionRule { string Name { get; }  RuleResult Evaluate(LoginContext ctx); }
record RuleResult(int Score, string? Reason);
```

**`LoginContext`** — objek input read-only untuk semua rule. Yang penting: riwayat sudah **di-load duluan** oleh service (`RecentHistory`, `KnownDevices`), jadi **rule tidak pernah menyentuh DB**. Ini yang bikin rule murni & gampang dites.

**`RiskScorer.Evaluate(ctx)`** — jalankan semua rule sekuensial, jumlahkan skor yang > 0, kumpulkan jadi `Hits`, lalu petakan total ke keputusan:

| Total skor | Decision |
|---|---|
| `>= 50` | **Block** |
| `25–49` | **Challenge** |
| `< 25` | **Allow** |

### 3.4 Daftar Rule

| Rule | Skor | Pemicu | Window |
|---|---|---|---|
| `BruteForceRule` | 30 | ≥ 5 login **gagal** dari IP/username sama | 1 menit |
| `ImpossibleTravelRule` | 40 | Login sukses dgn implied speed > 900 km/h dari login sukses sebelumnya (jarak Haversine ÷ jam) | berdasarkan riwayat |
| `VelocityRule` | 25 | ≥ 10 percobaan (sukses+gagal) dari IP/username sama | 10 detik |
| `NewDeviceRule` | 20 | Login **sukses** dari device fingerprint yang belum di-whitelist | — |
| `UnusualTimeRule` | 15 | Login jam 01:00–05:00 UTC | — |

Catatan tiap rule:
- **BruteForce** hanya hitung yang gagal; **Velocity** hitung semuanya (deteksi pola bot terlepas dari hasil).
- **ImpossibleTravel** butuh lat/lng pada event sekarang & event sukses sebelumnya; kalau salah satu null → skip. Pakai radius bumi 6371 km.
- **NewDevice** cek `ctx.KnownDevices`; whitelist-nya ditulis oleh pipeline setelah login sukses.
- **UnusualTime** masih versi sederhana: jendela malam hardcoded, basis UTC (belum sadar zona waktu user).

---

## 4. Layer Infrastructure

### 4.1 `AppDbContext`

`DbSet` untuk kelima entity. `OnModelCreating` memanggil `ApplyConfigurationsFromAssembly` → otomatis ambil semua `IEntityTypeConfiguration` di assembly (tidak perlu daftar manual).

### 4.2 `EntityConfigurations.cs` (Fluent API)

Inti aturan DB:

- **User**: `Username` max 50 + **index unik**.
- **LoginEvent**: panjang kolom dibatasi (Ip 45 utk muat IPv6, device 200). `Decision` disimpan sebagai `int`. FK `User` nullable, **`OnDelete: SetNull`** (hapus user → event tetap ada untuk audit). Index di `Username`, `IpAddress`, `TimestampUtc`, dan komposit `(Username, TimestampUtc)` — dioptimalkan untuk query riwayat pipeline.
- **RuleHit**: FK ke LoginEvent, **Cascade** (hapus event → hit ikut terhapus).
- **Alert**: relasi 1:0..1, FK `LoginEventId` **unik** (jamin maks 1 alert/event), index di `IsAcknowledged`, Cascade.
- **KnownDevice**: index **unik** `(UserId, DeviceFingerprint)` untuk cegah duplikat whitelist, Cascade.

### 4.3 Migrations

Satu migration awal: `20260628063716_InitialCreate`. Belum ada seed data.

---

## 5. Layer Api

### 5.1 `Program.cs` (composition root)

Registrasi DI penting:
- `AppDbContext` → SQL Server (connection string `Default`).
- `IPasswordHasher` → `BcryptPasswordHasher` (scoped).
- **Kelima rule** didaftarkan sebagai `IDetectionRule` → di-inject sebagai `IEnumerable<IDetectionRule>` ke `RiskScorer`.
- `RiskScorer`, `LoginPipelineService`, `AttackSimulator`, `IJwtTokenService` (scoped).
- `AddSignalR()` → hub di-map ke `/hubs/monitoring`.
- JWT Bearer: validasi Issuer, Audience, Lifetime, SigningKey dari section `Jwt`.

Pipeline HTTP: Swagger (dev only) → HTTPS redirect → **Authentication → Authorization** → MapHub(`/hubs/monitoring`) → MapControllers.

### 5.2 `LoginPipelineService` — ★ JANTUNG SISTEM

`ProcessAsync(username, password, ip, lat, lng, deviceFingerprint, isSimulated)` melakukan, berurutan:

1. **Verifikasi kredensial** — cari user, `_hasher.Verify` → `succeeded` (sumbu 1).
2. **Load riwayat** — `LoginEvents` 5 menit terakhir yang cocok IP atau username, descending. Juga load `KnownDevices` milik user.
3. **Bangun `LoginContext`** & jalankan `RiskScorer.Evaluate` (sumbu 2).
4. **Persist `LoginEvent`** + `SaveChanges` (perlu `Id` dulu).
5. **Whitelist device** kalau sukses & belum dikenal → tulis `KnownDevice`.
6. **Tulis `RuleHit`** untuk tiap hit.
7. **Buat `Alert`** kalau total skor ≥ `AlertThreshold` (severity High jika ≥ `BlockThreshold`, selain itu Medium; summary = gabungan nama rule).
8. `SaveChanges` final.
9. **Broadcast SignalR** — kirim `loginEvent` ke semua client; jika skor ≥ `AlertThreshold`, kirim juga `alert`.
10. Return `PipelineResult(succeeded, decision, score, user)`.

> Ambang kini berupa konstanta di service: `AlertThreshold = 25`, `BlockThreshold = 50` (selaras dengan ambang di `RiskScorer`).

Kedua jalur masuk — login asli (`AuthController`) dan simulasi (`AttackSimulator`) — **memakai pipeline yang sama**, jadi event simulasi diproses identik (cuma ditandai `IsSimulated = true`) dan ikut disiarkan lewat hub.

### 5.3 Controllers & Endpoint

**`AuthController` — `/api/auth`**

| Method | Endpoint | Fungsi |
|---|---|---|
| POST | `/api/auth/register` | Validasi non-kosong → cek username dobel (409) → simpan user dgn password ter-hash. Return `{ Id, Username }` |
| POST | `/api/auth/login` | Resolusi `ip`/`device` dari body atau header/koneksi → jalankan pipeline → mapping decision ke HTTP |

Mapping respons login:
- Kredensial salah → **401** (tetap sertakan decision + score).
- `Allow` → **200** + JWT token.
- `Challenge` → **403** "Verifikasi tambahan diperlukan".
- `Block` → **423 Locked** "Akses diblokir".

**`SimulatorController` — `/api/simulate`**

| Method | Endpoint | Fungsi |
|---|---|---|
| POST | `/api/simulate/{scenario}?username=...` | Jalankan skenario serangan terhadap user target |

Skenario (`AttackSimulator`): `bruteforce` (8× password salah dari 1 IP), `velocity` (15× rapid-fire), `impossible-travel` (2 login sukses Jakarta→London). Scenario tak dikenal → `EventsGenerated = 0` → 400.

### 5.4 Auth services

- **`BcryptPasswordHasher`** — `Hash` / `Verify` via BCrypt.
- **`JwtTokenService.GenerateToken(user)`** — claims `Sub` (user id), `UniqueName` (username), `Jti` (guid). HMAC-SHA256, expiry dari config (`ExpiryMinutes`, default 60).

### 5.5 Realtime — `MonitoringHub` (SignalR)

- **Hub**: `MonitoringHub : Hub`, di-map ke **`/hubs/monitoring`**. Hub **kosong / push-only** — klien hanya *listen*, backend yang mendorong data (klien tak memanggil method hub).
- **Dipancarkan dari `LoginPipelineService`** via `IHubContext<MonitoringHub>` ke `Clients.All`:

| Event | Kapan | Payload |
|---|---|---|
| `loginEvent` | **setiap** login diproses | `Id, Username, IpAddress, LoginSucceeded, RiskScore, Decision (string), IsSimulated, TimestampUtc` |
| `alert` | hanya jika skor ≥ `AlertThreshold` | `Id, Username, RiskScore, Decision (string), Rules (daftar nama rule)` |

> Untuk front-end: connect ke `/hubs/monitoring`, subscribe `loginEvent` (feed live) dan `alert` (notifikasi). Saat ini **belum ada auth/otorisasi di hub** — semua client tersambung menerima semua broadcast.

### 5.6 Konfigurasi (`appsettings.json`)

- `ConnectionStrings:Default` — SQL Server lokal, user `sa`.
- `Jwt` — `Key`, `Issuer` (`LoginAnomalyApi`), `Audience` (`LoginAnomalyClient`), `ExpiryMinutes` 60.
- Launch: http `:5108`, https `:7218`; Swagger UI di `/swagger`.

---

## 6. Alur End-to-End (login asli)

```
Client → POST /api/auth/login
      → AuthController (resolve ip/device)
      → LoginPipelineService.ProcessAsync
           ├─ verify password           (sumbu 1)
           ├─ load history + devices
           ├─ RiskScorer → semua Rule   (sumbu 2)
           ├─ simpan LoginEvent
           ├─ whitelist device (jika sukses)
           ├─ simpan RuleHit[]
           ├─ simpan Alert (jika skor ≥ 25)
           └─ broadcast SignalR: "loginEvent" (selalu) + "alert" (jika ≥ 25)
      → map Decision → 200 (+JWT) / 401 / 403 / 423

Dashboard (terhubung ke /hubs/monitoring) menerima "loginEvent"/"alert" secara realtime.
```

---

## 7. Batasan / Known Limitations (kondisi sekarang)

- **Belum ada endpoint baca data** untuk front-end: belum ada GET daftar `LoginEvent`, `Alert`, atau acknowledge alert. Ini yang kemungkinan besar perlu dibuat untuk dashboard.
- **`AcknowledgedAtUtc` & `IsAcknowledged`** sudah ada di model tapi belum ada jalur untuk meng-acknowledge alert.
- **Secret hardcoded**: JWT key & password DB ada di `appsettings.json` (plain). Perlu pindah ke user-secrets / env / vault sebelum publik.
- **JWT key** terlihat lemah/contoh; pastikan diganti & cukup panjang untuk HMAC-SHA256.
- **`UnusualTimeRule` berbasis UTC**, belum sadar zona waktu/kebiasaan tiap user — rawan false positive untuk user di timezone tertentu.
- **Lat/Lng & device fingerprint berasal dari client** (body request) — bisa dipalsukan; belum ada validasi/penurunan dari sumber tepercaya (mis. GeoIP server-side).
- **Impossible-travel simulator** memakai password hardcoded `rahasia123`; hanya jalan kalau user target benar punya password itu.
- **Belum ada rate limiting / lockout** di level HTTP (deteksi bersifat scoring, bukan penegakan).
- **Hub SignalR belum diamankan** — tidak ada `[Authorize]`, semua client yang connect menerima semua broadcast (termasuk username/IP). Perlu auth + kemungkinan grouping sebelum produksi.
- **Belum ada test** (unit/integration), padahal Domain dirancang gampang dites.
- **Belum ada logging terstruktur / global exception handling** yang eksplisit.
- **`[Authorize]` belum dipakai** di endpoint mana pun — semua endpoint masih anonim.
- **Tidak ada CORS** yang dikonfigurasi — perlu ditambah sebelum front-end (SPA) bisa memanggil API dari origin berbeda.

---

## 8. Future Considerations (untuk front-end & lanjutan)

**Yang kemungkinan diperlukan front-end dashboard:**
1. **Read API** — `GET /api/events` (filter user/ip/tanggal, paginasi), `GET /api/alerts` (filter severity / belum di-ack), `GET /api/events/{id}` lengkap dgn `RuleHits`.
2. **Acknowledge alert** — `POST /api/alerts/{id}/ack` (set `IsAcknowledged` + `AcknowledgedAtUtc`).
3. **CORS** untuk origin front-end (juga perlu untuk koneksi SignalR dari SPA).
4. **SignalR sudah jalan** (`/hubs/monitoring`, event `loginEvent` & `alert`) — tinggal di-consume front-end; sisanya: amankan hub (auth) & pertimbangkan grouping per-user/role.
5. **Endpoint statistik/ringkasan** untuk kartu dashboard (jumlah alert per severity, tren login, top IP, dll).

**Hardening / kualitas:**
6. Pindahkan secret ke konfigurasi aman; pasang `[Authorize]` + role/policy.
7. Tambah validasi input (FluentValidation / data annotations).
8. Unit test rule engine + integration test pipeline.
9. GeoIP server-side untuk lat/lng yang lebih tepercaya.
10. Bikin rule & ambang skor **configurable** (saat ini semua hardcoded `const`).
11. Logging terstruktur + global exception middleware + health check.
12. Seed data untuk demo dashboard.

---

## 9. Cara Menjalankan (referensi cepat)

```bash
# dari root solution
dotnet build

# jalankan API (butuh SQL Server hidup di localhost:1433)
dotnet run --project src/LoginAnomaly.Api

# Swagger UI:
#   http://localhost:5108/swagger  (atau https://localhost:7218/swagger)

# migration (jika perlu)
dotnet ef database update --project src/LoginAnomaly.Infrastructure --startup-project src/LoginAnomaly.Api
```
