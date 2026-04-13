from __future__ import annotations

from datetime import date

from sqlalchemy import and_, func, select
from sqlalchemy.orm import Session

from app.models.demo import DemoBooking


class DemoRepository:
    def __init__(self, db: Session) -> None:
        self.db = db

    def create(self, booking: DemoBooking) -> DemoBooking:
        self.db.add(booking)
        return booking

    def is_slot_available(self, *, demo_date: date, time_slot: str) -> bool:
        statement = select(func.count(DemoBooking.id)).where(
            and_(
                DemoBooking.demo_date == demo_date,
                DemoBooking.time_slot == time_slot,
            )
        )
        count = self.db.execute(statement).scalar_one()
        return count == 0

    def count_total(self) -> int:
        statement = select(func.count(DemoBooking.id))
        return int(self.db.execute(statement).scalar_one() or 0)

    def count_today(self, today: date) -> int:
        statement = select(func.count(DemoBooking.id)).where(DemoBooking.demo_date == today)
        return int(self.db.execute(statement).scalar_one() or 0)

    def list_recent(self, limit: int = 100) -> list[DemoBooking]:
        statement = select(DemoBooking).order_by(DemoBooking.created_at.desc()).limit(limit)
        return list(self.db.execute(statement).scalars().all())
