# Ticketing System - Frontend

**Framework:** SvelteKit 5 (SSR) + TypeScript  
**Styling:** Bootstrap 5 + Sass  
**Build Tool:** Vite  
**Testing:** Vitest + Testing Library  
**i18n:** svelte-i18n (PL/EN support)

---

## Quick Start

### Prerequisites
- **Node.js** 20+ and **pnpm** 9+
- Backend API running (default: http://localhost:5192)
- PostgreSQL database with seeded data

### Installation

```bash
cd frontend
pnpm install
```

### Development

```bash
# Start dev server
pnpm run dev

# Frontend: http://localhost:3000
# Vite proxies /api/* to backend (configured in vite.config.ts)
```

### Production Build

```bash
pnpm run build
pnpm run preview  # Test production build locally
```

---

## Project Structure

```
frontend/
├── src/
│   ├── lib/                          # Shared library code
│   │   ├── components/               # Reusable Svelte components
│   │   │   ├── ui/                   # UI primitives (Button, Input, etc.)
│   │   │   ├── tables/               # Data tables (TicketsTable, etc.)
│   │   │   ├── modals/               # Modal dialogs
│   │   │   └── layout/               # Layout components (NavRail, TopBar)
│   │   ├── services/                 # API client services
│   │   ├── stores/                   # Svelte stores (toast, modal, categories)
│   │   ├── i18n/                     # Internationalization (PL/EN)
│   │   ├── types/                    # TypeScript type definitions
│   │   │   ├── enums.ts              # Manual static enums (UserRole, TicketStatus, Priority)
│   │   └── utils/                    # Utility functions
│   ├── routes/                       # SvelteKit file-based routing
│   │   ├── (authentication)/         # Login pages (guest access)
│   │   └── app/                      # Protected app pages
│   └── styles/                       # Global styles (Bootstrap customization)
├── tests/                            # Test files (outside src/)
│   ├── unit/                         # Unit tests
│   └── integration/                  # Integration tests
└── static/                           # Static assets (logo, favicon)
```

---

## Key Concepts

### 1. **Backend Enum Synchronization**
Frontend enums are auto-generated from backend API to ensure type safety:

```bashStatic Enums vs Dynamic Categories**
Frontend uses **manual TypeScript enums** for static backend values:
- **UserRole** (Employee, Support, TeamLeader, Admin)
- **TicketStatus** (New, Open, InProgress, Resolved, etc.)
- **Priority** (Low, Medium, High, Critical)

**Categories** are **DB-driven** and fetched from `/api/categories` endpoint (Admin can create/edit/delete categories)

- **Translation files:** `src/lib/i18n/locales/{pl,en}.json`
- **Usage:** `import { getMessage } from '$lib/i18n'`
- **Backend translations:** Auto-synced via `pnpm run sync:translations`

```typescript
// Always use getMessage() for user-facing strings
consCategories:** Admin controls names (namePl/nameEn), displayed based on user's selected locale

```typescript
// Always use getMessage() for user-facing strings
const title = getMessage('ticket_title');

// For categories, use getCategoryName() from categories store
import { getCategoryName } from '$lib/stores/categories';
const categoryName = getCategoryName(ticket.categoryId); // Respects localenly cookie
- `hooks.server.ts` validates JWT and populates `event.locals.user`
- Protected routes redirect to `/login` if unauthenticated

### 4. **API Communication**
All API calls use relative paths `/api/*` (proxied by Vite in dev):

```typescript
import { API_BASE } from '$lib/config';  // '/api'
const response = await fetch(`${API_BASE}/tickets`, { credentials: 'include' });
```

---

## Development Workflow

### Code Quality

```bash
# Type checking
pnpm run check

# Linting
pnpm run lint

# Formatting
pnpm run format
```

---

## Environment Variables

### Development (`.env.development`)

```bash
PUBLIC_API_BASE=/api
LOOKUPS_API=http://localhost:5192
BACKEND_URL=http://localhost:5192
JWT_SECRET=<same-64-character-or-longer-key-used-by-the-development-api>
PUBLIC_DEFAULT_LOCALE=en-US
```

`LOOKUPS_API` configures the Vite development proxy. Server-side application
code prefers `BACKEND_URL` and falls back to `LOOKUPS_API` in development.

### Production runtime

```bash
BACKEND_URL=http://api:8080
JWT_SECRET=<injected-at-runtime>
```

Private values are read through SvelteKit `$env/dynamic/private` by the
adapter-node server. Only `PUBLIC_*` variables are exposed to browser code.

---

## Deployment

### Docker (Recommended)

```bash
# Build image
docker build -f frontend/Dockerfile -t ticketing-frontend .

# Inject private values only when the container starts
docker run -p 3000:3000 \
  -e BACKEND_URL=http://backend:8080 \
  -e JWT_SECRET='<same-64-character-or-longer-key-used-by-the-api>' \
  ticketing-frontend
```

### Node.js (Native)

```bash
pnpm run build
node build  # Runs SvelteKit adapter-node output
```

---
### 1. **Enum Type Errors After Backend Changes**
**Solution:** Re-run `pnpm run sync:lookups` to regenerate enums.

### 2. **API Proxy Not Working**
**Cause:** Vite proxy only works in `pnpm run dev`, not `preview` mode.  
**Solution:** Configure reverse proxy (nginx/Apache) for production.
1. **API Proxy Not Working**
**Cause:** Vite proxy only works in `pnpm run dev`, not `preview` mode.  
**Solution:** Configure reverse proxy (nginx/Apache) for production.

### 2
---

## Additional Documentation

- **Development Setup:** [../README.dev.md](../README.dev.md)
- **Project Plan:** [../PROJECT_PLAN.md](../PROJECT_PLAN.md)
- **Docker Setup:** [../README.docker.md](../README.docker.md)

---

## Tech Stack Details

- **SvelteKit 5** — Full-stack framework with SSR/SPA hybrid
- **TypeScript** — Type-safe development
- **Bootstrap 5** — UI framework with IronPack branding
- **Vite** — Lightning-fast dev server and build tool
- **svelte-i18n** — Internationalization
- **ESLint + Prettier** — Code quality and formatting

---

## License

Internal project for IronPack Sp. z o.o.
