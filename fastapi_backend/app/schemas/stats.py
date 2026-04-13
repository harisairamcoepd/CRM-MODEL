from __future__ import annotations

from app.schemas.base import ApiModel


class StatsResponse(ApiModel):
    total_leads: int
    today_leads: int
    total_bookings: int
    today_bookings: int
    conversion_count: int
    conversion_rate: float
    this_month_leads: int
    weekly_growth_percentage: float
    source_breakdown: dict[str, int]
    domain_breakdown: dict[str, int]


class GrowthPoint(ApiModel):
    date: str
    label: str
    count: int


class TrendStagePoint(ApiModel):
    date: str
    awareness: int
    interest: int
    desire: int
    action: int
