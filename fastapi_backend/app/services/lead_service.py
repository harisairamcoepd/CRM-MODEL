from __future__ import annotations

from datetime import datetime, timedelta, timezone

from sqlalchemy.orm import Session

from app.core.exceptions import AppException
from app.models.lead import Lead, LeadSource, LeadStatus
from app.models.lead_activity import LeadActivity
from app.models.user import User, UserRole
from app.repositories.analytics_repository import AnalyticsRepository
from app.repositories.lead_repository import LeadRepository
from app.repositories.user_repository import UserRepository
from app.services.automation_service import automation_service
from app.services.realtime import event_broker

_SOURCE_MAP = {
    "website": LeadSource.WEBSITE,
    "chatbot": LeadSource.CHATBOT,
    "ads": LeadSource.ADS,
    "manual": LeadSource.MANUAL,
}

_STATUS_MAP = {
    "new": LeadStatus.NEW,
    "contacted": LeadStatus.CONTACTED,
    "demo_booked": LeadStatus.DEMO_BOOKED,
    "demo booked": LeadStatus.DEMO_BOOKED,
    "converted": LeadStatus.CONVERTED,
    "lost": LeadStatus.LOST,
}


class LeadService:
    def __init__(self, db: Session) -> None:
        self.db = db
        self.leads = LeadRepository(db)
        self.users = UserRepository(db)
        self.analytics = AnalyticsRepository(db)

    def _parse_source(self, value: str | None) -> LeadSource:
        if not value:
            return LeadSource.WEBSITE
        normalized = value.strip().lower()
        return _SOURCE_MAP.get(normalized, LeadSource.WEBSITE)

    def _parse_status(self, value: str | None) -> LeadStatus:
        if not value:
            return LeadStatus.NEW
        normalized = value.strip().lower().replace("-", "_")
        return _STATUS_MAP.get(normalized, LeadStatus.NEW)

    def _serialize_lead(self, lead: Lead) -> dict:
        return {
            "id": lead.id,
            "name": lead.name,
            "phone": lead.phone,
            "email": lead.email,
            "location": lead.location,
            "domain": lead.domain,
            "source": lead.source.value,
            "status": lead.status.value,
            "assigned_staff_id": lead.assigned_staff_id,
            "created_at": lead.created_at,
            "updated_at": lead.updated_at,
        }

    def create_lead(self, payload: dict, *, source_hint: str | None = None) -> tuple[dict, bool]:
        source = self._parse_source(source_hint or payload.get("source"))
        status = self._parse_status(payload.get("status"))

        existing = self.leads.get_by_email_phone(email=payload["email"], phone=payload["phone"])
        if existing:
            return self._serialize_lead(existing), False

        assigned_staff_id = payload.get("assigned_staff_id")
        if assigned_staff_id is None:
            least_loaded_staff = self.users.get_least_loaded_staff()
            assigned_staff_id = least_loaded_staff.id if least_loaded_staff else None

        lead = Lead(
            name=payload["name"],
            phone=payload["phone"],
            email=payload["email"],
            location=payload["location"],
            domain=payload["domain"],
            source=source,
            status=status,
            notes=payload.get("notes"),
            assigned_staff_id=assigned_staff_id,
        )

        self.leads.create(lead)
        self.db.flush()

        self.db.add(
            LeadActivity(
                lead_id=lead.id,
                actor_user_id=None,
                activity_type="lead_created",
                current_status=lead.status,
                description="Lead captured from public funnel",
                metadata_json={"source": lead.source.value},
            )
        )

        self.analytics.create_event(
            event_type="lead_created",
            source=lead.source.value,
            lead_id=lead.id,
            payload={"domain": lead.domain, "location": lead.location},
            occurred_at=datetime.now(timezone.utc),
        )

        self.db.commit()
        self.db.refresh(lead)

        automation_service.send_whatsapp_after_lead_capture(lead)

        event_broker.publish(
            {
                "event": "lead_created",
                "leadId": lead.id,
                "source": lead.source.value,
                "createdAt": lead.created_at.isoformat(),
            }
        )

        return self._serialize_lead(lead), True

    def list_leads(self, *, current_user: User, limit: int = 200, offset: int = 0) -> list[dict]:
        if current_user.role == UserRole.ADMIN:
            rows = self.leads.list_all(limit=limit, offset=offset)
        else:
            rows = self.leads.list_assigned_to_staff(current_user.id, limit=limit, offset=offset)
        return [self._serialize_lead(item) for item in rows]

    def list_staff_leads(self, *, staff_user: User, limit: int = 200, offset: int = 0) -> list[dict]:
        rows = self.leads.list_assigned_to_staff(staff_user.id, limit=limit, offset=offset)
        return [self._serialize_lead(item) for item in rows]

    def list_today_leads(self, *, limit: int = 200, offset: int = 0) -> list[dict]:
        now = datetime.now(timezone.utc)
        today_start = datetime(now.year, now.month, now.day, tzinfo=timezone.utc)
        tomorrow_start = today_start + timedelta(days=1)
        rows = self.leads.list_today(today_start=today_start, tomorrow_start=tomorrow_start, limit=limit, offset=offset)
        return [self._serialize_lead(item) for item in rows]

    def get_lead(self, lead_id: int) -> Lead:
        lead = self.leads.get_by_id(lead_id)
        if not lead:
            raise AppException(message="Lead not found", status_code=404, code="lead_not_found")
        return lead

    def update_status(self, *, lead_id: int, status_value: str, actor: User | None) -> dict:
        lead = self.get_lead(lead_id)
        previous_status = lead.status
        current_status = self._parse_status(status_value)

        self.leads.update_status(lead, current_status)
        self.db.add(
            LeadActivity(
                lead_id=lead.id,
                actor_user_id=actor.id if actor else None,
                activity_type="status_updated",
                previous_status=previous_status,
                current_status=current_status,
                description=f"Lead status changed from {previous_status.value} to {current_status.value}",
            )
        )

        self.analytics.create_event(
            event_type="lead_status_updated",
            source="system",
            lead_id=lead.id,
            payload={
                "previous": previous_status.value,
                "current": current_status.value,
            },
            occurred_at=datetime.now(timezone.utc),
        )

        self.db.commit()
        self.db.refresh(lead)

        event_broker.publish(
            {
                "event": "lead_updated",
                "leadId": lead.id,
                "status": lead.status.value,
                "updatedAt": lead.updated_at.isoformat(),
            }
        )

        return self._serialize_lead(lead)

    def assign_staff(self, *, lead_id: int, staff_id: int, actor: User | None) -> dict:
        lead = self.get_lead(lead_id)
        staff = self.users.get_by_id(staff_id)
        if not staff or staff.role != UserRole.STAFF:
            raise AppException(message="Staff user not found", status_code=404, code="staff_not_found")

        lead.assigned_staff_id = staff.id
        self.db.add(
            LeadActivity(
                lead_id=lead.id,
                actor_user_id=actor.id if actor else None,
                activity_type="staff_assigned",
                current_status=lead.status,
                description=f"Lead assigned to {staff.full_name}",
                metadata_json={"staffId": staff.id},
            )
        )

        self.db.commit()
        self.db.refresh(lead)

        event_broker.publish(
            {
                "event": "lead_assigned",
                "leadId": lead.id,
                "staffId": staff.id,
                "updatedAt": lead.updated_at.isoformat(),
            }
        )

        return self._serialize_lead(lead)

    def delete_lead(self, *, lead_id: int, actor: User | None) -> dict:
        lead = self.get_lead(lead_id)

        self.db.add(
            LeadActivity(
                lead_id=lead.id,
                actor_user_id=actor.id if actor else None,
                activity_type="lead_deleted",
                current_status=lead.status,
                description="Lead deleted by admin",
            )
        )

        self.leads.delete(lead)
        self.db.commit()

        event_broker.publish(
            {
                "event": "lead_deleted",
                "leadId": lead_id,
            }
        )

        return {"deleted": True, "id": lead_id}
