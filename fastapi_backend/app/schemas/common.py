from __future__ import annotations

from typing import Any

from app.schemas.base import ApiModel


class MessageResponse(ApiModel):
    message: str


class ErrorResponse(ApiModel):
    code: str
    message: str
    details: Any | None = None
