# Ticketing System — Docker Development Setup

This guide explains how to run the entire system using **Docker Compose** — database, backend, and frontend — without installing .NET or Node locally.

---

## Prerequisites
- Docker Desktop (Windows/Mac) or Docker Engine (Linux)

---

## Quick Start

1. Make sure you have this file in the root:
  docker-compose.dev.yml
2. Build and start all services:
  docker compose -f docker-compose.dev.yml up --build
3. Open in your browser:
  Frontend: http://localhost:3000
  Backend (Swagger): http://localhost:5000/swagger


Services Overview:
===================================================
Service   | Tech            | Port  | Description
===================================================
db        | PostgreSQL 16   | 5432  | DB container
---------------------------------------------------
api       | ASP.NET 8       | 5000  | WEB API
---------------------------------------------------
frontend  | SvelteKit + Vite| 3000  | FE dev server
---------------------------------------------------

## Env variables:
# BE
- ASPNETCORE_ENVIRONMENT=Development
- ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=ticketing_system;Username=admin;Password=admin
# FE
- LOOKUPS_API=http://api:8080
- PUBLIC_API_BASE=/api

## Volumes:
postgres-data - persistnet DB volume
# To clear DB data:
docker compose -f docker-compose.dev.yml down -v


===================================================
# Usefull docker commands:
===================================================
# Stop containers
docker compose down
---------------------------------------------------
# Rebuild images after code change in Dockerfiles
docker compose build --no-cache
---------------------------------------------------
# View logs
docker compose logs -f api
---------------------------------------------------