from __future__ import annotations

from fastapi import APIRouter, Depends, Request
from sqlalchemy.orm import Session

from app.api.deps import get_current_user, oauth2_scheme
from app.core.database import get_db
from app.core.security import decode_access_token
from app.models.user import User
from app.schemas.auth import LoginRequest, TokenResponse
from app.services.auth_service import AuthService

router = APIRouter(prefix="/api/auth", tags=["auth"])


@router.post("/login", response_model=TokenResponse)
def login(payload: LoginRequest, request: Request, db: Session = Depends(get_db)) -> dict:
    service = AuthService(db)
    ip_address = request.client.host if request.client else None
    user_agent = request.headers.get("user-agent")
    return service.authenticate(email=payload.email, password=payload.password, ip_address=ip_address, user_agent=user_agent)


@router.get("/me")
def me(current_user: User = Depends(get_current_user)) -> dict:
    return {
        "id": current_user.id,
        "fullName": current_user.full_name,
        "email": current_user.email,
        "role": current_user.role.value,
        "isActive": current_user.is_active,
    }


@router.post("/logout")
def logout(token: str | None = Depends(oauth2_scheme), db: Session = Depends(get_db), current_user: User = Depends(get_current_user)) -> dict:
    if not token:
        return {"message": "Logged out"}

    payload = decode_access_token(token)
    token_jti = payload.get("jti")
    if token_jti:
        AuthService(db).logout(token_jti=str(token_jti))

    return {"message": "Logged out"}
