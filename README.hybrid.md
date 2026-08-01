# Ticketing System — Hybrid Development Setup

This guide explains how to run the antire system in hybrid mode: run DB in Docker, run API and frontend locally.

---

Install these once:

### Docker
- Install Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Verify (in bash):
  docker --version

### .NET 8 SDK
- Download from: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- Verify (in bash):
  dotnet --version

### Node.js + pnpm
- Install Node 20.x or higher:
  https://nodejs.org/en/download
- Install pnpm globally:
  npm i -g pnpm
- Verify (in bash):
  node -v
  pnpm -v

### PostgreSQL in Docker
- Run this once to create a DB container:
  docker run --name ticketing-pg \
  -e POSTGRES_USER=admin \
  -e POSTGRES_PASSWORD=admin \
  -e POSTGRES_DB=ticketing_system \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  -d postgres:16
---

# ============================================================================================================
## Quick Start
# ============================================================================================================
# Backend (ASP.NET 8)
# ------------------------------------------------------------------------------------------------------------
cd backend/TicketingSystem.Api
dotnet restore

# Configure connection string - Edit appsettings.Development.json:
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ticketing_system;Username=admin;Password=admin"
  }

# Run API (API will start on something like http://localhost:5192 or similar (see console output)):
  dotnet watch
# ------------------------------------------------------------------------------------------------------------
# Frontend (SvelteKit)
# ------------------------------------------------------------------------------------------------------------
cd frontend
pnpm install

# Create a .env.local file:
LOOKUPS_API=http://localhost:5192
PUBLIC_API_BASE=/api

# Start the frontend (Open http://localhost:3000)
pnpm run dev
# ------------------------------------------------------------------------------------------------------------


Summary:
===================================================
Component             | Runs    | Required Install
===================================================
PostgreSQL            | Docker  | Docker
---------------------------------------------------
API (.NET)            | Local   | .NET SDK
---------------------------------------------------
Frontend (SvelteKit)  | Local   | Node.js + pnpm
---------------------------------------------------


===================================================
# Usefull commands:
===================================================
# Stop/ restart DB
docker stop ticketing-pg
docker start ticketing-pg
# remove DB (and data):
docker rm -f ticketing-pg
docker volume rm pgdata


### Recommended Workflow
# Start DB:
  docker start ticketing-pg
# Run backend:
  dotnet watch
# Run frontend:
  pnpm run dev