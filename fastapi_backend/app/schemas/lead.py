from __future__ import annotations

from datetime import datetime

from pydantic import EmailStr, Field, field_validator

from app.schemas.base import ApiModel


class LeadCreateRequest(ApiModel):
    name: str = Field(min_length=2, max_length=120)
    phone: str = Field(min_length=8, max_length=20)
    email: EmailStr
    location: str = Field(min_length=2, max_length=120)
    domain: str = Field(min_length=2, max_length=120)
    source: str | None = Field(default=None, max_length=50)
    status: str | None = Field(default=None, max_length=40)
    notes: str | None = Field(default=None, max_length=1500)

    @field_validator("phone")
    @classmethod
    def normalize_phone(cls, value: str) -> str:
        return value.strip()


class LeadResponse(ApiModel):
    id: int
    name: str
    phone: str
    email: EmailStr
    location: str
    domain: str
    source: str
    status: str
    assigned_staff_id: int | None = None
    created_at: datetime
    updated_at: datetime


class LeadListResponse(ApiModel):
    items: list[LeadResponse]
    total: int


class LeadCreateResponse(ApiModel):
    lead: LeadResponse


class LeadStatusUpdateRequest(ApiModel):
    status: str = Field(min_length=2, max_length=40)


class LeadAssignRequest(ApiModel):
    staff_id: int = Field(gt=0)


class LeadDeleteResponse(ApiModel):
    deleted: bool
    id: int
