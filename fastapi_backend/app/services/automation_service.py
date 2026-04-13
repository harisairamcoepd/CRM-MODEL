from __future__ import annotations

import json
import logging
from datetime import date
from urllib import error, request

from app.core.config import get_settings
from app.models.demo import DemoBooking
from app.models.lead import Lead

settings = get_settings()
logger = logging.getLogger(__name__)


class AutomationService:
    def __init__(self) -> None:
        self.whatsapp_api_url = settings.whatsapp_api_url.strip()
        self.whatsapp_access_token = settings.whatsapp_access_token.strip()
        self.whatsapp_sender_id = settings.whatsapp_sender_id.strip()

    def _post_json(self, url: str, payload: dict, headers: dict[str, str] | None = None) -> None:
        body = json.dumps(payload).encode("utf-8")
        req_headers = {"Content-Type": "application/json", **(headers or {})}
        req = request.Request(url=url, data=body, headers=req_headers, method="POST")

        with request.urlopen(req, timeout=10) as response:
            if response.status >= 300:
                raise RuntimeError(f"Automation call failed with status {response.status}")

    def send_whatsapp_after_lead_capture(self, lead: Lead) -> None:
        if not self.whatsapp_api_url or not self.whatsapp_access_token or not self.whatsapp_sender_id:
            logger.info("WhatsApp integration not configured. Skipping lead message for lead_id=%s", lead.id)
            return

        payload = {
            "to": lead.phone,
            "from": self.whatsapp_sender_id,
            "type": "template",
            "template": {
                "name": "lead_capture_acknowledgement",
                "language": {"code": "en"},
                "components": [
                    {
                        "type": "body",
                        "parameters": [
                            {"type": "text", "text": lead.name},
                            {"type": "text", "text": lead.domain},
                        ],
                    }
                ],
            },
        }

        try:
            self._post_json(
                self.whatsapp_api_url,
                payload,
                headers={"Authorization": f"Bearer {self.whatsapp_access_token}"},
            )
            logger.info("WhatsApp lead acknowledgement sent for lead_id=%s", lead.id)
        except (error.URLError, RuntimeError, TimeoutError) as exc:
            logger.warning("WhatsApp send failed for lead_id=%s: %s", lead.id, exc)

    def send_demo_confirmation_email(self, lead: Lead, demo: DemoBooking) -> None:
        # Provider-specific implementation can be injected later. Kept as a safe no-op logger.
        logger.info(
            "Email automation placeholder: demo confirmation for lead_id=%s demo_id=%s date=%s time_slot=%s",
            lead.id,
            demo.id,
            demo.demo_date,
            demo.time_slot,
        )

    def send_follow_up_email(self, lead: Lead, follow_up_date: date) -> None:
        logger.info(
            "Email automation placeholder: follow-up scheduled for lead_id=%s follow_up_date=%s",
            lead.id,
            follow_up_date,
        )


automation_service = AutomationService()
