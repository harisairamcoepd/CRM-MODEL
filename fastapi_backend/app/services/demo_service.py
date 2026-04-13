from __future__ import annotations

from datetime import datetime, timezone
from uuid import uuid4

from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.core.exceptions import AppException
from app.models.demo import DemoBooking, DemoStatus
from app.models.lead import LeadStatus
from app.models.lead_activity import LeadActivity
from app.models.user import User
from app.repositories.analytics_repository import AnalyticsRepository
from app.repositories.demo_repository import DemoRepository
from app.repositories.lead_repository import LeadRepository
from app.services.automation_service import automation_service
from app.services.realtime import event_broker


class DemoService:
    def __init__(self, db: Session) -> None:
        self.db = db
        self.demos = DemoRepository(db)
        self.leads = LeadRepository(db)
        self.analytics = AnalyticsRepository(db)

    def _build_confirmation_code(self) -> str:
        return f"DMO-{datetime.now(timezone.utc).strftime('%Y%m%d')}-{uuid4().hex[:6].upper()}"

    def check_availability(self, *, date_value, time_slot: str) -> bool:
        return self.demos.is_slot_available(demo_date=date_value, time_slot=time_slot)

    def create_booking(self, *, lead_id: int, date_value, time_slot: str, scheduled_by: User | None = None) -> dict:
        lead = self.leads.get_by_id(lead_id)
        if not lead:
            raise AppException(message="Lead not found for demo booking", status_code=404, code="lead_not_found")

        if not self.check_availability(date_value=date_value, time_slot=time_slot):
            raise AppException(message="Selected demo slot is unavailable", status_code=409, code="slot_unavailable")

        booking = DemoBooking(
            lead_id=lead.id,
            demo_date=date_value,
            time_slot=time_slot,
            status=DemoStatus.SCHEDULED,
            confirmation_code=self._build_confirmation_code(),
            scheduled_by_user_id=scheduled_by.id if scheduled_by else None,
        )

        self.demos.create(booking)

        previous_status = lead.status
        lead.status = LeadStatus.DEMO_BOOKED

        self.db.add(
            LeadActivity(
                lead_id=lead.id,
                actor_user_id=scheduled_by.id if scheduled_by else None,
                activity_type="demo_booked",
                previous_status=previous_status,
                current_status=LeadStatus.DEMO_BOOKED,
                description=f"Demo booked for {date_value.isoformat()} {time_slot}",
                metadata_json={"timeSlot": time_slot},
            )
        )

        self.analytics.create_event(
            event_type="demo_booked",
            source="system",
            lead_id=lead.id,
            payload={"date": date_value.isoformat(), "timeSlot": time_slot},
            occurred_at=datetime.now(timezone.utc),
        )

        try:
            self.db.commit()
        except IntegrityError as exc:
            self.db.rollback()
            raise AppException(message="Selected demo slot is unavailable", status_code=409, code="slot_unavailable") from exc

        self.db.refresh(booking)
        self.db.refresh(lead)

        automation_service.send_demo_confirmation_email(lead, booking)

        event_broker.publish(
            {
                "event": "demo_booked",
                "leadId": lead.id,
                "bookingId": booking.id,
                "demoDate": booking.demo_date.isoformat(),
                "timeSlot": booking.time_slot,
            }
        )

        return {
            "booking": {
                "id": booking.id,
                "leadId": booking.lead_id,
                "date": booking.demo_date,
                "timeSlot": booking.time_slot,
                "status": booking.status.value,
                "confirmationCode": booking.confirmation_code,
                "createdAt": booking.created_at,
            },
            "confirmation": {
                "confirmationCode": booking.confirmation_code,
            },
        }
