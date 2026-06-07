# Job Portal

Full-stack job portal: candidates browse jobs and apply; recruiters post jobs and manage applicants. Built with **ASP.NET Core 8**, **Angular 21**, **PostgreSQL**, and **JWT** auth.

## Tech stack

- **Backend:** .NET 8 Web API, EF Core, JWT, BCrypt, Swagger
- **Frontend:** Angular 21 (standalone components, signals, HTTP interceptor)
- **Database:** PostgreSQL (Neon or Supabase)

## Local setup

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- PostgreSQL connection string (free [Neon](https://neon.tech) or [Supabase](https://supabase.com) account)

### Backend

1. Copy the example config:
   ```powershell
   cd backend\JobPortalWebsite\JobPortal.API
   copy appsettings.Development.example.json appsettings.Development.json
   ```
2. Edit `appsettings.Development.json` — set the PostgreSQL connection string and JWT key (never commit this file).
3. Apply migrations:
   ```powershell
   dotnet ef database update
   ```
4. Run the API:
   ```powershell
   dotnet run
   ```
   Swagger: `https://localhost:7011/swagger`

### Frontend

```powershell
cd frontend
npm install
ng serve
```

Open `http://localhost:4200`

## Deploy (free tier)

| Part | Host |
|------|------|
| Angular | [Vercel](https://vercel.com) — root: `frontend` |
| API | [Render](https://render.com) — use `Dockerfile` |
| Database | Neon or Supabase |

### Render environment variables

| Key | Value |
|-----|--------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | Long secret (32+ chars) |
| `Jwt__Issuer` | `JobPortalAPI` |
| `Jwt__Audience` | `JobPortalClient` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `FileStorage__PublicBaseUrl` | `https://<api-name>.onrender.com` |
| `Cors__AllowedOrigins__0` | `https://<app-name>.vercel.app` |

After Render deploy, set `apiUrl` in `frontend/src/environments/environment.production.ts` to the Render API URL, then deploy Vercel.

### Cloud database migrations

```powershell
$env:ConnectionStrings__DefaultConnection="<postgres-connection-string>"
dotnet ef database update
```

## Live demo

_Add the live Vercel URL here after deploy._
