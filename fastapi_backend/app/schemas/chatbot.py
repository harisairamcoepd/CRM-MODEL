from __future__ import annotations

from datetime import datetime

from pydantic import Field

from app.schemas.base import ApiModel


class ChatSessionStartRequest(ApiModel):
    session_key: str = Field(min_length=4, max_length=64)
    lead_id: int | None = None


class ChatMessageRequest(ApiModel):
    session_key: str = Field(min_length=4, max_length=64)
    role: str = Field(min_length=2, max_length=20)
    content: str = Field(min_length=1, max_length=2000)
    lead_id: int | None = None


class ChatMessageResponse(ApiModel):
    id: int
    session_key: str
    role: str
    content: str
    created_at: datetime
