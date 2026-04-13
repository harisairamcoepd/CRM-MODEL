from __future__ import annotations

from datetime import date

from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from app.api.deps import get_optional_current_user
from app.core.database import get_db
from app.models.user import User
from app.schemas.demo import DemoBookingCreateRequest
from app.services.demo_service import DemoService

router = APIRouter(tags=["demo"])


@router.post("/api/demo-booking")
def create_demo_booking(
    payload: DemoBookingCreateRequest,
    db: Session = Depends(get_db),
    current_user: User | None = Depends(get_optional_current_user),
) -> dict:
    return DemoService(db).create_booking(
        lead_id=payload.lead_id,
        date_value=payload.date,
        time_slot=payload.time_slot,
        scheduled_by=current_user,
    )


@router.post("/api/demo")
def create_demo_booking_legacy(
    payload: DemoBookingCreateRequest,
    db: Session = Depends(get_db),
    current_user: User | None = Depends(get_optional_current_user),
) -> dict:
    return DemoService(db).create_booking(
        lead_id=payload.lead_id,
        date_value=payload.date,
        time_slot=payload.time_slot,
        scheduled_by=current_user,
    )


@router.get("/api/demo/availability")
def demo_availability(
    date_value: date = Query(alias="date"),
    time_slot: str = Query(alias="timeSlot"),
    db: Session = Depends(get_db),
) -> dict:
    is_available = DemoService(db).check_availability(date_value=date_value, time_slot=time_slot)
    return {"availability": {"isAvailable": is_available}}
