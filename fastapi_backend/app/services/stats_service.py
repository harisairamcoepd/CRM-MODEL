from __future__ import annotations

from datetime import datetime, timedelta, timezone

from sqlalchemy.orm import Session

from app.models.lead import LeadStatus
from app.repositories.demo_repository import DemoRepository
from app.repositories.lead_repository import LeadRepository


class StatsService:
    def __init__(self, db: Session) -> None:
        self.db = db
        self.leads = LeadRepository(db)
        self.demos = DemoRepository(db)

    def get_stats(self) -> dict:
        now = datetime.now(timezone.utc)
        today_start = datetime(now.year, now.month, now.day, tzinfo=timezone.utc)
        tomorrow_start = today_start + timedelta(days=1)

        month_start = datetime(now.year, now.month, 1, tzinfo=timezone.utc)
        if now.month == 12:
            next_month_start = datetime(now.year + 1, 1, 1, tzinfo=timezone.utc)
        else:
            next_month_start = datetime(now.year, now.month + 1, 1, tzinfo=timezone.utc)

        total_leads = self.leads.count_total()
        today_leads = self.leads.count_today(today_start=today_start, tomorrow_start=tomorrow_start)
        this_month_leads = self.leads.count_this_month(month_start=month_start, next_month_start=next_month_start)

        total_bookings = self.demos.count_total()
        today_bookings = self.demos.count_today(today=now.date())

        conversion_count = self.leads.count_by_status(LeadStatus.CONVERTED)

        conversion_rate = (conversion_count / total_leads * 100) if total_leads else 0.0

        current_week_start = today_start - timedelta(days=7)
        previous_week_start = current_week_start - timedelta(days=7)

        current_week_leads = self.leads.count_by_period(start=current_week_start, end=today_start)
        previous_week_leads = self.leads.count_by_period(start=previous_week_start, end=current_week_start)

        if previous_week_leads == 0:
            weekly_growth_percentage = 100.0 if current_week_leads > 0 else 0.0
        else:
            weekly_growth_percentage = ((current_week_leads - previous_week_leads) / previous_week_leads) * 100

        return {
            "totalLeads": total_leads,
            "todayLeads": today_leads,
            "totalBookings": total_bookings,
            "todayBookings": today_bookings,
            "conversionCount": conversion_count,
            "conversionRate": round(conversion_rate, 2),
            "thisMonthLeads": this_month_leads,
            "weeklyGrowthPercentage": round(weekly_growth_percentage, 2),
            "sourceBreakdown": self.leads.source_breakdown(),
            "domainBreakdown": self.leads.domain_breakdown(limit=12),
        }

    def get_growth(self, days: int = 14) -> list[dict[str, int | str]]:
        days = max(1, min(days, 90))
        return self.leads.growth_points(days=days)

    def get_funnel_trend(self, days: int = 14) -> dict:
        points = self.get_growth(days=days)
        trend = [
            {
                "date": point["date"],
                "awareness": 0,
                "interest": 0,
                "desire": 0,
                "action": point["count"],
            }
            for point in points
        ]
        return {
            "days": days,
            "trend": trend,
        }
