from __future__ import annotations

from datetime import datetime, timezone

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.models.analytics import AnalyticsEvent


class AnalyticsRepository:
    def __init__(self, db: Session) -> None:
        self.db = db

    def create_event(
        self,
        *,
        event_type: str,
        source: str,
        lead_id: int | None = None,
        payload: dict | None = None,
        occurred_at: datetime | None = None,
    ) -> AnalyticsEvent:
        event = AnalyticsEvent(
            event_type=event_type,
            source=source,
            lead_id=lead_id,
            payload=payload,
            occurred_at=occurred_at or datetime.now(timezone.utc),
        )
        self.db.add(event)
        return event

    def list_recent(self, limit: int = 100) -> list[AnalyticsEvent]:
        statement = select(AnalyticsEvent).order_by(AnalyticsEvent.occurred_at.desc()).limit(limit)
        return list(self.db.execute(statement).scalars().all())
