# Ticketing System — Dev Setup

Microservices-style web app: **Backend** (.NET 8 Web API + EF Core + PostgreSQL) and **Frontend** (SvelteKit + Vite). This guide shows how to run it locally with Docker (recommended) or with native SDKs.

---

## Prerequisites

**Option A (recommended):** Docker Desktop (Windows/Mac) or Docker Engine (Linux).  
**Option B (native):**
- .NET 8 SDK
- Node.js 20.x
- pnpm (`npm i -g pnpm`)
- PostgreSQL 16 (if not using Docker for DB)

---

## Quick Start (Docker — hot reload FE via Vite proxy)

1) Create a `docker-compose.dev.yml` at the repo root with the following content:

```yaml
version: "3.8"
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: admin
      POSTGRES_DB: ticketing_system
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data

  api:
    build: ./backend/TicketingSystem.Api
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://0.0.0.0:8080
      # IMPORTANT: this overrides ConnectionStrings:DefaultConnection in appsettings.json
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=ticketing_system;Username=admin;Password=admin
    depends_on:
      - db
    ports:
      - "5000:8080"

  frontend:
    build: ./frontend
    working_dir: /app
    command: >
      sh -c "pnpm install && pnpm run dev -- --host 0.0.0.0 --port 3000"
    environment:
      # Vite dev proxy target for /api -> api container
      LOOKUPS_API: http://api:8080
      # Browser-side base path for API calls in the app
      PUBLIC_API_BASE: /api
      PUBLIC_DEFAULT_LOCALE: en-US
    depends_on:
      - api
    ports:
      - "3000:3000"

volumes:
  postgres-data:
```

2) Start the stack (in bash):
docker compose -f docker-compose.dev.yml up --build
```

- API available at: **http://localhost:5000/swagger**
- Frontend at: **http://localhost:3000**
- FE dev server proxies any **/api** request to the backend via `LOOKUPS_API`

> If ports are in use, adjust the host ports on the left-hand side of the mappings (e.g., `6000:8080`).

---

## Alternative: Native Run (without Docker)

**Database (Docker only for DB):**
```bash
docker run --name ticketing-pg -e POSTGRES_USER=admin -e POSTGRES_PASSWORD=admin -e POSTGRES_DB=ticketing_system -p 5432:5432 -v pgdata:/var/lib/postgresql/data -d postgres:16
```

**Backend:**
```bash
cd backend/TicketingSystem.Api
dotnet restore
# Ensure appsettings.json has a valid ConnectionStrings:DefaultConnection pointing to localhost:5432
dotnet ef database update  # if migrations exist
dotnet run
# API -> http://localhost:5192 (or as shown in your launch profile)
```

**Frontend:**
```bash
cd frontend
pnpm install
# The dev server proxies /api to LOOKUPS_API (defaults to http://localhost:5192). Override if needed:
LOOKUPS_API=http://localhost:5000 pnpm run dev
# FE -> http://localhost:3000
```

---

## Environment Variables

**Backend (.NET)**  
- `ASPNETCORE_ENVIRONMENT`: `Development` in dev
- `ConnectionStrings__DefaultConnection`: override DB connection (used in Docker)
- `ASPNETCORE_URLS`: set to `http://0.0.0.0:8080` in containers

**Frontend (SvelteKit + Vite)**  
- `PUBLIC_API_BASE`: base path used in the browser, typically `/api` (do **not** put the full host here)  
- `LOOKUPS_API`: backend URL for Vite dev proxy, e.g. `http://api:8080` (in Docker) or `http://localhost:5192` (native)

> Note: `PUBLIC_*` vars are exposed to the browser by Vite; others are server-only.

---

## Project Structure (simplified)

```
backend/
  TicketingSystem.Api/
    Dockerfile
    appsettings.json
    appsettings.Development.json
    Properties/launchSettings.json
    ...
frontend/
  Dockerfile
  package.json
  vite.config.ts
  .env.development
  .env.production
  src/...
docker-compose.dev.yml   # (you create this)
```

---

## Common Troubleshooting

- **Frontend shows 404 or cannot reach API**  
  Ensure you run the dev server (not preview) so the Vite proxy is active, and `LOOKUPS_API` points to the backend.

- **Backend can't connect to DB (in Docker)**  
  Confirm `ConnectionStrings__DefaultConnection` uses `Host=db;Port=5432` and the `db` service is healthy.

- **Port already in use**  
  Change host ports in compose (e.g., `5001:8080`, `3001:3000`).

- **EF migrations missing**  
  Add migrations if needed: `dotnet ef migrations add InitialCreate` and `dotnet ef database update`.

---

## Team Workflow

1. Clone the repo: `git clone <repo-url>`  
2. Copy any example env files if provided (e.g., `cp frontend/.env.development.example frontend/.env.development`)  
3. Use **Docker Quick Start** (recommended), or the native run.  
4. Commit code without secrets — `.gitignore` is configured to prevent committing local/running artifacts.

---

## Notes

- The provided `docker-compose.yml` in the repo root may be a placeholder. Prefer the `docker-compose.dev.yml` above for development with hot reload + proxy.
- Do **not** commit real `.env` files or DB data directories.