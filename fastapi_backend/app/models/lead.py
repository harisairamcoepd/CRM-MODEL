from __future__ import annotations

from enum import StrEnum

from sqlalchemy import Enum, ForeignKey, Index, Integer, String, Text
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.base import Base, TimestampMixin


class LeadStatus(StrEnum):
    NEW = "new"
    CONTACTED = "contacted"
    DEMO_BOOKED = "demo_booked"
    CONVERTED = "converted"
    LOST = "lost"


class LeadSource(StrEnum):
    WEBSITE = "website"
    CHATBOT = "chatbot"
    ADS = "ads"
    MANUAL = "manual"


class Lead(TimestampMixin, Base):
    __tablename__ = "leads"
    __table_args__ = (
        Index("ix_leads_created_at", "created_at"),
        Index("ix_leads_status_created", "status", "created_at"),
        Index("ix_leads_source_created", "source", "created_at"),
    )

    id: Mapped[int] = mapped_column(Integer, primary_key=True, autoincrement=True)
    name: Mapped[str] = mapped_column(String(120), nullable=False)
    phone: Mapped[str] = mapped_column(String(20), nullable=False, index=True)
    email: Mapped[str] = mapped_column(String(150), nullable=False, index=True)
    location: Mapped[str] = mapped_column(String(120), nullable=False)
    domain: Mapped[str] = mapped_column(String(120), nullable=False, index=True)
    source: Mapped[LeadSource] = mapped_column(Enum(LeadSource, name="lead_source"), nullable=False, default=LeadSource.WEBSITE)
    status: Mapped[LeadStatus] = mapped_column(Enum(LeadStatus, name="lead_status"), nullable=False, default=LeadStatus.NEW)
    notes: Mapped[str | None] = mapped_column(Text, nullable=True)

    assigned_staff_id: Mapped[int | None] = mapped_column(
        ForeignKey("users.id", ondelete="SET NULL"),
        nullable=True,
        index=True,
    )

    assigned_staff = relationship("User", back_populates="assigned_leads")
    demo_bookings = relationship("DemoBooking", back_populates="lead", cascade="all, delete-orphan")
    chat_sessions = relationship("ChatSession", back_populates="lead")
    activities = relationship("LeadActivity", back_populates="lead", cascade="all, delete-orphan")
    analytics_events = relationship("AnalyticsEvent", back_populates="lead")
