# RideSafeSA — Backend (MVP)

A standalone backend for a community-driven rideshare driver safety check.
Riders can check whether a driver has any confirmed reports, and submit a
report if something went wrong. The messaging platform (Telegram for now,
WhatsApp later) is a thin client on top of this API — all the real logic
lives here.

This is a learning project as much as a product prototype, so the code
favors clarity over cleverness. Comments in the source explain *why*
things are built the way they are, especially around abuse-prevention.

---

## 1. What you need to install

You don't need any of this to read the code, but you do to run it.

| Tool | Why | Link |
|---|---|---|
| **.NET 8 SDK** | Compiles and runs the project | https://dotnet.microsoft.com/download |
| **VS Code** + **C# Dev Kit extension** | Free editor with debugging, IntelliSense | https://code.visualstudio.com |
| **DB Browser for SQLite** (optional) | Lets you visually inspect `ridesafe.db` | https://sqlitebrowser.org |

You do **not** need Postman — the project has Swagger built in (see below),
which gives you a browser-based way to test every endpoint without any
extra tools.

Coming from Java/C#-family languages, the mental model is close to Spring
Boot: `Program.cs` is roughly your `@RestController` + `@Configuration`
combined, `AppDbContext` is your JPA `EntityManager` equivalent, and the
`Models/` classes are your `@Entity` classes.

---

## 2. Running it

```bash
cd src/RideSafeSA.Api
dotnet restore
dotnet run
```

First run will print a local URL (default `http://localhost:5080`) and
apply EF Core migrations automatically, creating `ridesafe.db` (a SQLite
file) in that folder if it doesn't already exist. Open:

```
http://localhost:5080/swagger
```

That gives you a clickable UI to try every endpoint listed below without
writing any client code. Click **Authorize** and paste the admin key (see
below) to unlock the `/api/admin/*` endpoints in the UI.

### Admin key

The `/api/admin/*` endpoints require an `X-Admin-Key` header. Locally,
`appsettings.Development.json` already sets a placeholder
(`dev-local-admin-key-change-me`) so things work out of the box. For any
other environment, set the real value via the `AdminApiKey` environment
variable (or user-secrets) — never commit a real key to `appsettings.json`.

### Changing the database schema

Schema changes go through EF Core migrations now, not automatic inference:

```bash
# one-time: dotnet tool install --global dotnet-ef --version 8.0.8
cd src/RideSafeSA.Api
dotnet ef migrations add <DescriptiveName>
```

That generates a new file under `Migrations/`. Commit it alongside your
model changes — the app applies any pending migrations automatically on
startup (`db.Database.Migrate()` in `Program.cs`), so you don't need to
run anything manually against the SQLite file.

---

## 3. Project structure

```
src/RideSafeSA.Api/
├── Program.cs              # all endpoints live here (Minimal API style)
├── Models/
│   ├── Driver.cs            # a driver, keyed by normalized license plate
│   ├── Report.cs            # a single report against a driver
│   ├── ReportCategory.cs    # fixed enum of report types (not free text)
│   ├── ReportStatus.cs      # Pending -> Confirmed/Rejected moderation flow
│   ├── Severity.cs          # Low/Medium/High, used to prioritize review
│   └── CategorySeverity.cs  # maps each ReportCategory to a Severity
├── Data/
│   └── AppDbContext.cs      # EF Core database context
├── Dtos/                    # request/response shapes (what the API accepts/returns)
├── Filters/
│   └── AdminApiKeyFilter.cs # gates /api/admin/* behind the X-Admin-Key header
├── Migrations/              # EF Core schema history - see "Changing the database schema"
└── appsettings.json          # SQLite connection string, logging config, AdminApiKey
```

---

## 4. Endpoints

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/drivers/check` | Rider checks a driver by name + plate before/during a ride |
| `POST` | `/api/reports` | Rider submits a report against a driver |
| `GET` | `/api/admin/reports/pending` 🔒 | Moderator: list reports awaiting review, prioritized by severity |
| `POST` | `/api/admin/reports/{id}/decision` 🔒 | Moderator: approve or reject a pending report |

🔒 = requires the `X-Admin-Key` header (see "Admin key" above).

### Example: checking a driver

```json
POST /api/drivers/check
{
  "name": "Sipho",
  "licensePlate": "CA 123-456"
}
```

```json
{
  "driverKnown": false,
  "name": "Sipho",
  "confirmedReportCount": 0,
  "pendingReportCount": 0,
  "confirmedByCategory": [],
  "summary": "No record found for this driver yet. That doesn't guarantee a safe ride — it just means nothing has been reported here."
}
```

### Example: submitting a report

```json
POST /api/reports
{
  "driverName": "Sipho",
  "licensePlate": "CA 123-456",
  "category": "Harassment",
  "detail": "Made repeated unwanted comments during the ride.",
  "photoReference": null
}
```

---

## 5. Design decisions worth understanding (not just copying)

- **Reports are Pending by default and invisible until a moderator
  confirms them.** This is the single most important anti-abuse
  mechanism in the whole system — nobody's single anonymous report can
  ever directly damage a driver's status. See `Program.cs` and the
  comments on `ReportStatus`.
- **Category is a fixed enum, not free text.** Easier to aggregate
  safely, harder to abuse than an open text field.
- **`/api/drivers/check` never returns raw report text**, only counts
  and categories. Raw detail is only ever exposed via the admin
  (moderator) endpoints. This matters for defamation risk — see the
  earlier conversation about surfacing aggregate signals rather than
  raw accusations.
- **Drivers are matched by normalized license plate**, not name (names
  aren't unique, plates mostly are). `NormalizePlate()` in `Program.cs`
  strips spaces/punctuation and uppercases so formatting differences
  don't create duplicate driver records.

---

## 6. What's intentionally stubbed for now

- **Admin endpoints are behind a single shared API key**, not real
  auth. There are no accounts, roles, or per-moderator audit trail —
  anyone with the key can approve/reject any report. Fine for a solo
  moderator; not fine once there's more than one person moderating.
- **`PhotoReference` is just a string field**, not a real upload
  pipeline. For the MVP, treat it as a placeholder — actual photo
  storage (and the access-control questions that come with storing
  photos of named individuals) is a deliberate next step, not an
  oversight.
- **No messaging bot yet.** This backend is designed to be called by
  one — Telegram first (free, fast to build), WhatsApp Business API
  later once there's funding/need for it.
- **`/api/reports` is rate-limited per IP (5 requests / 10 minutes)**,
  not per driver or account. It stops naive scripted spam, but someone
  spread across multiple IPs (or a shared IP like campus/office wifi
  hitting the limit for everyone behind it) isn't fully covered. Good
  enough as a first line of defense, not a complete answer.

---

## 7. Before any real deployment

- Replace the shared `AdminApiKey` with real per-moderator accounts/roles.
- Get a legal read on data retention/POPIA before storing real reports.
- Move off SQLite to Postgres once you have concurrent users.
- Tune or replace the per-IP rate limit on `/api/reports` once real usage
  patterns exist (see section 6) — 5/10min per IP is a rough starting
  guess, not a measured number.

---

## 8. Roadmap (rough order)

1. ✅ Standalone backend (this repo)
2. ✅ Basic moderator auth on admin endpoints (shared API key)
3. Telegram bot as a thin client calling this API
4. Real photo storage (with access controls)
5. WhatsApp Business API integration (once funded)
