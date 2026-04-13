from __future__ import annotations

from datetime import datetime, timezone

from sqlalchemy import and_, func, select
from sqlalchemy.orm import Session

from app.models.lead import Lead
from app.models.user import User, UserRole, UserSession


class UserRepository:
    def __init__(self, db: Session) -> None:
        self.db = db

    def get_by_id(self, user_id: int) -> User | None:
        statement = select(User).where(User.id == user_id)
        return self.db.execute(statement).scalar_one_or_none()

    def get_by_email(self, email: str) -> User | None:
        statement = select(User).where(User.email == email)
        return self.db.execute(statement).scalar_one_or_none()

    def create(self, user: User) -> User:
        self.db.add(user)
        return user

    def list_staff(self) -> list[User]:
        statement = select(User).where(and_(User.role == UserRole.STAFF, User.is_active.is_(True))).order_by(User.id.asc())
        return list(self.db.execute(statement).scalars().all())

    def get_least_loaded_staff(self) -> User | None:
        statement = (
            select(User, func.count(Lead.id).label("lead_count"))
            .outerjoin(Lead, Lead.assigned_staff_id == User.id)
            .where(and_(User.role == UserRole.STAFF, User.is_active.is_(True)))
            .group_by(User.id)
            .order_by(func.count(Lead.id).asc(), User.id.asc())
        )
        row = self.db.execute(statement).first()
        if not row:
            return None
        return row[0]

    def create_session(self, session: UserSession) -> UserSession:
        self.db.add(session)
        return session

    def get_session_by_jti(self, token_jti: str) -> UserSession | None:
        statement = select(UserSession).where(UserSession.token_jti == token_jti)
        return self.db.execute(statement).scalar_one_or_none()

    def revoke_session(self, token_jti: str) -> None:
        session = self.get_session_by_jti(token_jti)
        if session and session.revoked_at is None:
            session.revoked_at = datetime.now(timezone.utc)
