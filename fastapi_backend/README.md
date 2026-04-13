# COEPD AI Funnel OS - FastAPI Backend

Production-ready FastAPI backend for the CRM funnel system.

## Stack
- FastAPI + Uvicorn
- SQLAlchemy 2.0
- PostgreSQL (`psycopg`)
- JWT auth (Admin/Staff roles)
- Realtime events (SSE)

## 1) Setup

```powershell
cd fastapi_backend
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
```

Update `.env` with real values:
- `DATABASE_URL`
- `JWT_SECRET_KEY`
- seeded user credentials
- WhatsApp values if enabled

## 2) Create Database

```sql
CREATE DATABASE coepd_ai_funnel;
```

You can either:
1. Let SQLAlchemy auto-create tables on startup (default).
2. Apply SQL schema manually:

```powershell
psql -U postgres -d coepd_ai_funnel -f sql/schema.sql
```

## 3) Run API

```powershell
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

ASGI app path is `app.main:app`.

## 4) Frontend Integration

If you open static files from `dashboard/` via file protocol, frontend JS uses:
- `http://localhost:8000`

Main endpoints used by landing/dashboard:
- `POST /api/leads`
- `POST /api/demo` (legacy alias)
- `POST /api/demo-booking`
- `GET /api/demo/availability?date=YYYY-MM-DD&timeSlot=Morning`
- `GET /api/stats`
- `GET /api/stats/growth`
- `GET /api/events/stream`

## 5) Auth APIs

- `POST /api/auth/login`
- `GET /api/auth/me`
- `POST /api/auth/logout`

Default seeded users come from `.env`:
- `ADMIN_EMAIL` / `ADMIN_PASSWORD`
- `STAFF_EMAIL` / `STAFF_PASSWORD`

## 6) Admin + Staff APIs

- `GET /api/leads` (Admin: all, Staff: assigned)
- `PATCH /api/leads/{lead_id}/status`
- `PATCH /api/leads/{lead_id}/assign` (Admin)
- `DELETE /api/leads/{lead_id}` (Admin)
- `GET /api/admin/leads`
- `GET /api/admin/leads/today`
- `GET /api/admin/lead-stats`
- `GET /api/admin/lead-growth?days=14`
- `GET /api/staff/leads`
- `PATCH /api/staff/leads/{lead_id}/status`

## 7) Chatbot Module APIs

- `POST /api/chatbot/sessions`
- `POST /api/chatbot/messages`

## 8) Health + Docs

- `GET /health`
- Swagger UI: `/docs`

## 9) Optional Alembic Migration Commands

```powershell
pip install alembic
alembic init migrations
alembic revision --autogenerate -m "initial schema"
alembic upgrade head
```

## 10) Deployment Notes

- Use environment variables only (no hardcoded secrets in frontend).
- Put API behind HTTPS reverse proxy.
- Set strong `JWT_SECRET_KEY` (32+ chars).
- Restrict CORS to trusted origins in production.
