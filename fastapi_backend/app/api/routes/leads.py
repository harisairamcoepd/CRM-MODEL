from __future__ import annotations

from fastapi import APIRouter, Depends, Header, Query
from sqlalchemy.orm import Session

from app.api.deps import get_current_admin, get_current_staff_or_admin
from app.core.database import get_db
from app.models.user import User
from app.schemas.lead import LeadAssignRequest, LeadCreateRequest, LeadStatusUpdateRequest
from app.services.lead_service import LeadService

router = APIRouter(prefix="/api/leads", tags=["leads"])


@router.post("")
def create_lead(
    payload: LeadCreateRequest,
    db: Session = Depends(get_db),
    x_client_app: str | None = Header(default=None, alias="X-Client-App"),
) -> dict:
    source_hint = "chatbot" if (x_client_app or "").strip().lower() == "chatbot" else payload.source
    lead, created = LeadService(db).create_lead(payload.model_dump(), source_hint=source_hint)
    return {
        "lead": lead,
        "created": created,
        "message": "Lead created" if created else "Lead already exists",
    }


@router.get("")
def list_leads(
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_staff_or_admin),
    limit: int = Query(default=200, ge=1, le=500),
    offset: int = Query(default=0, ge=0),
) -> dict:
    items = LeadService(db).list_leads(current_user=current_user, limit=limit, offset=offset)
    return {"items": items, "total": len(items)}


@router.patch("/{lead_id}/status")
def update_lead_status(
    lead_id: int,
    payload: LeadStatusUpdateRequest,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_staff_or_admin),
) -> dict:
    lead = LeadService(db).update_status(lead_id=lead_id, status_value=payload.status, actor=current_user)
    return {"lead": lead}


@router.patch("/{lead_id}/assign")
def assign_lead(
    lead_id: int,
    payload: LeadAssignRequest,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_admin),
) -> dict:
    lead = LeadService(db).assign_staff(lead_id=lead_id, staff_id=payload.staff_id, actor=current_user)
    return {"lead": lead}


@router.delete("/{lead_id}")
def delete_lead(
    lead_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_admin),
) -> dict:
    return LeadService(db).delete_lead(lead_id=lead_id, actor=current_user)
