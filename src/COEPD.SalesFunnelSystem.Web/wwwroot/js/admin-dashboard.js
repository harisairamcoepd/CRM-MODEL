(function () {
    const growthCanvas = document.getElementById("leadGrowthChart");
    const sourceCanvas = document.getElementById("sourceChart");
    if (!growthCanvas || !sourceCanvas || typeof Chart === "undefined") {
        return;
    }

    const refs = {
        totalLeads: document.querySelector('[data-stat="totalLeads"]'),
        todayLeads: document.querySelector('[data-stat="todayLeads"]'),
        conversionRate: document.querySelector('[data-stat="conversionRate"]'),
        demoBookedLeads: document.querySelector('[data-stat="demoBookedLeads"]'),
        thisMonthLeads: document.querySelector('[data-stat="thisMonthLeads"]'),
        totalBookings: document.querySelector('[data-stat="totalBookings"]'),
        weeklyGrowth: document.querySelector('[data-stat="weeklyGrowth"]'),
        activeSources: document.querySelector('[data-stat="activeSources"]'),
        refreshStatus: document.getElementById("dashboard-refresh-status")
    };

    const requestJson = async (url) => {
        const response = await fetch(url, {
            method: "GET",
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        });

        const body = await response.json().catch(() => null);
        if (!response.ok) {
            throw new Error(body?.title || body?.message || "Request failed.");
        }

        return body;
    };

    const setRefreshState = (healthy, message) => {
        if (!refs.refreshStatus) {
            return;
        }

        refs.refreshStatus.innerHTML = healthy
            ? `<i class="ph ph-broadcast"></i> ${message}`
            : `<i class="ph ph-warning-circle"></i> ${message}`;
    };

    const formatNumber = (value) => Number(value || 0).toLocaleString();

    const upsertChart = (canvas, config) => {
        const existing = Chart.getChart(canvas);
        if (existing) {
            existing.destroy();
        }

        return new Chart(canvas, config);
    };

    const renderCharts = (growth, sourceBreakdown) => {
        upsertChart(growthCanvas, {
            type: "line",
            data: {
                labels: growth.map((point) => point.label),
                datasets: [{
                    label: "Leads",
                    data: growth.map((point) => point.count),
                    borderColor: "#2563eb",
                    backgroundColor: "rgba(37, 99, 235, 0.12)",
                    fill: true,
                    tension: 0.35,
                    borderWidth: 3,
                    pointRadius: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } }
            }
        });

        upsertChart(sourceCanvas, {
            type: "doughnut",
            data: {
                labels: Object.keys(sourceBreakdown),
                datasets: [{
                    data: Object.values(sourceBreakdown),
                    backgroundColor: ["#2563eb", "#14b8a6", "#4f46e5", "#0f172a", "#7c3aed", "#06b6d4"],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "70%"
            }
        });
    };

    const updateCards = (stats) => {
        const totalLeads = Number(stats.totalLeads || 0);
        const todayLeads = Number(stats.todayLeads || 0);
        const totalBookings = Number(stats.totalBookings || 0);
        const conversionCount = Number(stats.conversionCount || 0);
        const conversionRate = totalLeads === 0 ? 0 : ((conversionCount / totalLeads) * 100);

        if (refs.totalLeads) refs.totalLeads.textContent = formatNumber(totalLeads);
        if (refs.todayLeads) refs.todayLeads.textContent = formatNumber(todayLeads);
        if (refs.conversionRate) refs.conversionRate.textContent = `${conversionRate.toFixed(2)}%`;
        if (refs.demoBookedLeads) refs.demoBookedLeads.textContent = formatNumber(totalBookings);
        if (refs.thisMonthLeads) refs.thisMonthLeads.textContent = formatNumber(stats.thisMonthLeads || 0);
        if (refs.totalBookings) refs.totalBookings.textContent = formatNumber(totalBookings);
        if (refs.weeklyGrowth) refs.weeklyGrowth.innerHTML = `<i class="ph ph-trend-up"></i> ${Math.abs(Number(stats.weeklyGrowthPercentage || 0)).toFixed(1)}% vs last week`;
        if (refs.activeSources) refs.activeSources.textContent = Object.keys(stats.sourceBreakdown || {}).length;
    };

    const refreshDashboard = async () => {
        try {
            const [stats, growth] = await Promise.all([
                requestJson("/api/stats"),
                requestJson("/api/admin/lead-growth")
            ]);

            updateCards(stats);
            renderCharts(Array.isArray(growth) ? growth : [], stats.sourceBreakdown || {});
            setRefreshState(true, `Live sync updated at ${new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" })}`);
        } catch (error) {
            setRefreshState(false, "Live sync retrying");
        }
    };

    refreshDashboard();
    window.setInterval(() => {
        if (!document.hidden) {
            refreshDashboard();
        }
    }, 20000);

    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) {
            refreshDashboard();
        }
    });
})();
