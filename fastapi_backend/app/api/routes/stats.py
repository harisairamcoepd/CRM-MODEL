from __future__ import annotations

from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from app.api.deps import get_current_admin, get_current_staff_or_admin
from app.core.database import get_db
from app.models.user import User
from app.services.stats_service import StatsService

router = APIRouter(tags=["stats"])


@router.get("/api/stats")
def stats(db: Session = Depends(get_db)) -> dict:
    return StatsService(db).get_stats()


@router.get("/api/stats/growth")
def stats_growth(days: int = Query(default=14, ge=1, le=90), db: Session = Depends(get_db)) -> list[dict]:
    return StatsService(db).get_growth(days=days)


@router.get("/api/admin/lead-growth")
def admin_lead_growth(
    days: int = Query(default=14, ge=1, le=90),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_staff_or_admin),
) -> list[dict]:
    return StatsService(db).get_growth(days=days)


@router.get("/api/funnel/analytics")
def funnel_analytics(days: int = Query(default=14, ge=1, le=90), db: Session = Depends(get_db)) -> dict:
    return StatsService(db).get_funnel_trend(days=days)


@router.get("/api/admin/stats")
def admin_stats(db: Session = Depends(get_db), current_user: User = Depends(get_current_admin)) -> dict:
    return StatsService(db).get_stats()
