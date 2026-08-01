# Ticketing System - Production Overview

**Version:** 1.0  
**Last Updated:** January 12, 2026  
**Status:** Production-Ready

---

## Project Summary

Enterprise ticketing system for IronPack Sp. z o.o. for managing technical issues, failures, and incidents. Fully implements all 8 core IronPack requirements with modern web technologies.

### Tech Stack
- **Backend**: .NET 8, ASP.NET Core Web API, EF Core, PostgreSQL
- **Frontend**: SvelteKit 5 (SSR), TypeScript, Bootstrap 5
- **Auth**: JWT tokens (HttpOnly cookies)
- **Email**: MailKit/SMTP
- **Deployment**: Docker Compose

---

## Core Features

### ✅ All IronPack Requirements Met (8/8)

| # | Requirement | Status |
|---|------------|---------|
| 1 | Submit issues via reporting form | ✅ Complete |
| 2 | Categorization (IT, logistics, administration) | ✅ Complete |
| 3 | Automatic assignment to personnel/departments | ✅ Complete |
| 4 | Ticket statuses (new, in progress, resolved) | ✅ Complete |
| 5 | Logging and history of reports | ✅ Complete |
| 6 | Email notifications about status changes | ✅ Complete |
| 7 | Reports on tickets count, resolution time | ✅ Complete |
| 8 | Intuitive and easy to use interface | ✅ Complete |

### User Roles & Permissions

| Feature | Employee | Support Staff | Team Leader | Admin |
|---------|----------|---------------|-------------|-------|
| My Requests | ✅ | ✅ | ✅ | ✅ |
| All Tickets | ❌ | ❌ | ✅ | ✅ |
| My Workload | ❌ | ✅ | ✅ | ❌ |
| Unassigned Pool | ❌ | ✅ | ❌ | ❌ |
| Team Tickets | ❌ | ❌ | ✅ | ❌ |
| Dashboard | ❌ | ❌ | ✅ | ✅ |
| Admin Panel | ❌ | ❌ | ❌ | ✅ |

**Role Descriptions:**
- **Employee**: Submit and track their own tickets
- **Support Staff**: Process assigned tickets and work from unassigned pool
- **Team Leader**: Oversee team tickets, view dashboard, reassign, manage priorities (category-scoped)
- **Admin**: Full system access including user/category management and reports

---

## Key Capabilities

### Ticket Management
- Create tickets with file attachments (multipart upload)
- Update ticket properties (title, description, category, status, priority, assignment)
- Filter and search tickets (status, category, priority, date range, search text)
- Sort by multiple fields (asc/desc)
- Export tickets to CSV
- Ticket lifecycle: New → Open → In Progress → Resolved/Canceled/Postponed/Returned

### File Attachments
- Upload files with tickets (stored in PostgreSQL bytea)
- Download files with range support (streaming)
- File validation (size, count, content type whitelist)
- Maximum: 5 files per ticket, 10MB per file

### Comments
- Add comments to tickets
- Internal comments (visible to support/admin only)
- Comment validation (content, length)

### Auto-Assignment
- Category-based automatic ticket assignment
- IT → IT Leader, Logistics → Logistics Leader, etc.

### Ticket History
- Complete audit trail (creation, status changes, reassignments, priority changes)
- Chronological timeline with user names and timestamps

### Email Notifications
- **6 notification types**: Assigned, Reassigned, Status Changed, Comment Added, Priority Escalated, Resolved
- **Dual-language**: Sequential Polish/English sections in all emails
- **Recipient deduplication**: No duplicate emails to same person
- **Fire-and-forget**: Email failures don't block ticket operations
- HTML templates with IronPack branding

### Dashboard & Reports
- KPI cards: Total Tickets, Open, In Progress, Resolved, Avg Resolution Time
- Tickets by category/status/priority (bar visualizations)
- Ticket trend analysis (tickets over time)
- Date range filtering
- Role-based access (Admin/TeamLeader)

### Admin Panel
- **Category Management**: CRUD operations, bilingual names (PL/EN), soft/hard delete
- **User Management**: CRUD, password policy, role assignment, category assignment, search/filter

### Internationalization
- **Dual-language**: Polish and English (PL/EN)
- Frontend: svelte-i18n with locale selection
- Backend: Localization service with JSON translations
- Auto-sync: Frontend enums generated from backend API

---

## Security & Production Features

### Security Hardening ✅
- **HTTPS Enforcement**: Redirection + HSTS in production
- **Strong JWT Secret**: Environment variable required (min 64 chars)
- **Password Policy**: Min 8 chars, uppercase, lowercase, digit, special char
- **Rate Limiting**: Login (5/15min), File ops (20/5min), API (100/min)
- **CORS**: Configuration-driven, no wildcards in production
- **Input Validation**: All DTOs validated, no XSS vulnerabilities
- **File Upload Security**: Extension/MIME whitelist, size limits

### Performance ✅
- **Database Indexing**: Status, CreatedAt, composite indexes
- **Query Optimization**: AsNoTracking() on all read-only queries, no N+1 issues
- **Caching**: HTTP cache headers on lookups, IMemoryCache for translations
- **Frontend**: SvelteKit automatic code splitting, lazy-loaded routes

### Monitoring & Logging ✅
- **Structured Logging**: Serilog with console + file sinks (daily rotation, 30-day retention)
- **Health Check**: `/health` endpoint with database connectivity check
- **Performance Metrics**: Request duration tracking, slow request detection (>1s)
- **Log Enrichment**: Machine name, environment, thread ID, user context

### Backup & Recovery ✅
- **Automated Backups**: Quartz.NET scheduled job (daily at 2 AM)
- **Retention Policy**: 30-day automatic cleanup
- **Compression**: gzip (70-80% size reduction)
- **Manual Scripts**: `backup-database.ps1`, `restore-database.ps1`
- **Supports**: Docker and native PostgreSQL

---

## Deployment

### Docker Compose (Recommended)
```bash
# Development
docker compose -f docker-compose.dev.yml up --build

# Production
docker compose up --build
```

**Services:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Database: PostgreSQL on port 5432

### Environment Variables (Production)
```bash
# JWT Authentication (REQUIRED)
JWT__KEY=<strong-secret-64-chars-minimum>

# Database
ConnectionStrings__DefaultConnection=Host=db;Database=ticketing;Username=postgres;Password=<password>

# CORS (REQUIRED)
Cors__AllowedOrigins=https://yourdomain.com,https://app.yourdomain.com

# Email (SMTP)
Email__SmtpHost=smtp.yourdomain.com
Email__SmtpPort=587
Email__SmtpUsername=<username>
Email__SmtpPassword=<password>
Email__FromAddress=noreply@yourdomain.com

# Backup (Optional)
Backup__Enabled=true
Backup__Schedule=0 2 * * *  # Daily at 2 AM
Backup__RetentionDays=30
```

### Initial Setup
1. Start database container
2. API runs migrations automatically on startup
3. Bootstrap admin user (if `ADMIN_EMAIL` and `ADMIN_PASSWORD` env vars provided)
4. Or use dev seeding: Set `SEED_DEMO_DATA=true` for demo data

**Default Admin (Development):**
- Email: `admin@ironpack.pl`
- Password: `admin`

---

## Database Schema

### Main Tables
- **Users**: User accounts with roles, email, password hash
- **Categories**: Ticket categories (IT, Logistics, Administrative) - bilingual names
- **Tickets**: Core ticket data (title, description, status, priority, category, assignment, timestamps)
- **TicketFileMetadata**: File metadata (name, size, content type)
- **TicketFileContents**: Binary file storage (bytea)
- **TicketComments**: Ticket comments (content, internal flag)
- **TicketHistories**: Audit trail (change type, old/new values, user, timestamp)

### Enums
- **TicketStatus**: New, Open, InProgress, Resolved, Canceled, Postponed, Returned
- **TicketPriority**: Low, Medium, High, Critical
- **UserRole**: Admin, TeamLeader, Support, Employee

---

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with email/password
- `POST /api/auth/logout` - Logout (clear cookie)
- `GET /api/auth/me` - Get current user info

### Tickets
- `GET /api/tickets` - List tickets (pagination, filtering, sorting)
- `POST /api/tickets` - Create ticket (multipart/form-data for files)
- `GET /api/tickets/{id}` - Get ticket details
- `PUT /api/tickets/{id}` - Update ticket
- `DELETE /api/tickets/{id}` - Delete ticket
- `GET /api/tickets/{id}/comments` - List comments
- `POST /api/tickets/{id}/comments` - Add comment
- `GET /api/tickets/{id}/files` - List attachments
- `GET /api/tickets/{id}/files/{fileId}` - Download file
- `GET /api/tickets/{id}/history` - Get ticket history
- `GET /api/tickets/export` - Export to CSV

### Reports
- `GET /api/reports/dashboard` - Dashboard stats
- `GET /api/reports/tickets-by-category` - Category breakdown
- `GET /api/reports/tickets-by-status` - Status breakdown
- `GET /api/reports/ticket-trend` - Trend analysis

### Categories (Admin)
- `GET /api/categories` - List categories
- `POST /api/categories` - Create category
- `PUT /api/categories/{id}` - Update category
- `DELETE /api/categories/{id}` - Delete category

### Users (Admin)
- `GET /api/users` - List users (search, filter, pagination)
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Lookups
- `GET /api/lookups` - Get all enums (for frontend sync)
- `GET /api/lookups/translations/{locale}` - Get translations (en/pl)

### Health
- `GET /health` - Health check (database connectivity)

---

## Quality Assurance

### Testing Coverage
**Backend Testing** (Completed during development):
- **110+ Automated Tests**: Unit tests for services, validators, and utilities
- **25+ Integration Tests**: Controllers and critical workflows
- **Coverage Areas**:
  - Ticket CRUD operations and lifecycle transitions
  - Authentication and authorization (role-based access)
  - File upload/download with validation
  - Email notification triggers
  - Category management and user management
  - Dashboard and reporting calculations
  - Ticket history audit trail
- **Test Status**: 100% of tests passing

**Frontend Testing** (Completed during development):
- Component and service testing
- User interface interactions and state management
- i18n functionality and locale switching
- API integration and error handling
- Responsive design across viewport sizes

---

## Project Structure

```
ticketing-system/
├── backend/
│   ├── TicketingSystem.Api/              # Main API project
│   ├── TicketingSystem.Api.Tests/        # Unit tests
│   └── TicketingSystem.Api.IntegrationTests/  # Integration tests
├── frontend/
│   ├── src/
│   │   ├── routes/                       # SvelteKit routes
│   │   ├── lib/                          # Components, services, stores
│   │   └── ...
│   └── static/                           # Static assets
├── scripts/
│   ├── backup-database.ps1               # Backup script
│   ├── restore-database.ps1              # Restore script
│   ├── seed-full.ps1                     # Seed demo data
│   ├── clean-database.ps1                # Clean database
│   ├── reset-seed-and-check.ps1          # Orchestrator script
│   └── check-database-state.ps1          # Database state verification
├── docs/
│   └── AttachementFiles/                 # Seed data and attachments
├── docker-compose.yml                    # Production Docker config
├── docker-compose.dev.yml                # Development Docker config
├── README.dev.md                         # Development setup guide
├── README.docker.md                      # Docker deployment guide
├── README.hybrid.md                      # Hybrid deployment guide
└── PRODUCTION_OVERVIEW.md                # This file
```

---

## Known Limitations & Future Enhancements

### Out of Scope (Not Implemented)
- Mobile native apps (iOS/Android)
- Real-time chat/messaging
- Third-party integrations (CRM, ERP)
- AI-based ticket classification
- Knowledge base / FAQ system
- OAuth/SSO integration
- WebSocket/SignalR for live updates

### Nice-to-Have (Future Versions)
- SLA monitoring with automatic escalation
- Bulk operations (status change, reassignment)
- Advanced saved filters (user presets)
- In-app notification center
- Search within comments
- User notification preferences
- Admin notification configuration
- Email template customization
- Single-language email preference

---

## Documentation

### Setup & Deployment Guides
- **Development Setup**: [README.dev.md](README.dev.md)
- **Docker Deployment**: [README.docker.md](README.docker.md)
- **Hybrid Deployment**: [README.hybrid.md](README.hybrid.md)

### Configuration Files
- **Email Templates**: `backend/TicketingSystem.Api/EmailTemplates/` (HTML templates with IronPack branding)
- **Translations**: 
  - Frontend: `frontend/src/lib/i18n/locales/{en,pl}.json`
  - Backend: `backend/TicketingSystem.Api/Localization/translations.{en,pl}.json`
- **Seed Data**: `docs/AttachementFiles/` (test data for demo setup)

---

## Deployment Checklist

### Pre-Deployment Configuration
- [ ] Set strong JWT secret (min 64 chars) via `JWT__KEY` environment variable
- [ ] Configure CORS allowed origins (e.g., `https://yourdomain.com`)
- [ ] Set up SMTP credentials for email notifications
- [ ] Configure PostgreSQL connection string
- [ ] Review and adjust file upload limits in `appsettings.json`
- [ ] Enable automated backups: `Backup__Enabled=true`
- [ ] Test backup/restore procedures
- [ ] Configure HTTPS certificates for production domain
- [ ] Set rate limiting thresholds if needed

### Post-Deployment Verification
- [ ] Health check endpoint responds: `GET /health`
- [ ] Authentication flow works (login/logout)
- [ ] Admin user created and can access Admin Panel
- [ ] Categories configured for your organization
- [ ] Email notifications send correctly
- [ ] File uploads and downloads work
- [ ] All user roles have appropriate access (Employee, Support, TeamLeader, Admin)
- [ ] Dashboard and reports display data
- [ ] Log files are being created and rotated
- [ ] Backup jobs execute as scheduled

---

**Project Status:** Production-Ready ✅  
**Completion:** 99% (Only deployment configuration remains)

