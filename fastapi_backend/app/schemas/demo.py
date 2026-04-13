from __future__ import annotations

from datetime import date, datetime

from pydantic import Field

from app.schemas.base import ApiModel


class DemoBookingCreateRequest(ApiModel):
    lead_id: int = Field(gt=0)
    date: date
    time_slot: str = Field(min_length=3, max_length=40)


class DemoBookingResponse(ApiModel):
    id: int
    lead_id: int
    date: date
    time_slot: str
    status: str
    confirmation_code: str
    created_at: datetime


class DemoBookingCreateResponse(ApiModel):
    booking: DemoBookingResponse
    confirmation: dict[str, str]


class DemoAvailabilityResponse(ApiModel):
    availability: dict[str, bool]
