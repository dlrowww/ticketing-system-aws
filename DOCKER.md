# Local Docker development

Local development has one supported Compose file:

```bash
docker compose -f docker-compose.local.yml up --build
```

It starts PostgreSQL, runs the one-shot database migration/seed process, then
starts the API and Frontend development servers. Backend source changes are
handled by `dotnet watch`; Frontend source changes are handled by Vite HMR.

Local endpoints:

- Frontend: <http://localhost:3000>
- API: <http://localhost:5000>
- PostgreSQL: `localhost:5432`

Stop the stack without deleting the database:

```bash
docker compose -f docker-compose.local.yml down
```

To intentionally reset all local database and dependency volumes:

```bash
docker compose -f docker-compose.local.yml down --volumes
```

`frontend/Dockerfile` and `backend/TicketingSystem.Api/Dockerfile` remain the
production image definitions used by Kubernetes. Their `.dev` counterparts are
only for local hot-reload development. `deploy/docker-compose.yml` is retained
as a legacy non-AWS reference; AWS production deployment uses `k8s_deploy`.
