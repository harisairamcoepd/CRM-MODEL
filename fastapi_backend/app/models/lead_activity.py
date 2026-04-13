from __future__ import annotations

from sqlalchemy import Enum, ForeignKey, Integer, JSON, String, Text
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.base import Base, TimestampMixin
from app.models.lead import LeadStatus


class LeadActivity(TimestampMixin, Base):
    __tablename__ = "lead_activities"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, autoincrement=True)
    lead_id: Mapped[int] = mapped_column(ForeignKey("leads.id", ondelete="CASCADE"), nullable=False, index=True)
    actor_user_id: Mapped[int | None] = mapped_column(ForeignKey("users.id", ondelete="SET NULL"), nullable=True, index=True)
    activity_type: Mapped[str] = mapped_column(String(60), nullable=False, index=True)
    previous_status: Mapped[LeadStatus | None] = mapped_column(Enum(LeadStatus, name="lead_status", create_type=False), nullable=True)
    current_status: Mapped[LeadStatus | None] = mapped_column(Enum(LeadStatus, name="lead_status", create_type=False), nullable=True)
    description: Mapped[str | None] = mapped_column(Text, nullable=True)
    metadata_json: Mapped[dict] = mapped_column(JSON, nullable=True)

    lead = relationship("Lead", back_populates="activities")
    actor = relationship("User", back_populates="lead_activities")
