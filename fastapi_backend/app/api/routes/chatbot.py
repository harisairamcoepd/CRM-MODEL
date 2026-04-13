from __future__ import annotations

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from app.core.database import get_db
from app.schemas.chatbot import ChatMessageRequest, ChatSessionStartRequest
from app.services.chatbot_service import ChatbotService

router = APIRouter(prefix="/api/chatbot", tags=["chatbot"])


@router.post("/sessions")
def start_session(payload: ChatSessionStartRequest, db: Session = Depends(get_db)) -> dict:
    session = ChatbotService(db).get_or_create_session(session_key=payload.session_key, lead_id=payload.lead_id)
    return {
        "session": {
            "id": session.id,
            "sessionKey": session.session_key,
            "leadId": session.lead_id,
            "isActive": session.is_active,
            "createdAt": session.created_at,
        }
    }


@router.post("/messages")
def append_message(payload: ChatMessageRequest, db: Session = Depends(get_db)) -> dict:
    message = ChatbotService(db).append_message(
        session_key=payload.session_key,
        role=payload.role,
        content=payload.content,
        lead_id=payload.lead_id,
    )
    return {
        "message": {
            "id": message.id,
            "sessionKey": payload.session_key,
            "role": message.role,
            "content": message.content,
            "createdAt": message.created_at,
        }
    }
