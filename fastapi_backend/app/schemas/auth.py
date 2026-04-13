from __future__ import annotations

from pydantic import Field

from app.schemas.base import ApiModel


class LoginRequest(ApiModel):
    email: str
    password: str = Field(min_length=8, max_length=128)


class UserResponse(ApiModel):
    id: int
    full_name: str
    email: str
    role: str
    is_active: bool


class TokenResponse(ApiModel):
    access_token: str
    token_type: str = "bearer"
    expires_in: int
    user: UserResponse


class LogoutResponse(ApiModel):
    message: str
