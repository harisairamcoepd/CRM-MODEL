from __future__ import annotations

import json

from fastapi import APIRouter
from fastapi.responses import StreamingResponse

from app.services.realtime import event_broker

router = APIRouter(tags=["events"])


@router.get("/api/events/stream")
async def stream_events() -> StreamingResponse:
    async def event_generator():
        async for event in event_broker.subscribe():
            payload = json.dumps(event, default=str)
            yield f"data: {payload}\n\n"

    return StreamingResponse(
        event_generator(),
        media_type="text/event-stream",
        headers={
            "Cache-Control": "no-cache",
            "Connection": "keep-alive",
            "X-Accel-Buffering": "no",
        },
    )
