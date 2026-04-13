from __future__ import annotations

from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from app.api.deps import get_current_staff_or_admin
from app.core.database import get_db
from app.models.user import User
from app.schemas.lead import LeadStatusUpdateRequest
from app.services.lead_service import LeadService

router = APIRouter(prefix="/api/staff", tags=["staff"])


@router.get("/leads")
def staff_leads(
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_staff_or_admin),
    limit: int = Query(default=200, ge=1, le=500),
    offset: int = Query(default=0, ge=0),
) -> dict:
    if current_user.role.value == "admin":
        items = LeadService(db).list_leads(current_user=current_user, limit=limit, offset=offset)
    else:
        items = LeadService(db).list_staff_leads(staff_user=current_user, limit=limit, offset=offset)
    return {"items": items, "total": len(items)}


@router.patch("/leads/{lead_id}/status")
def staff_update_lead_status(
    lead_id: int,
    payload: LeadStatusUpdateRequest,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_staff_or_admin),
) -> dict:
    lead = LeadService(db).update_status(lead_id=lead_id, status_value=payload.status, actor=current_user)
    return {"lead": lead}
