from __future__ import annotations

from sqlalchemy import and_, select
from sqlalchemy.orm import Session

from app.models.chatbot import ChatMessage, ChatSession


class ChatbotService:
    def __init__(self, db: Session) -> None:
        self.db = db

    def get_or_create_session(self, *, session_key: str, lead_id: int | None = None) -> ChatSession:
        statement = select(ChatSession).where(ChatSession.session_key == session_key)
        session = self.db.execute(statement).scalar_one_or_none()
        if session:
            if lead_id and not session.lead_id:
                session.lead_id = lead_id
                self.db.commit()
                self.db.refresh(session)
            return session

        session = ChatSession(session_key=session_key, lead_id=lead_id)
        self.db.add(session)
        self.db.commit()
        self.db.refresh(session)
        return session

    def append_message(self, *, session_key: str, role: str, content: str, lead_id: int | None = None) -> ChatMessage:
        session = self.get_or_create_session(session_key=session_key, lead_id=lead_id)
        message = ChatMessage(chat_session_id=session.id, role=role, content=content)
        self.db.add(message)
        self.db.commit()
        self.db.refresh(message)
        return message
