from __future__ import annotations

import asyncio
from collections.abc import AsyncGenerator
from typing import Any


class EventBroker:
    def __init__(self) -> None:
        self._subscribers: set[asyncio.Queue[dict[str, Any]]] = set()

    def publish(self, event: dict[str, Any]) -> None:
        dead_subscribers: list[asyncio.Queue[dict[str, Any]]] = []
        for queue in list(self._subscribers):
            try:
                queue.put_nowait(event)
            except asyncio.QueueFull:
                try:
                    queue.get_nowait()
                    queue.put_nowait(event)
                except Exception:
                    dead_subscribers.append(queue)
            except Exception:
                dead_subscribers.append(queue)

        for subscriber in dead_subscribers:
            self._subscribers.discard(subscriber)

    async def subscribe(self) -> AsyncGenerator[dict[str, Any], None]:
        queue: asyncio.Queue[dict[str, Any]] = asyncio.Queue(maxsize=200)
        self._subscribers.add(queue)
        try:
            while True:
                try:
                    event = await asyncio.wait_for(queue.get(), timeout=20.0)
                except TimeoutError:
                    yield {"event": "heartbeat"}
                    continue
                yield event
        finally:
            self._subscribers.discard(queue)


event_broker = EventBroker()
