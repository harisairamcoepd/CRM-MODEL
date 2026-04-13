from __future__ import annotations

from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from app.api.deps import get_current_admin
from app.core.database import get_db
from app.models.user import User
from app.services.lead_service import LeadService
from app.services.stats_service import StatsService

router = APIRouter(prefix="/api/admin", tags=["admin"])


@router.get("/leads")
def admin_leads(
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_admin),
    limit: int = Query(default=200, ge=1, le=500),
    offset: int = Query(default=0, ge=0),
) -> dict:
    items = LeadService(db).list_leads(current_user=current_user, limit=limit, offset=offset)
    return {"items": items, "total": len(items)}


@router.get("/leads/today")
def admin_today_leads(
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_admin),
    limit: int = Query(default=200, ge=1, le=500),
    offset: int = Query(default=0, ge=0),
) -> dict:
    items = LeadService(db).list_today_leads(limit=limit, offset=offset)
    return {"items": items, "total": len(items)}


@router.get("/lead-stats")
def admin_lead_stats(db: Session = Depends(get_db), current_user: User = Depends(get_current_admin)) -> dict:
    return StatsService(db).get_stats()


@router.delete("/lead/{lead_id}")
def admin_delete_lead(
    lead_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_admin),
) -> dict:
    return LeadService(db).delete_lead(lead_id=lead_id, actor=current_user)
