from __future__ import annotations

from collections import Counter
from datetime import datetime, timedelta, timezone

from sqlalchemy import and_, func, select
from sqlalchemy.orm import Session

from app.models.lead import Lead, LeadSource, LeadStatus


class LeadRepository:
    def __init__(self, db: Session) -> None:
        self.db = db

    def create(self, lead: Lead) -> Lead:
        self.db.add(lead)
        return lead

    def get_by_id(self, lead_id: int) -> Lead | None:
        statement = select(Lead).where(Lead.id == lead_id)
        return self.db.execute(statement).scalar_one_or_none()

    def get_by_email_phone(self, *, email: str, phone: str) -> Lead | None:
        statement = select(Lead).where(and_(Lead.email == email, Lead.phone == phone))
        return self.db.execute(statement).scalar_one_or_none()

    def list_all(self, limit: int = 200, offset: int = 0) -> list[Lead]:
        statement = select(Lead).order_by(Lead.created_at.desc()).offset(offset).limit(limit)
        return list(self.db.execute(statement).scalars().all())

    def list_assigned_to_staff(self, staff_id: int, limit: int = 200, offset: int = 0) -> list[Lead]:
        statement = (
            select(Lead)
            .where(Lead.assigned_staff_id == staff_id)
            .order_by(Lead.created_at.desc())
            .offset(offset)
            .limit(limit)
        )
        return list(self.db.execute(statement).scalars().all())

    def list_today(self, *, today_start: datetime, tomorrow_start: datetime, limit: int = 200, offset: int = 0) -> list[Lead]:
        statement = (
            select(Lead)
            .where(and_(Lead.created_at >= today_start, Lead.created_at < tomorrow_start))
            .order_by(Lead.created_at.desc())
            .offset(offset)
            .limit(limit)
        )
        return list(self.db.execute(statement).scalars().all())

    def delete(self, lead: Lead) -> None:
        self.db.delete(lead)

    def count_total(self) -> int:
        statement = select(func.count(Lead.id))
        return int(self.db.execute(statement).scalar_one() or 0)

    def count_today(self, today_start: datetime, tomorrow_start: datetime) -> int:
        statement = select(func.count(Lead.id)).where(and_(Lead.created_at >= today_start, Lead.created_at < tomorrow_start))
        return int(self.db.execute(statement).scalar_one() or 0)

    def count_this_month(self, month_start: datetime, next_month_start: datetime) -> int:
        statement = select(func.count(Lead.id)).where(and_(Lead.created_at >= month_start, Lead.created_at < next_month_start))
        return int(self.db.execute(statement).scalar_one() or 0)

    def count_by_status(self, status: LeadStatus) -> int:
        statement = select(func.count(Lead.id)).where(Lead.status == status)
        return int(self.db.execute(statement).scalar_one() or 0)

    def count_by_period(self, start: datetime, end: datetime) -> int:
        statement = select(func.count(Lead.id)).where(and_(Lead.created_at >= start, Lead.created_at < end))
        return int(self.db.execute(statement).scalar_one() or 0)

    def source_breakdown(self) -> dict[str, int]:
        statement = select(Lead.source, func.count(Lead.id)).group_by(Lead.source)
        rows = self.db.execute(statement).all()
        result: dict[str, int] = {}
        for source, count in rows:
            label = source.value if isinstance(source, LeadSource) else str(source)
            result[label.capitalize()] = int(count or 0)
        return result

    def domain_breakdown(self, limit: int = 10) -> dict[str, int]:
        statement = (
            select(Lead.domain, func.count(Lead.id).label("count"))
            .group_by(Lead.domain)
            .order_by(func.count(Lead.id).desc())
            .limit(limit)
        )
        rows = self.db.execute(statement).all()
        return {str(domain): int(count or 0) for domain, count in rows}

    def growth_points(self, days: int) -> list[dict[str, int | str]]:
        today = datetime.now(timezone.utc).date()
        start_date = today - timedelta(days=days - 1)
        start_dt = datetime.combine(start_date, datetime.min.time(), tzinfo=timezone.utc)

        statement = select(Lead.created_at).where(Lead.created_at >= start_dt)
        rows = self.db.execute(statement).scalars().all()

        bucket = Counter()
        for created_at in rows:
            bucket[created_at.date().isoformat()] += 1

        points: list[dict[str, int | str]] = []
        for offset in range(days):
            date_value = start_date + timedelta(days=offset)
            iso_date = date_value.isoformat()
            points.append(
                {
                    "date": iso_date,
                    "label": date_value.strftime("%b %d"),
                    "count": int(bucket.get(iso_date, 0)),
                }
            )

        return points

    def update_status(self, lead: Lead, status: LeadStatus) -> Lead:
        lead.status = status
        return lead

    def open_lead_count_for_staff(self, staff_id: int) -> int:
        statement = select(func.count(Lead.id)).where(
            and_(
                Lead.assigned_staff_id == staff_id,
                Lead.status.notin_([LeadStatus.CONVERTED, LeadStatus.LOST]),
            )
        )
        return int(self.db.execute(statement).scalar_one() or 0)
