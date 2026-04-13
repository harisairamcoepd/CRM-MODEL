-- COEPD AI Funnel OS - PostgreSQL schema
-- Run with: psql -U postgres -d coepd_ai_funnel -f sql/schema.sql

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'user_role') THEN
        CREATE TYPE user_role AS ENUM ('admin', 'staff');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'lead_source') THEN
        CREATE TYPE lead_source AS ENUM ('website', 'chatbot', 'ads', 'manual');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'lead_status') THEN
        CREATE TYPE lead_status AS ENUM ('new', 'contacted', 'demo_booked', 'converted', 'lost');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'demo_status') THEN
        CREATE TYPE demo_status AS ENUM ('scheduled', 'completed', 'cancelled', 'no_show');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'chat_channel') THEN
        CREATE TYPE chat_channel AS ENUM ('web_widget', 'api');
    END IF;
END
$$;

CREATE TABLE IF NOT EXISTS users (
    id BIGSERIAL PRIMARY KEY,
    full_name VARCHAR(120) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role user_role NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_sessions (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_jti VARCHAR(120) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ NULL,
    ip_address VARCHAR(64) NULL,
    user_agent VARCHAR(255) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_user_sessions_user_id ON user_sessions(user_id);

CREATE TABLE IF NOT EXISTS leads (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(120) NOT NULL,
    phone VARCHAR(20) NOT NULL,
    email VARCHAR(150) NOT NULL,
    location VARCHAR(120) NOT NULL,
    domain VARCHAR(120) NOT NULL,
    source lead_source NOT NULL DEFAULT 'website',
    status lead_status NOT NULL DEFAULT 'new',
    notes TEXT NULL,
    assigned_staff_id BIGINT NULL REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_leads_phone ON leads(phone);
CREATE INDEX IF NOT EXISTS ix_leads_email ON leads(email);
CREATE INDEX IF NOT EXISTS ix_leads_domain ON leads(domain);
CREATE INDEX IF NOT EXISTS ix_leads_assigned_staff_id ON leads(assigned_staff_id);
CREATE INDEX IF NOT EXISTS ix_leads_created_at ON leads(created_at);
CREATE INDEX IF NOT EXISTS ix_leads_status_created ON leads(status, created_at);
CREATE INDEX IF NOT EXISTS ix_leads_source_created ON leads(source, created_at);

CREATE TABLE IF NOT EXISTS demo_bookings (
    id BIGSERIAL PRIMARY KEY,
    lead_id BIGINT NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    demo_date DATE NOT NULL,
    time_slot VARCHAR(40) NOT NULL,
    status demo_status NOT NULL DEFAULT 'scheduled',
    confirmation_code VARCHAR(32) NOT NULL UNIQUE,
    meeting_url VARCHAR(255) NULL,
    notes TEXT NULL,
    scheduled_by_user_id BIGINT NULL REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_demo_bookings_slot UNIQUE (demo_date, time_slot)
);
CREATE INDEX IF NOT EXISTS ix_demo_bookings_lead_id ON demo_bookings(lead_id);
CREATE INDEX IF NOT EXISTS ix_demo_bookings_demo_date ON demo_bookings(demo_date);
CREATE INDEX IF NOT EXISTS ix_demo_bookings_time_slot ON demo_bookings(time_slot);

CREATE TABLE IF NOT EXISTS lead_activities (
    id BIGSERIAL PRIMARY KEY,
    lead_id BIGINT NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    actor_user_id BIGINT NULL REFERENCES users(id) ON DELETE SET NULL,
    activity_type VARCHAR(60) NOT NULL,
    previous_status lead_status NULL,
    current_status lead_status NULL,
    description TEXT NULL,
    metadata_json JSONB NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_lead_activities_lead_id ON lead_activities(lead_id);
CREATE INDEX IF NOT EXISTS ix_lead_activities_actor_user_id ON lead_activities(actor_user_id);
CREATE INDEX IF NOT EXISTS ix_lead_activities_activity_type ON lead_activities(activity_type);

CREATE TABLE IF NOT EXISTS chat_sessions (
    id BIGSERIAL PRIMARY KEY,
    session_key VARCHAR(64) NOT NULL UNIQUE,
    lead_id BIGINT NULL REFERENCES leads(id) ON DELETE SET NULL,
    channel chat_channel NOT NULL DEFAULT 'web_widget',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    ended_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_chat_sessions_lead_id ON chat_sessions(lead_id);

CREATE TABLE IF NOT EXISTS chat_messages (
    id BIGSERIAL PRIMARY KEY,
    chat_session_id BIGINT NOT NULL REFERENCES chat_sessions(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_chat_messages_chat_session_id ON chat_messages(chat_session_id);

CREATE TABLE IF NOT EXISTS analytics_events (
    id BIGSERIAL PRIMARY KEY,
    event_type VARCHAR(80) NOT NULL,
    source VARCHAR(40) NOT NULL DEFAULT 'system',
    lead_id BIGINT NULL REFERENCES leads(id) ON DELETE SET NULL,
    payload JSONB NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_analytics_events_event_type ON analytics_events(event_type);
CREATE INDEX IF NOT EXISTS ix_analytics_events_source ON analytics_events(source);
CREATE INDEX IF NOT EXISTS ix_analytics_events_lead_id ON analytics_events(lead_id);
CREATE INDEX IF NOT EXISTS ix_analytics_events_type_time ON analytics_events(event_type, occurred_at);
CREATE INDEX IF NOT EXISTS ix_analytics_events_source_time ON analytics_events(source, occurred_at);

COMMIT;
