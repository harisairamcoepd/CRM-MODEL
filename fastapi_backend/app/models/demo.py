from __future__ import annotations

from datetime import date
from enum import StrEnum

from sqlalchemy import Date, Enum, ForeignKey, Integer, String, Text, UniqueConstraint
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.base import Base, TimestampMixin


class DemoStatus(StrEnum):
    SCHEDULED = "scheduled"
    COMPLETED = "completed"
    CANCELLED = "cancelled"
    NO_SHOW = "no_show"


class DemoBooking(TimestampMixin, Base):
    __tablename__ = "demo_bookings"
    __table_args__ = (
        UniqueConstraint("demo_date", "time_slot", name="uq_demo_bookings_slot"),
        UniqueConstraint("confirmation_code", name="uq_demo_bookings_confirmation"),
    )

    id: Mapped[int] = mapped_column(Integer, primary_key=True, autoincrement=True)
    lead_id: Mapped[int] = mapped_column(ForeignKey("leads.id", ondelete="CASCADE"), nullable=False, index=True)
    demo_date: Mapped[date] = mapped_column(Date, nullable=False, index=True)
    time_slot: Mapped[str] = mapped_column(String(40), nullable=False, index=True)
    status: Mapped[DemoStatus] = mapped_column(Enum(DemoStatus, name="demo_status"), nullable=False, default=DemoStatus.SCHEDULED)
    confirmation_code: Mapped[str] = mapped_column(String(32), nullable=False)
    meeting_url: Mapped[str | None] = mapped_column(String(255), nullable=True)
    notes: Mapped[str | None] = mapped_column(Text, nullable=True)
    scheduled_by_user_id: Mapped[int | None] = mapped_column(ForeignKey("users.id", ondelete="SET NULL"), nullable=True)

    lead = relationship("Lead", back_populates="demo_bookings")
    scheduled_by = relationship("User", back_populates="scheduled_demos")
