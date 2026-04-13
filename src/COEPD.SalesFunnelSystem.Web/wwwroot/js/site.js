(function () {
    function requestJson(url, method, payload) {
        const headers = { "Content-Type": "application/json" };

        return fetch(url, {
            method,
            headers,
            body: payload ? JSON.stringify(payload) : undefined,
            credentials: "same-origin"
        }).then(async (response) => {
            const body = await response.json().catch(() => null);
            if (!response.ok) {
                const message = body?.title || body?.message || body?.errors?.join(", ") || "Request failed.";
                throw new Error(message);
            }

            return body;
        });
    }

    function postJson(url, payload) {
        return requestJson(url, "POST", payload);
    }

    function putJson(url, payload) {
        return requestJson(url, "PUT", payload);
    }

    function setupTopbar() {
        const topbar = document.querySelector(".topbar");
        if (!topbar) {
            return;
        }

        const updateTopbar = () => {
            topbar.classList.toggle("topbar-scrolled", window.scrollY > 8);
        };

        updateTopbar();
        window.addEventListener("scroll", updateTopbar, { passive: true });
    }

    function setupRevealAnimations() {
        const revealItems = document.querySelectorAll(".funnel-section, .trust-card, .issue-card, .domain-browser-card, .testimonial-card, .demo-benefits-card, .demo-booking-card");
        if (!revealItems.length || !("IntersectionObserver" in window)) {
            return;
        }

        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    entry.target.classList.add("is-visible");
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12 });

        revealItems.forEach((item) => {
            item.classList.add("reveal");
            observer.observe(item);
        });
    }

    function setStatus(element, message, kind) {
        if (!element) {
            return;
        }

        element.textContent = message || "";
        element.className = "status-text";
        if (kind) {
            element.classList.add(kind);
        }
    }

    function updateDemoState(leadId) {
        const unlockCard = document.getElementById("demo-unlock-card");
        const title = document.getElementById("demo-state-title");
        const copy = document.getElementById("demo-state-copy");
        const demoLeadInput = document.getElementById("demo-leadId");
        const demoSubmit = document.getElementById("demo-submit");

        if (demoLeadInput) {
            demoLeadInput.value = leadId || "";
        }

        if (!leadId) {
            unlockCard?.setAttribute("data-locked", "true");
            if (title) {
                title.textContent = "Lead capture required";
            }
            if (copy) {
                copy.textContent = "Submit the primary form above to unlock demo scheduling with your new Lead ID prefilled automatically.";
            }
            if (demoSubmit) {
                demoSubmit.disabled = true;
            }
            return;
        }

        unlockCard?.setAttribute("data-locked", "false");
        if (title) {
            title.textContent = `Lead created: #${leadId}`;
        }
        if (copy) {
            copy.textContent = "Your demo flow is unlocked. Choose a day and time slot below to continue the same admissions journey.";
        }
        if (demoSubmit) {
            demoSubmit.disabled = false;
        }
    }

    function setupLeadCapture() {
        const leadForm = document.getElementById("lead-form");
        if (!leadForm) {
            return;
        }

        const leadSubmit = document.getElementById("lead-submit");
        const leadStatus = document.getElementById("lead-form-status");
        const savedLeadId = localStorage.getItem("coepd-lead-id");
        updateDemoState(savedLeadId);

        leadForm.addEventListener("submit", async (event) => {
            event.preventDefault();

            const originalLabel = leadSubmit?.textContent || "Create Lead And Continue";
            const payload = Object.fromEntries(new FormData(leadForm).entries());

            try {
                if (leadSubmit) {
                    leadSubmit.disabled = true;
                    leadSubmit.textContent = "Creating Lead...";
                }
                setStatus(leadStatus, "", null);

                const result = await postJson("/api/leads", payload);
                const lead = result?.lead ?? result;
                const leadId = String(lead.id);

                localStorage.setItem("coepd-lead-id", leadId);
                localStorage.setItem("coepd-chat-lead-id", leadId);
                updateDemoState(leadId);
                leadForm.reset();

                const domainSelect = leadForm.querySelector('select[name="domain"]');
                if (domainSelect) {
                    domainSelect.selectedIndex = 0;
                }

                setStatus(leadStatus, `Lead created successfully. Your Lead ID is ${leadId}. Demo booking is now unlocked below.`, "success");
                document.getElementById("demo-experience")?.scrollIntoView({ behavior: "smooth", block: "start" });
            } catch (error) {
                setStatus(leadStatus, error.message || "Lead capture failed.", "error");
            } finally {
                if (leadSubmit) {
                    leadSubmit.disabled = false;
                    leadSubmit.textContent = originalLabel;
                }
            }
        });
    }

    function setupDemoBooking() {
        const demoForm = document.getElementById("demo-form");
        if (!demoForm) {
            return;
        }

        const demoSubmit = document.getElementById("demo-submit");
        const demoStatus = document.getElementById("demo-form-status");
        const savedLeadId = localStorage.getItem("coepd-lead-id");
        updateDemoState(savedLeadId);

        demoForm.addEventListener("submit", async (event) => {
            event.preventDefault();

            const leadId = document.getElementById("demo-leadId")?.value;
            if (!leadId) {
                setStatus(demoStatus, "Create the lead first, then continue to booking.", "error");
                return;
            }

            const originalLabel = demoSubmit?.textContent || "Confirm Free Demo";

            try {
                if (demoSubmit) {
                    demoSubmit.disabled = true;
                    demoSubmit.textContent = "Booking Demo...";
                }

                setStatus(demoStatus, "", null);

                await fetch("/api/funnel/event", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ leadId: Number(leadId), stage: "Desire" })
                }).catch(() => null);

                const payload = {
                    leadId: Number(leadId),
                    day: document.getElementById("demo-day")?.value,
                    slot: document.getElementById("demo-slot")?.value
                };

                const result = await postJson("/api/demo", payload);
                const confirmation = result?.confirmation;
                setStatus(
                    demoStatus,
                    `Demo booked successfully${confirmation?.confirmationCode ? ` (${confirmation.confirmationCode})` : ""}.`,
                    "success");
            } catch (error) {
                setStatus(demoStatus, error.message || "Demo booking failed.", "error");
            } finally {
                if (demoSubmit) {
                    demoSubmit.disabled = false;
                    demoSubmit.textContent = originalLabel;
                }
            }
        });
    }

    function setupDomainBrowser() {
        const grid = document.getElementById("domain-grid");
        if (!grid) {
            return;
        }

        const searchInput = document.getElementById("domain-search");
        const summary = document.getElementById("domain-results-summary");
        const clearButton = document.getElementById("domain-clear");
        const loadMoreButton = document.getElementById("domain-load-more");
        const filterButtons = Array.from(document.querySelectorAll(".filter-btn"));
        const domains = Array.isArray(window.coepdDomains) ? window.coepdDomains : [];

        const state = {
            search: "",
            filter: "all",
            visibleCount: window.innerWidth < 768 ? 8 : 12
        };

        let searchTimer = null;

        function getBatchSize() {
            return window.innerWidth < 768 ? 8 : 12;
        }

        function getFilteredDomains() {
            return domains.filter((item) => {
                const matchesSearch = !state.search || item.name.toLowerCase().includes(state.search);
                const matchesFilter = state.filter === "all" || item.category === state.filter;
                return matchesSearch && matchesFilter;
            });
        }

        function focusLeadCapture(domainName) {
            const domainSelect = document.querySelector('#lead-form select[name="domain"]');
            if (!domainSelect) {
                return;
            }

            let option = Array.from(domainSelect.options).find((item) => item.value === domainName);
            if (!option) {
                option = document.createElement("option");
                option.value = domainName;
                option.textContent = domainName;
                domainSelect.appendChild(option);
            }

            domainSelect.value = domainName;
            document.getElementById("lead-capture")?.scrollIntoView({ behavior: "smooth", block: "start" });
            domainSelect.focus();
        }

        function renderDomains() {
            const filtered = getFilteredDomains();
            const visible = filtered.slice(0, state.visibleCount);
            const fragment = document.createDocumentFragment();

            grid.innerHTML = "";

            visible.forEach((item) => {
                const card = document.createElement("button");
                card.type = "button";
                card.className = "domain-browser-card";
                card.dataset.domain = item.name;
                card.dataset.category = item.category;
                card.innerHTML = `
                    <span class="domain-browser-card__category">${item.category}</span>
                    <h3>${item.name}</h3>
                    <p>${item.description}</p>
                    <span class="domain-browser-card__action">Use this track</span>
                `;
                fragment.appendChild(card);
            });

            grid.appendChild(fragment);

            if (summary) {
                summary.textContent = `${visible.length} of ${filtered.length} domains shown`;
            }

            if (clearButton) {
                clearButton.hidden = !state.search && state.filter === "all";
            }

            if (loadMoreButton) {
                loadMoreButton.hidden = filtered.length <= state.visibleCount;
            }
        }

        searchInput?.addEventListener("input", (event) => {
            window.clearTimeout(searchTimer);
            searchTimer = window.setTimeout(() => {
                state.search = event.target.value.trim().toLowerCase();
                state.visibleCount = getBatchSize();
                renderDomains();
            }, 120);
        });

        clearButton?.addEventListener("click", () => {
            state.search = "";
            state.filter = "all";
            state.visibleCount = getBatchSize();

            if (searchInput) {
                searchInput.value = "";
            }

            filterButtons.forEach((button) => {
                button.classList.toggle("is-active", button.dataset.filter === "all");
            });

            renderDomains();
        });

        filterButtons.forEach((button) => {
            button.addEventListener("click", () => {
                state.filter = button.dataset.filter || "all";
                state.visibleCount = getBatchSize();
                filterButtons.forEach((item) => item.classList.toggle("is-active", item === button));
                renderDomains();
            });
        });

        loadMoreButton?.addEventListener("click", () => {
            state.visibleCount += getBatchSize();
            renderDomains();
        });

        grid.addEventListener("click", (event) => {
            const card = event.target.closest(".domain-browser-card");
            if (!card) {
                return;
            }

            focusLeadCapture(card.dataset.domain || "");
        });

        window.addEventListener("resize", () => {
            const minimumVisible = getBatchSize();
            if (state.visibleCount < minimumVisible) {
                state.visibleCount = minimumVisible;
            }
        });

        renderDomains();
    }

    function setupPipelineInteractions() {
        const pipelineButtons = document.querySelectorAll(".pipeline-update-btn");
        if (!pipelineButtons.length) {
            return;
        }

        pipelineButtons.forEach((button) => {
            button.addEventListener("click", async (event) => {
                const currentButton = event.currentTarget;
                const card = currentButton.closest(".pipeline-card");
                const select = card?.querySelector(".pipeline-status-select");
                const stage = select?.value;
                if (!stage) {
                    return;
                }

                const original = currentButton.textContent;
                currentButton.disabled = true;
                currentButton.textContent = "Updating...";

                try {
                    const result = await putJson(`/api/pipeline/${currentButton.dataset.id}/move`, { stage });
                    const updated = result?.lead;
                    if (card && updated?.status) {
                        card.dataset.currentStage = updated.status;
                    }
                    currentButton.textContent = "Done";
                } catch (error) {
                    alert(error.message || "Lead update failed.");
                    currentButton.textContent = original || "Move";
                } finally {
                    setTimeout(() => {
                        currentButton.disabled = false;
                        currentButton.textContent = original || "Move";
                    }, 700);
                }
            });
        });
    }

    function setupDashboardRefresh() {
        if (!window.dashboardData || typeof Chart === "undefined") {
            return;
        }

        let growthChart;
        let sourceChart;

        function renderCharts(data) {
            const growthCanvas = document.getElementById("leadGrowthChart");
            const sourceCanvas = document.getElementById("sourceChart");

            if (growthCanvas) {
                growthChart?.destroy();
                growthChart = new Chart(growthCanvas, {
                    type: "line",
                    data: {
                        labels: data.growth.map((item) => item.label),
                        datasets: [{
                            label: "Leads",
                            data: data.growth.map((item) => item.count),
                            borderColor: "#2563eb",
                            backgroundColor: "rgba(37, 99, 235, 0.12)",
                            fill: true,
                            tension: 0.32,
                            borderWidth: 3
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } }
                    }
                });
            }

            if (sourceCanvas) {
                sourceChart?.destroy();
                sourceChart = new Chart(sourceCanvas, {
                    type: "doughnut",
                    data: {
                        labels: Object.keys(data.source),
                        datasets: [{
                            data: Object.values(data.source),
                            backgroundColor: ["#2563eb", "#4f46e5", "#14b8a6", "#0f172a", "#8b5cf6", "#06b6d4"],
                            borderWidth: 0
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        cutout: "70%"
                    }
                });
            }
        }

        renderCharts(window.dashboardData);
    }

    function setupLeadDeletion() {
        const deleteButtons = document.querySelectorAll(".delete-lead-btn");
        if (!deleteButtons.length) {
            return;
        }

        deleteButtons.forEach((button) => {
            button.addEventListener("click", async () => {
                if (!confirm("Delete this lead?")) {
                    return;
                }

                const original = button.textContent;
                button.disabled = true;
                button.textContent = "Deleting...";

                try {
                    await fetch(`/api/leads/${button.dataset.id}`, {
                        method: "DELETE",
                        credentials: "same-origin"
                    });
                    button.closest("tr")?.remove();
                } catch {
                    alert("Unable to delete lead right now.");
                    button.textContent = original || "Delete";
                    button.disabled = false;
                }
            });
        });
    }

    setupTopbar();
    setupRevealAnimations();
    setupLeadCapture();
    setupDemoBooking();
    setupDomainBrowser();
    setupPipelineInteractions();
    setupDashboardRefresh();
    setupLeadDeletion();

    document.addEventListener("coepd:lead-updated", (event) => {
        const leadId = event.detail?.leadId;
        if (leadId) {
            updateDemoState(leadId);
        }
    });
})();
