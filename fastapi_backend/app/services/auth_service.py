from __future__ import annotations

from datetime import datetime, timedelta, timezone
from uuid import uuid4

from sqlalchemy.orm import Session

from app.core.config import get_settings
from app.core.exceptions import AppException
from app.core.security import create_access_token, hash_password, verify_password
from app.models.user import User, UserRole, UserSession
from app.repositories.user_repository import UserRepository

settings = get_settings()


class AuthService:
    def __init__(self, db: Session) -> None:
        self.db = db
        self.user_repository = UserRepository(db)

    def seed_default_users(self) -> None:
        seeded = False

        admin = self.user_repository.get_by_email(settings.admin_email)
        if not admin:
            self.user_repository.create(
                User(
                    full_name=settings.admin_full_name,
                    email=settings.admin_email,
                    password_hash=hash_password(settings.admin_password),
                    role=UserRole.ADMIN,
                    is_active=True,
                )
            )
            seeded = True

        staff = self.user_repository.get_by_email(settings.staff_email)
        if not staff:
            self.user_repository.create(
                User(
                    full_name=settings.staff_full_name,
                    email=settings.staff_email,
                    password_hash=hash_password(settings.staff_password),
                    role=UserRole.STAFF,
                    is_active=True,
                )
            )
            seeded = True

        if seeded:
            self.db.commit()

    def authenticate(self, *, email: str, password: str, ip_address: str | None = None, user_agent: str | None = None) -> dict:
        user = self.user_repository.get_by_email(email)
        if not user or not verify_password(password, user.password_hash):
            raise AppException(message="Invalid email or password", status_code=401, code="invalid_credentials")

        if not user.is_active:
            raise AppException(message="User is inactive", status_code=403, code="inactive_user")

        expires_delta = timedelta(minutes=settings.jwt_access_token_expire_minutes)
        expires_at = datetime.now(timezone.utc) + expires_delta
        jti = str(uuid4())
        token = create_access_token(subject=str(user.id), role=user.role.value, jti=jti, expires_delta=expires_delta)

        user.last_login_at = datetime.now(timezone.utc)

        self.user_repository.create_session(
            UserSession(
                user_id=user.id,
                token_jti=jti,
                expires_at=expires_at,
                ip_address=ip_address,
                user_agent=user_agent,
            )
        )

        self.db.commit()
        self.db.refresh(user)

        return {
            "access_token": token,
            "token_type": "bearer",
            "expires_in": int(expires_delta.total_seconds()),
            "user": {
                "id": user.id,
                "full_name": user.full_name,
                "email": user.email,
                "role": user.role.value,
                "is_active": user.is_active,
            },
        }

    def logout(self, *, token_jti: str) -> None:
        self.user_repository.revoke_session(token_jti)
        self.db.commit()
