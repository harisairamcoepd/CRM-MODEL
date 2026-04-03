(function () {
    const token = document.querySelector('meta[name="access-token"]')?.content || "";

    /* =============================================
       HERO — Particle Canvas + Counter Animations
       ============================================= */
    const canvas = document.getElementById("hero-particles");
    if (canvas) {
        const ctx = canvas.getContext("2d");
        let particles = [];
        const PARTICLE_COUNT = 50;
        const CONNECTION_DIST = 120;

        function resize() {
            canvas.width = canvas.offsetWidth;
            canvas.height = canvas.offsetHeight;
        }
        resize();
        window.addEventListener("resize", resize);

        function createParticle() {
            return {
                x: Math.random() * canvas.width,
                y: Math.random() * canvas.height,
                vx: (Math.random() - 0.5) * 0.4,
                vy: (Math.random() - 0.5) * 0.4,
                r: Math.random() * 2 + 1,
                opacity: Math.random() * 0.3 + 0.1
            };
        }

        for (let i = 0; i < PARTICLE_COUNT; i++) particles.push(createParticle());

        function drawParticles() {
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            // Draw connections
            for (let i = 0; i < particles.length; i++) {
                for (let j = i + 1; j < particles.length; j++) {
                    const dx = particles[i].x - particles[j].x;
                    const dy = particles[i].y - particles[j].y;
                    const dist = Math.sqrt(dx * dx + dy * dy);
                    if (dist < CONNECTION_DIST) {
                        const alpha = (1 - dist / CONNECTION_DIST) * 0.08;
                        ctx.strokeStyle = `rgba(37, 99, 235, ${alpha})`;
                        ctx.lineWidth = 1;
                        ctx.beginPath();
                        ctx.moveTo(particles[i].x, particles[i].y);
                        ctx.lineTo(particles[j].x, particles[j].y);
                        ctx.stroke();
                    }
                }
            }

            // Draw particles
            particles.forEach(p => {
                ctx.beginPath();
                ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
                ctx.fillStyle = `rgba(37, 99, 235, ${p.opacity})`;
                ctx.fill();

                p.x += p.vx;
                p.y += p.vy;

                if (p.x < 0 || p.x > canvas.width) p.vx *= -1;
                if (p.y < 0 || p.y > canvas.height) p.vy *= -1;
            });

            requestAnimationFrame(drawParticles);
        }
        drawParticles();
    }

    /* Floating Card Counter Animation */
    function animateCounter(el) {
        const target = parseInt(el.dataset.count, 10);
        const isPct = el.classList.contains("fc-value-pct");
        const duration = 1800;
        const start = performance.now();

        function tick(now) {
            const progress = Math.min((now - start) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            const current = Math.round(eased * target);
            el.textContent = isPct ? current + "%" : current.toLocaleString();
            if (progress < 1) requestAnimationFrame(tick);
        }
        requestAnimationFrame(tick);
    }

    const counterObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                animateCounter(entry.target);
                counterObserver.unobserve(entry.target);
            }
        });
    }, { threshold: 0.3 });

    document.querySelectorAll(".fc-value[data-count]").forEach(el => counterObserver.observe(el));

    /* Generic Scroll Reveal Observer */
    const scrollRevealObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add("active");
                // Optional: scrollRevealObserver.unobserve(entry.target); // If you only want it to reveal once
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll(".section, .story-card, .testimonial-card, .kpi-card, .domain-card").forEach(el => {
        el.classList.add("reveal");
        scrollRevealObserver.observe(el);
    });

    /* Parallax for floating cards on mouse move */
    const heroPremium = document.querySelector(".hero-premium");
    if (heroPremium) {
        heroPremium.addEventListener("mousemove", (e) => {
            const rect = heroPremium.getBoundingClientRect();
            const x = (e.clientX - rect.left) / rect.width - 0.5;
            const y = (e.clientY - rect.top) / rect.height - 0.5;

            document.querySelectorAll(".floating-card").forEach(card => {
                const depth = parseFloat(card.dataset.depth) || 1;
                const moveX = x * depth * 20;
                const moveY = y * depth * 15;
                card.style.transform = `translate(${moveX}px, ${moveY}px)`;
            });
        });

        heroPremium.addEventListener("mouseleave", () => {
            document.querySelectorAll(".floating-card").forEach(card => {
                card.style.transition = "transform 0.6s cubic-bezier(0.34, 1.56, 0.64, 1)";
                card.style.transform = "";
                setTimeout(() => { card.style.transition = ""; }, 600);
            });
        });
    }

    /* =============================================
       API + FORM HANDLERS
       ============================================= */

    /* ── Ripple Effect ── */
    document.addEventListener("click", function (e) {
        const target = e.target.closest(".btn, .chatbot-quick-replies button, .chip, .filter-btn");
        if (!target) return;
        target.style.position = target.style.position || "relative";
        target.style.overflow = "hidden";
        const ripple = document.createElement("span");
        ripple.className = "ripple";
        const rect = target.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height) * 2;
        ripple.style.width = ripple.style.height = size + "px";
        ripple.style.left = (e.clientX - rect.left - size / 2) + "px";
        ripple.style.top = (e.clientY - rect.top - size / 2) + "px";
        target.appendChild(ripple);
        ripple.addEventListener("animationend", () => ripple.remove());
    });

    /* ── Success Animation Helper ── */
    function showFormSuccess(form, title, subtitle) {
        const wrapper = form.closest(".hero-panel") || form.closest(".demo-form-card") || form.parentElement;
        if (!wrapper) return;
        wrapper.style.position = "relative";
        const overlay = document.createElement("div");
        overlay.className = "form-success-overlay";
        overlay.innerHTML =
            '<div class="success-check"><svg viewBox="0 0 24 24"><path d="M5 13l4 4L19 7"/></svg></div>' +
            '<h4>' + title + '</h4>' +
            '<p>' + subtitle + '</p>';
        wrapper.appendChild(overlay);
        setTimeout(() => {
            overlay.style.transition = "opacity 0.4s ease";
            overlay.style.opacity = "0";
            setTimeout(() => overlay.remove(), 400);
        }, 2500);
    }

    /* ── Skeleton Loader Helper ── */
    function showFormSkeleton(form) {
        form.querySelectorAll("input, select, button, .input-grid, .demo-input-group, .demo-form-actions").forEach(el => {
            el.style.visibility = "hidden";
        });
        const skel = document.createElement("div");
        skel.className = "form-skeleton-overlay";
        skel.style.cssText = "display:flex;flex-direction:column;gap:14px;padding:4px 0;";
        skel.innerHTML =
            '<div class="skeleton skeleton-text w-full" style="height:48px;border-radius:16px;"></div>' +
            '<div class="skeleton skeleton-text w-full" style="height:48px;border-radius:16px;"></div>' +
            '<div class="skeleton skeleton-btn"></div>';
        form.appendChild(skel);
        return skel;
    }

    function hideFormSkeleton(form, skel) {
        if (skel) {
            skel.style.transition = "opacity 0.3s ease";
            skel.style.opacity = "0";
            setTimeout(() => skel.remove(), 300);
        }
        form.querySelectorAll("input, select, button, .input-grid, .demo-input-group, .demo-form-actions").forEach(el => {
            el.style.visibility = "";
        });
    }

    async function requestJson(url, method, payload, authToken) {
        const headers = { "Content-Type": "application/json" };
        if (authToken) headers["Authorization"] = `Bearer ${authToken}`;
        const response = await fetch(url, { method, headers, body: JSON.stringify(payload), credentials: "same-origin" });
        if (!response.ok) {
            const errBody = await response.json().catch(() => null);
            const msg = errBody?.message || errBody?.errors?.join(", ") || "Request failed";
            throw new Error(msg);
        }
        return response.json();
    }

    async function postJson(url, payload, authToken) {
        return requestJson(url, "POST", payload, authToken);
    }

    async function putJson(url, payload, authToken) {
        return requestJson(url, "PUT", payload, authToken);
    }

    const pipelineStageMeta = {
        New: { label: "New", progress: 25 },
        Contacted: { label: "Contacted", progress: 50 },
        DemoBooked: { label: "Demo Booked", progress: 75 },
        Converted: { label: "Converted", progress: 100 }
    };

    function syncPipelineCounts() {
        document.querySelectorAll("[data-stage-cards]").forEach((container) => {
            const stage = container.getAttribute("data-stage-cards");
            const count = container.querySelectorAll(".pipeline-card").length;
            const badge = document.querySelector(`[data-stage-count="${stage}"]`);
            if (badge) badge.textContent = count;
        });
    }

    function updatePipelineCard(card, stage) {
        const meta = pipelineStageMeta[stage] ?? pipelineStageMeta.New;
        card.dataset.currentStage = stage;

        const label = card.querySelector(".kanban-progress-label");
        const value = card.querySelector(".kanban-progress-value");
        const fill = card.querySelector(".kanban-card-progress .kanban-progress-fill");

        if (label) label.textContent = meta.label;
        if (value) value.textContent = `${meta.progress}%`;
        if (fill) fill.style.width = `${meta.progress}%`;
    }

    function movePipelineCard(card, stage) {
        const destination = document.querySelector(`[data-stage-cards="${stage}"]`);
        if (!destination) return;
        destination.prepend(card);
        updatePipelineCard(card, stage);
        syncPipelineCounts();
    }

    async function trackFunnelEvent(stage, leadId) {
        if (!leadId || Number(leadId) <= 0) return;
        await fetch("/api/funnel/event", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ leadId: Number(leadId), stage })
        });
    }

    const leadForm = document.getElementById("lead-form");
    if (leadForm) {
        leadForm.addEventListener("submit", async (event) => {
            event.preventDefault();
            const status = document.getElementById("lead-form-status");
            const submitBtn = leadForm.querySelector('button[type="submit"]');
            const originalHTML = submitBtn.innerHTML;
            try {
                submitBtn.disabled = true;
                submitBtn.classList.add("btn-submitting");
                submitBtn.innerHTML = '<span class="btn-spinner"></span> Saving...';
                status.className = "status-text";
                status.textContent = "";
                const payload = Object.fromEntries(new FormData(leadForm).entries());
                const result = await postJson("/api/leads", payload);
                const createdLead = result.lead ?? result;

                submitBtn.classList.remove("btn-submitting");
                submitBtn.classList.add("btn-success-state");
                submitBtn.innerHTML = '<i class="fas fa-check"></i> Saved!';

                showFormSuccess(leadForm, "Lead Created!", "Your Lead ID is " + createdLead.id);

                status.textContent = "Lead created. Your Lead ID is " + createdLead.id + ".";
                status.classList.add("success");
                leadForm.reset();

                const demoLeadInput = document.querySelector('#demo-form input[name="leadId"]');
                if (demoLeadInput) {
                    demoLeadInput.value = createdLead.id;
                    setTimeout(() => {
                        document.getElementById("demo-booking")?.scrollIntoView({ behavior: "smooth" });
                    }, 1500);
                }
                localStorage.setItem("coepd-lead-id", createdLead.id);

                setTimeout(() => {
                    submitBtn.classList.remove("btn-success-state");
                    submitBtn.innerHTML = originalHTML;
                }, 2800);
            } catch (err) {
                status.textContent = err.message;
                status.classList.add("error");
                leadForm.classList.add("shake");
                setTimeout(() => leadForm.classList.remove("shake"), 500);
            } finally {
                submitBtn.disabled = false;
                submitBtn.classList.remove("btn-submitting");
                if (!submitBtn.classList.contains("btn-success-state")) {
                    submitBtn.innerHTML = originalHTML;
                }
            }
        });
    }

    const demoForm = document.getElementById("demo-form");
    if (demoForm) {
        const storedLeadId = localStorage.getItem("coepd-lead-id");
        const demoLeadInput = demoForm.querySelector('input[name="leadId"]');
        if (storedLeadId && demoLeadInput && !demoLeadInput.value) {
            demoLeadInput.value = storedLeadId;
        }

        /* ── Step Navigation ── */
        const steps = demoForm.querySelectorAll('.demo-form-step');
        const indicators = document.querySelectorAll('.demo-step');
        const stepLine = document.querySelector('.demo-step-line');
        const nextBtn = demoForm.querySelector('.btn-demo-next');
        const backBtn = demoForm.querySelector('.btn-demo-back');

        function goToStep(n) {
            steps.forEach(s => { s.classList.remove('demo-form-step--active'); s.style.display = 'none'; });
            indicators.forEach(ind => { ind.classList.remove('active', 'completed'); });
            const target = demoForm.querySelector('.demo-form-step[data-step="' + n + '"]');
            if (target) { target.style.display = 'flex'; target.classList.add('demo-form-step--active'); }
            indicators.forEach(ind => {
                const s = parseInt(ind.dataset.step);
                if (s < n) ind.classList.add('completed');
                if (s === n) ind.classList.add('active');
            });
            if (stepLine) stepLine.classList.toggle('filled', n > 1);
        }

        if (nextBtn) nextBtn.addEventListener('click', () => {
            const leadInput = demoForm.querySelector('input[name="leadId"]');
            if (!leadInput.value.trim()) { leadInput.focus(); return; }
            goToStep(2);
        });
        if (backBtn) backBtn.addEventListener('click', () => goToStep(1));

        demoForm.addEventListener("submit", async (event) => {
            event.preventDefault();
            const status = document.getElementById("demo-form-status");
            const submitBtn = demoForm.querySelector('.btn-demo-submit');
            try {
                submitBtn.disabled = true;
                submitBtn.classList.add("btn-submitting");
                submitBtn.innerHTML = '<span class="btn-spinner"></span> Booking...';
                status.className = "status-text";
                status.textContent = "";
                const formData = new FormData(demoForm);
                const leadId = Number(formData.get("leadId"));
                await trackFunnelEvent("Desire", leadId);
                const result = await postJson("/api/demo", {
                    leadId,
                    day: formData.get("day"),
                    slot: formData.get("slot")
                });
                const booking = result.booking ?? result;

                submitBtn.classList.remove("btn-submitting");
                submitBtn.classList.add("btn-success-state");
                submitBtn.innerHTML = '<i class="fas fa-check"></i> Booked!';

                showFormSuccess(demoForm, "Demo Confirmed!", booking.day + " \u2022 " + booking.slot);

                status.textContent = "Demo booked: " + booking.id + ", " + booking.day + ", " + booking.slot + ".";
                status.classList.add("success");
                demoForm.reset();
                setTimeout(() => {
                    goToStep(1);
                    submitBtn.classList.remove("btn-success-state");
                    submitBtn.innerHTML = '<i class="fas fa-calendar-check"></i> Confirm Demo';
                }, 2800);
            } catch (err) {
                status.textContent = err.message;
                status.classList.add("error");
                demoForm.classList.add("shake");
                setTimeout(() => demoForm.classList.remove("shake"), 500);
            } finally {
                submitBtn.disabled = false;
                submitBtn.classList.remove("btn-submitting");
                if (!submitBtn.classList.contains("btn-success-state")) {
                    submitBtn.innerHTML = '<i class="fas fa-calendar-check"></i> Confirm Demo';
                }
            }
        });
    }

    // --- DOMAIN GRID SEARCH & FILTER ---
    const domainSearch = document.getElementById("domain-search");
    const filterButtons = document.querySelectorAll(".filter-btn");
    const domainCards = document.querySelectorAll(".domain-card");

    function updateDomainCards() {
        const searchTerm = domainSearch?.value.toLowerCase() || "";
        const activeFilter = document.querySelector(".filter-btn.active")?.dataset.filter || "all";

        domainCards.forEach(card => {
            const name = card.dataset.name || "";
            const category = card.dataset.category || "";
            const matchesSearch = name.includes(searchTerm);
            const matchesFilter = activeFilter === "all" || category === activeFilter;

            if (matchesSearch && matchesFilter) {
                card.style.display = "flex";
                card.style.opacity = "1";
                card.style.transform = "scale(1)";
            } else {
                card.style.display = "none";
                card.style.opacity = "0";
                card.style.transform = "scale(0.95)";
            }
        });
    }

    if (domainSearch) {
        domainSearch.addEventListener("input", updateDomainCards);
    }

    filterButtons.forEach(btn => {
        btn.addEventListener("click", () => {
            filterButtons.forEach(b => b.classList.remove("active"));
            btn.classList.add("active");
            updateDomainCards();
        });
    });

    document.addEventListener("click", async (event) => {
        const button = event.target.closest(".pipeline-update-btn");
        if (!button) return;

        const card = button.closest(".pipeline-card");
        const select = card?.querySelector(".pipeline-status-select");
        const stage = select?.value;
        if (!card || !stage) return;

        const original = button.textContent;
        button.disabled = true;
        button.textContent = "Moving...";

        try {
            const result = await putJson(`/api/pipeline/${button.dataset.id}/move`, { stage }, token);
            const updatedLead = result?.lead;
            const updatedStage = updatedLead?.status || stage;

            movePipelineCard(card, updatedStage);
            if (select) select.value = updatedStage;

            button.textContent = "Updated";
            setTimeout(() => {
                button.disabled = false;
                button.textContent = original || "Move Stage";
            }, 800);
        } catch (err) {
            alert(`Failed to update status: ${err.message}`);
            button.disabled = false;
            button.textContent = original || "Move Stage";
        }
    });

    if (window.dashboardData) {
        let growthChart = null;
        let sourceChart = null;

        function renderCharts(data) {
            const growthCtx = document.getElementById("leadGrowthChart");
            const sourceCtx = document.getElementById("sourceChart");
            if (growthCtx) {
                if (growthChart) growthChart.destroy();
                growthChart = new Chart(growthCtx, {
                    type: "line",
                    data: {
                        labels: data.growth.map((x) => x.label),
                        datasets: [{
                            label: "Leads",
                            data: data.growth.map((x) => x.count),
                            borderColor: "#004CFF",
                            backgroundColor: "rgba(0, 76, 255, 0.08)",
                            fill: true,
                            tension: 0.4,
                            pointBackgroundColor: "#004CFF",
                            pointRadius: 4,
                            borderWidth: 3
                        }]
                    },
                    options: { 
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: { 
                            y: { beginAtZero: true, grid: { color: "#F1F5F9" } },
                            x: { grid: { display: false } }
                        } 
                    }
                });
            }
            if (sourceCtx) {
                if (sourceChart) sourceChart.destroy();
                sourceChart = new Chart(sourceCtx, {
                    type: "doughnut",
                    data: {
                        labels: Object.keys(data.source),
                        datasets: [{
                            data: Object.values(data.source),
                            backgroundColor: ["#004CFF", "#00D1FF", "#6366F1", "#8B5CF6", "#EC4899", "#F43F5E"],
                            borderWidth: 0
                        }]
                    },
                    options: { 
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { position: 'bottom', labels: { usePointStyle: true, padding: 20 } } },
                        cutout: '70%'
                    }
                });
            }
        }

        function updateStatCards(stats) {
            const totalEl = document.querySelector('[data-stat="totalLeads"]');
            const todayEl = document.querySelector('[data-stat="todayLeads"]');
            const monthEl = document.querySelector('[data-stat="thisMonthLeads"]');
            const bookingsEl = document.querySelector('[data-stat="totalBookings"]');
            const sourcesEl = document.querySelector('[data-stat="activeSources"]');
            const weeklyGrowthEl = document.querySelector('[data-stat="weeklyGrowth"]');
            if (totalEl) totalEl.textContent = stats.totalLeads ?? 0;
            if (todayEl) todayEl.textContent = stats.todayLeads ?? 0;
            if (monthEl) monthEl.textContent = stats.thisMonthLeads ?? 0;
            if (bookingsEl) bookingsEl.textContent = stats.totalBookings ?? 0;
            if (sourcesEl) sourcesEl.textContent = Object.keys(stats.sourceBreakdown ?? {}).length;
            if (weeklyGrowthEl) {
                const growthValue = stats.weeklyGrowthPercentage ?? 0;
                weeklyGrowthEl.innerHTML = `<i class="ph-bold ph-trend-up"></i> +${growthValue}% this week`;
            }
        }

        renderCharts(window.dashboardData);

        async function refreshDashboard() {
            try {
                const statsRes = await fetch("/api/stats", { credentials: "same-origin" });
                if (statsRes.ok) {
                    const stats = await statsRes.json();
                    updateStatCards(stats);
                }

                const dashboardHeaders = token ? { Authorization: `Bearer ${token}` } : {};
                const [dashboardStatsRes, growthRes] = await Promise.all([
                    fetch("/api/dashboard/stats", { headers: dashboardHeaders, credentials: "same-origin" }),
                    fetch("/api/dashboard/lead-growth", { headers: dashboardHeaders, credentials: "same-origin" })
                ]);

                if (dashboardStatsRes.ok && growthRes.ok) {
                    const dashboardStats = await dashboardStatsRes.json();
                    const growth = await growthRes.json();
                    updateStatCards({
                        totalLeads: dashboardStats.totalLeads,
                        todayLeads: dashboardStats.todayLeads,
                        thisMonthLeads: dashboardStats.thisMonthLeads,
                        totalBookings: dashboardStats.totalBookings,
                        weeklyGrowthPercentage: dashboardStats.weeklyGrowthPercentage,
                        sourceBreakdown: dashboardStats.sourceBreakdown
                    });
                    renderCharts({ growth, source: dashboardStats.sourceBreakdown });
                }
            } catch {
                // retry on next interval
            }
        }
        setInterval(refreshDashboard, 30000);

        document.querySelectorAll(".delete-lead-btn").forEach((button) => {
            button.addEventListener("click", async () => {
                if (!token) return alert("Auth token missing. Please log in again.");
                if (!confirm("Delete this lead?")) return;
                button.disabled = true;
                button.textContent = "Deleting...";
                try {
                    const response = await fetch(`/api/leads/${button.dataset.id}`, {
                        method: "DELETE",
                        headers: { Authorization: `Bearer ${token}` }
                    });
                    if (response.ok) {
                        button.closest("tr")?.remove();
                    } else {
                        alert("Failed to delete lead.");
                        button.disabled = false;
                        button.textContent = "Delete";
                    }
                } catch {
                    alert("Network error deleting lead.");
                    button.disabled = false;
                    button.textContent = "Delete";
                }
            });
        });
    }
})();
