(() => {
    const apiBase = document.body?.dataset?.apiBase || (window.location.protocol === "file:" ? "http://localhost:8000" : "");
    const apiUrl = (path) => `${apiBase}${path}`;

    const endpoints = {
        stats: "/api/stats",
        growth: [
            "/api/stats/growth?days=14",
            "/api/admin/lead-growth?days=14",
            "/api/funnel/analytics?days=14"
        ],
        stream: "/api/events/stream"
    };

    const refs = {
        totalLeads: document.getElementById("kpi-total-leads"),
        todayLeads: document.getElementById("kpi-today-leads"),
        demoBookings: document.getElementById("kpi-demo-bookings"),
        conversionRate: document.getElementById("kpi-conversion-rate"),
        lastUpdated: document.getElementById("last-updated"),
        error: document.getElementById("dashboard-error"),
        liveIndicator: document.getElementById("live-indicator")
    };

    const authTokenKey = "coepd-auth-token";

    let leadGrowthChart;
    let sourceBreakdownChart;
    let refreshTimer;
    let liveEventSource;

    const formatNumber = (value) => Number(value || 0).toLocaleString();

    const getAuthHeaders = () => {
        const token = window.localStorage.getItem(authTokenKey);
        if (!token) {
            return {};
        }

        return { Authorization: `Bearer ${token}` };
    };

    const setError = (message) => {
        if (!refs.error) return;
        if (!message) {
            refs.error.hidden = true;
            refs.error.textContent = "";
            return;
        }

        refs.error.hidden = false;
        refs.error.textContent = message;
    };

    const setLiveState = (isHealthy) => {
        if (!refs.liveIndicator) return;
        refs.liveIndicator.textContent = isHealthy ? "Live" : "Retrying";
        refs.liveIndicator.style.background = isHealthy
            ? "linear-gradient(135deg, #1877f2, #0eb7a7)"
            : "linear-gradient(135deg, #c97b1f, #cc5f3f)";
    };

    const requestJson = async (url) => {
        const response = await fetch(apiUrl(url), {
            method: "GET",
            headers: {
                Accept: "application/json",
                ...getAuthHeaders()
            },
            credentials: apiBase ? "omit" : "same-origin"
        });

        if (!response.ok) {
            throw new Error(`Request failed: ${response.status}`);
        }

        return response.json();
    };

    const fetchStats = async () => requestJson(endpoints.stats);

    const normalizeGrowthRows = (data) => {
        if (Array.isArray(data)) {
            return data.map((item) => ({
                label: item.label || item.date || "",
                date: item.date || "",
                count: Number(item.count || item.value || 0)
            }));
        }

        if (data && Array.isArray(data.trend)) {
            return data.trend.map((point) => ({
                label: point.date || "",
                date: point.date || "",
                count:
                    Number(point.count || 0) +
                    Number(point.action || 0) +
                    Number(point.desire || 0) +
                    Number(point.interest || 0) +
                    Number(point.awareness || 0)
            }));
        }

        return [];
    };

    const fetchGrowth = async () => {
        for (const endpoint of endpoints.growth) {
            try {
                const data = await requestJson(endpoint);
                const normalized = normalizeGrowthRows(data);
                if (normalized.length > 0) {
                    return normalized;
                }
            } catch {
                continue;
            }
        }

        return [];
    };

    const ensureGrowthData = (rows) => {
        if (rows.length > 0) {
            return rows;
        }

        const fallback = [];
        for (let i = 6; i >= 0; i -= 1) {
            const d = new Date();
            d.setDate(d.getDate() - i);
            fallback.push({
                label: d.toLocaleDateString(undefined, { month: "short", day: "numeric" }),
                count: 0
            });
        }
        return fallback;
    };

    const updateCards = (stats) => {
        const totalLeads = Number(stats.totalLeads || 0);
        const todayLeads = Number(stats.todayLeads || 0);
        const demoBookings = Number(stats.totalBookings || 0);
        const conversionCount = Number(stats.conversionCount || 0);
        const conversionRate = totalLeads === 0 ? Number(stats.conversionRate || 0) : (conversionCount / totalLeads) * 100;

        refs.totalLeads.textContent = formatNumber(totalLeads);
        refs.todayLeads.textContent = formatNumber(todayLeads);
        refs.demoBookings.textContent = formatNumber(demoBookings);
        refs.conversionRate.textContent = `${conversionRate.toFixed(1)}%`;
    };

    const renderLeadGrowthChart = (growthData) => {
        const canvas = document.getElementById("lead-growth-chart");
        if (!canvas) return;

        const labels = growthData.map((point) => point.label);
        const values = growthData.map((point) => point.count);

        if (leadGrowthChart) {
            leadGrowthChart.data.labels = labels;
            leadGrowthChart.data.datasets[0].data = values;
            leadGrowthChart.update();
            return;
        }

        leadGrowthChart = new Chart(canvas, {
            type: "line",
            data: {
                labels,
                datasets: [{
                    label: "Leads",
                    data: values,
                    borderColor: "#1877f2",
                    backgroundColor: "rgba(24, 119, 242, 0.14)",
                    borderWidth: 2.5,
                    fill: true,
                    tension: 0.35,
                    pointRadius: 3,
                    pointHoverRadius: 5,
                    pointBackgroundColor: "#1877f2"
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: "index", intersect: false },
                plugins: { legend: { display: false } },
                scales: {
                    x: {
                        grid: { color: "rgba(16, 37, 59, 0.06)" },
                        ticks: { color: "#5d7287" }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: "rgba(16, 37, 59, 0.06)" },
                        ticks: { color: "#5d7287", precision: 0 }
                    }
                }
            }
        });
    };

    const renderSourceChart = (sourceBreakdown, domainBreakdown) => {
        const canvas = document.getElementById("source-breakdown-chart");
        if (!canvas) return;

        const input = Object.keys(sourceBreakdown || {}).length > 0
            ? sourceBreakdown
            : (domainBreakdown || {});

        const entries = Object.entries(input);
        const labels = entries.length > 0 ? entries.map(([k]) => k) : ["No Source Data"];
        const values = entries.length > 0 ? entries.map(([, v]) => Number(v || 0)) : [1];
        const colors = ["#1877f2", "#0eb7a7", "#6d5ce8", "#f2994a", "#26a69a", "#536dfe"];

        if (sourceBreakdownChart) {
            sourceBreakdownChart.data.labels = labels;
            sourceBreakdownChart.data.datasets[0].data = values;
            sourceBreakdownChart.update();
            return;
        }

        sourceBreakdownChart = new Chart(canvas, {
            type: "doughnut",
            data: {
                labels,
                datasets: [{
                    data: values,
                    backgroundColor: colors,
                    borderColor: "#ffffff",
                    borderWidth: 2,
                    hoverOffset: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "62%",
                plugins: {
                    legend: {
                        position: "bottom",
                        labels: {
                            usePointStyle: true,
                            boxWidth: 8,
                            color: "#5d7287",
                            font: { size: 12, family: "Inter" }
                        }
                    }
                }
            }
        });
    };

    const updateLastUpdated = () => {
        if (!refs.lastUpdated) return;
        const now = new Date();
        refs.lastUpdated.textContent = `Updated: ${now.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" })}`;
    };

    const refreshDashboard = async () => {
        try {
            const [stats, growthRaw] = await Promise.all([fetchStats(), fetchGrowth()]);
            const growth = ensureGrowthData(growthRaw);

            updateCards(stats);
            renderLeadGrowthChart(growth);
            renderSourceChart(stats.sourceBreakdown || {}, stats.domainBreakdown || {});
            updateLastUpdated();
            setError("");
            setLiveState(true);
        } catch {
            setLiveState(false);
            setError("Unable to refresh dashboard data. Retrying automatically...");
        }
    };

    const startRealtimeUpdates = () => {
        window.clearInterval(refreshTimer);
        refreshTimer = window.setInterval(() => {
            if (document.hidden) return;
            refreshDashboard();
        }, 20000);
    };

    const stopEventStream = () => {
        if (liveEventSource) {
            liveEventSource.close();
            liveEventSource = null;
        }
    };

    const connectEventStream = () => {
        stopEventStream();

        if (!("EventSource" in window)) {
            return;
        }

        const token = window.localStorage.getItem(authTokenKey);
        const url = new URL(apiUrl(endpoints.stream), window.location.href);
        if (token) {
            url.searchParams.set("access_token", token);
        }

        liveEventSource = new EventSource(url.toString());

        liveEventSource.onmessage = (event) => {
            let payload = null;
            try {
                payload = JSON.parse(event.data);
            } catch {
                return;
            }

            if (!payload || payload.event === "heartbeat") {
                return;
            }

            if (["lead_created", "lead_updated", "lead_deleted", "demo_booked", "lead_assigned"].includes(payload.event)) {
                refreshDashboard();
            }
        };

        liveEventSource.onerror = () => {
            stopEventStream();
            window.setTimeout(connectEventStream, 5000);
        };
    };

    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) {
            refreshDashboard();
        }
    });

    window.addEventListener("beforeunload", stopEventStream);

    refreshDashboard();
    startRealtimeUpdates();
    connectEventStream();
})();
