(function () {
    const apiBase = document.body.dataset.apiBase || (window.location.protocol === "file:" ? "http://localhost:5099" : "");
    const apiUrl = (path) => `${apiBase}${path}`;
    const requestCredentials = window.location.protocol === "file:" ? "omit" : "include";
    const leadStorageKey = "coepd-public-lead-id";
    Array.from(document.querySelectorAll(".chatbot")).slice(1).forEach((node) => node.remove());

    const refs = {
        navbar: document.querySelector(".navbar"),
        menuButton: document.querySelector(".mobile-menu-btn"),
        navLinks: document.getElementById("primary-nav"),
        leadForm: document.getElementById("lead-capture-form"),
        leadSubmit: document.getElementById("lead-submit"),
        leadStatus: document.getElementById("lead-form-status"),
        demoForm: document.getElementById("demo-booking-form"),
        demoSubmit: document.getElementById("demo-submit"),
        demoStatus: document.getElementById("demo-form-status"),
        demoDate: document.getElementById("demo-date"),
        demoTimeSlot: document.getElementById("demo-time-slot"),
        demoLeadId: document.getElementById("demo-lead-id"),
        availabilityStatus: document.getElementById("availability-status"),
        leadStep: document.getElementById("lead-step"),
        demoStep: document.getElementById("demo-step"),
        leadStateCard: document.getElementById("lead-state-card"),
        leadStateTitle: document.getElementById("lead-state-title"),
        leadStateCopy: document.getElementById("lead-state-copy"),
        domainSearch: document.getElementById("domain-search"),
        domainGrid: document.getElementById("domain-grid"),
        domainSummary: document.getElementById("domain-results-summary"),
        domainLoadMore: document.getElementById("domain-load-more"),
        domainClear: document.getElementById("domain-clear"),
        filterButtons: Array.from(document.querySelectorAll(".filter-btn")),
        chatbotToggle: document.getElementById("chatbot-toggle"),
        chatbotPanel: document.getElementById("chatbot-panel"),
        chatbotClose: document.getElementById("chatbot-close"),
        chatbotMessages: document.getElementById("chatbot-messages"),
        chatbotQuickReplies: document.getElementById("chatbot-quick-replies"),
        chatbotForm: document.getElementById("chatbot-form"),
        chatbotInput: document.getElementById("chatbot-text")
    };

    const fallbackDomainCatalog = [
        { name: "Business Analysis", category: "Business Analysis", description: "Requirement lifecycle, stakeholder management, and process mapping." },
        { name: "Healthcare BA", category: "Business Analysis", description: "Healthcare workflows, compliance context, and domain-specific analysis." },
        { name: "Data Analytics", category: "Analytics", description: "Reporting, KPI tracking, and business insight generation." },
        { name: "Power BI", category: "Analytics", description: "Interactive dashboards and decision-focused visual analytics." },
        { name: "Product Management", category: "Product & Delivery", description: "Product discovery, roadmap planning, and execution strategy." },
        { name: "Agile & Scrum", category: "Product & Delivery", description: "Agile ceremonies, sprint operations, and delivery velocity." },
        { name: "Project Management", category: "Product & Delivery", description: "Delivery planning, stakeholder updates, and risk management." },
        { name: "Salesforce", category: "Technology", description: "CRM platform fundamentals and implementation workflows." },
        { name: "Generative AI", category: "Technology", description: "AI-assisted workflows, prompt design, and practical implementation." },
        { name: "Cloud Fundamentals", category: "Technology", description: "Core cloud concepts and modern platform delivery patterns." },
        { name: "Automation Testing", category: "Technology", description: "Automation frameworks and scalable QA workflows." },
        { name: "DevOps Fundamentals", category: "Technology", description: "CI/CD basics, deployment pipelines, and team operations." }
    ];

    const deriveDomainCatalogFromSelect = () => {
        const select = document.getElementById("lead-domain");
        if (!select) {
            return [];
        }

        return Array.from(select.options)
            .map((option) => option.value.trim())
            .filter((value) => Boolean(value))
            .map((name) => ({
                name,
                category: "Technology",
                description: "Track details and outcomes available in the guided demo workflow."
            }));
    };

    const normalizeDomainItem = (item) => {
        const name = (item?.name || "").trim();
        const category = (item?.category || "Technology").trim();
        const description = (item?.description || "Track details and outcomes available in the guided demo workflow.").trim();
        return { name, category, description };
    };

    const rawDomainCatalog = Array.isArray(window.coepdDomainCatalog) ? window.coepdDomainCatalog : [];
    const derivedDomainCatalog = deriveDomainCatalogFromSelect();
    const domainCatalog = (rawDomainCatalog.length ? rawDomainCatalog : (derivedDomainCatalog.length ? derivedDomainCatalog : fallbackDomainCatalog))
        .map(normalizeDomainItem)
        .filter((item) => item.name.length > 0);

    const browserState = {
        search: "",
        filter: "all",
        visibleCount: window.innerWidth < 768 ? 8 : 10
    };

    const chatbotState = {
        step: "welcome",
        leadId: null,
        draft: {
            name: "",
            phone: "",
            email: "",
            location: "",
            domain: "",
            demoDate: "",
            demoTimeSlot: ""
        }
    };

    const requestJson = async (path, options = {}) => {
        const response = await fetch(apiUrl(path), {
            credentials: requestCredentials,
            headers: {
                "Accept": "application/json",
                ...(options.body ? { "Content-Type": "application/json" } : {}),
                ...(options.headers || {})
            },
            ...options
        });

        const body = await response.json().catch(() => null);
        if (!response.ok) {
            const message = body?.title || body?.message || body?.errors?.join(", ") || "Request failed.";
            throw new Error(message);
        }

        return body;
    };

    const setStatus = (element, message, kind) => {
        if (!element) {
            return;
        }

        element.textContent = message || "";
        element.className = "form-status";
        if (kind) {
            element.classList.add(kind);
        }
    };

    const closeMenu = () => {
        if (!refs.menuButton || !refs.navLinks) {
            return;
        }

        refs.navLinks.classList.remove("is-open");
        refs.menuButton.setAttribute("aria-expanded", "false");
    };

    const setupNavigation = () => {
        refs.menuButton?.addEventListener("click", () => {
            const willOpen = !refs.navLinks?.classList.contains("is-open");
            refs.navLinks?.classList.toggle("is-open", willOpen);
            refs.menuButton?.setAttribute("aria-expanded", String(willOpen));
        });

        window.addEventListener("resize", () => {
            if (window.innerWidth > 780) {
                closeMenu();
            }
        });

        const navAnchors = Array.from(document.querySelectorAll('.nav-links a[href^="#"]'));
        navAnchors.forEach((anchor) => {
            anchor.addEventListener("click", (event) => {
                const href = anchor.getAttribute("href");
                if (!href || href === "#") {
                    return;
                }

                const target = document.querySelector(href);
                if (!target) {
                    return;
                }

                event.preventDefault();
                const top = target.getBoundingClientRect().top + window.scrollY - 92;
                window.scrollTo({ top, behavior: "smooth" });
                closeMenu();
            });
        });

        const setActiveNav = (targetId) => {
            navAnchors.forEach((anchor) => {
                const href = anchor.getAttribute("href") || "";
                anchor.classList.toggle("is-active", href === `#${targetId}`);
            });
        };

        const observedSections = navAnchors
            .map((anchor) => anchor.getAttribute("href"))
            .filter((href) => Boolean(href && href.startsWith("#")))
            .map((href) => document.querySelector(href))
            .filter((section) => section instanceof HTMLElement);

        if ("IntersectionObserver" in window && observedSections.length) {
            const navObserver = new IntersectionObserver((entries) => {
                const visible = entries
                    .filter((entry) => entry.isIntersecting)
                    .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];

                if (visible?.target?.id) {
                    setActiveNav(visible.target.id);
                }
            }, {
                threshold: [0.2, 0.35, 0.55],
                rootMargin: "-18% 0px -55% 0px"
            });

            observedSections.forEach((section) => navObserver.observe(section));
        }

        const updateNavbarScrollState = () => {
            refs.navbar?.classList.toggle("is-scrolled", window.scrollY > 8);
        };

        updateNavbarScrollState();
        window.addEventListener("scroll", updateNavbarScrollState, { passive: true });
    };

    const animateOnLoad = () => {
        document.querySelectorAll(".hero-copy, .hero-panel")
            .forEach((element, index) => {
                element.classList.add("is-fade-up");
                element.style.animationDelay = `${Math.min(index * 0.04, 0.28)}s`;
            });

        const revealTargets = document.querySelectorAll([
            "main .section-head",
            "main .stat",
            "main .problem",
            "main .solution",
            "main .domain-browser",
            "main .demo-card",
            "main .testimonial",
            "main .final-cta-card"
        ].join(", "));

        revealTargets.forEach((element) => element.classList.add("reveal-item"));

        if (!("IntersectionObserver" in window)) {
            revealTargets.forEach((element) => element.classList.add("is-visible"));
            return;
        }

        const revealObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting) {
                    return;
                }

                entry.target.classList.add("is-visible");
                observer.unobserve(entry.target);
            });
        }, {
            threshold: 0.15,
            rootMargin: "0px 0px -10% 0px"
        });

        revealTargets.forEach((element) => revealObserver.observe(element));
    };

    const computeConversionRate = (stats) => {
        const total = Number(stats.totalLeads || 0);
        const converted = Number(stats.conversionCount || 0);
        return total === 0 ? 0 : ((converted / total) * 100);
    };

    const updateStatText = (id, value) => {
        const element = document.getElementById(id);
        if (!element) {
            return;
        }

        element.textContent = value;
    };

    const loadStats = async () => {
        try {
            const stats = await requestJson("/api/stats");
            const conversionRate = computeConversionRate(stats);

            updateStatText("hero-total-leads", Number(stats.totalLeads || 0).toLocaleString());
            updateStatText("hero-demo-bookings", Number(stats.totalBookings || 0).toLocaleString());
            updateStatText("hero-conversion-rate", `${conversionRate.toFixed(1)}%`);
            updateStatText("hero-today-leads", Number(stats.todayLeads || 0).toLocaleString());
            updateStatText("hero-month-leads", Number(stats.thisMonthLeads || 0).toLocaleString());
            updateStatText("hero-weekly-growth", `${Number(stats.weeklyGrowthPercentage || 0).toFixed(1)}%`);

            updateStatText("trust-total-leads", Number(stats.totalLeads || 0).toLocaleString());
            updateStatText("trust-today-leads", Number(stats.todayLeads || 0).toLocaleString());
            updateStatText("trust-total-bookings", Number(stats.totalBookings || 0).toLocaleString());
            updateStatText("trust-conversion-count", Number(stats.conversionCount || 0).toLocaleString());
        } catch {
            // Landing stats are decorative. Leave defaults when the API is unavailable.
        }
    };

    const getBrowserBatchSize = () => window.innerWidth < 768 ? 8 : 10;

    const getFilteredDomains = () => {
        const normalizedFilter = browserState.filter.toLowerCase();
        return domainCatalog.filter((item) => {
            const matchesSearch = !browserState.search
                || item.name.toLowerCase().includes(browserState.search)
                || item.description.toLowerCase().includes(browserState.search)
                || item.category.toLowerCase().includes(browserState.search);
            const matchesFilter = normalizedFilter === "all" || item.category.toLowerCase() === normalizedFilter;
            return matchesSearch && matchesFilter;
        });
    };

    const focusLeadFormWithDomain = (domainName) => {
        const select = document.getElementById("lead-domain");
        if (!select || !domainName) {
            return;
        }

        let option = Array.from(select.options).find((item) => item.value === domainName);
        if (!option) {
            option = document.createElement("option");
            option.value = domainName;
            option.textContent = domainName;
            select.appendChild(option);
        }

        select.value = domainName;
        document.getElementById("demo-cta")?.scrollIntoView({ behavior: "smooth", block: "start" });
        select.focus();
    };

    const getDomainIcon = (category) => {
        const normalized = (category || "").toLowerCase();
        if (normalized.includes("analysis")) {
            return "BA";
        }

        if (normalized.includes("analytics")) {
            return "DA";
        }

        if (normalized.includes("product") || normalized.includes("delivery")) {
            return "PM";
        }

        if (normalized.includes("technology")) {
            return "TE";
        }

        return "CRM";
    };

    const renderDomainBrowser = () => {
        if (!refs.domainGrid) {
            return;
        }

        const filtered = getFilteredDomains();
        const visible = filtered.slice(0, browserState.visibleCount);

        refs.domainGrid.innerHTML = "";

        if (visible.length === 0) {
            const empty = document.createElement("div");
            empty.className = "domain-empty card-soft";
            empty.textContent = "No domains match your search. Try another keyword or clear filters.";
            refs.domainGrid.appendChild(empty);
        }

        visible.forEach((domain) => {
            const card = document.createElement("button");
            card.type = "button";
            card.className = "domain-card card-soft";
            card.dataset.domain = domain.name;
            card.dataset.category = domain.category;
            card.innerHTML = `
                <div class="domain-meta">
                    <span class="domain-icon" aria-hidden="true">${getDomainIcon(domain.category)}</span>
                    <span class="domain-category">${domain.category}</span>
                </div>
                <strong>${domain.name}</strong>
                <p>${domain.description}</p>
                <span class="domain-action">Use this track</span>
            `;
            refs.domainGrid.appendChild(card);
        });

        if (refs.domainSummary) {
            refs.domainSummary.textContent = filtered.length
                ? `${visible.length} of ${filtered.length} domains shown`
                : "No domains found";
        }

        if (refs.domainLoadMore) {
            refs.domainLoadMore.hidden = filtered.length === 0 || filtered.length <= browserState.visibleCount;
        }

        if (refs.domainClear) {
            refs.domainClear.hidden = !browserState.search && browserState.filter === "all";
        }
    };

    const setupDomainBrowser = () => {
        if (!refs.domainGrid) {
            return;
        }

        let timer = null;
        browserState.visibleCount = getBrowserBatchSize();

        refs.domainSearch?.addEventListener("input", (event) => {
            window.clearTimeout(timer);
            timer = window.setTimeout(() => {
                browserState.search = event.target.value.trim().toLowerCase();
                browserState.visibleCount = getBrowserBatchSize();
                renderDomainBrowser();
            }, 120);
        });

        if (refs.filterButtons.length) {
            refs.filterButtons.forEach((item) => item.classList.toggle("is-active", item.dataset.filter === "all"));
        }

        refs.filterButtons.forEach((button) => {
            button.addEventListener("click", () => {
                browserState.filter = button.dataset.filter || "all";
                browserState.visibleCount = getBrowserBatchSize();
                refs.filterButtons.forEach((item) => item.classList.toggle("is-active", item === button));
                renderDomainBrowser();
            });
        });

        refs.domainLoadMore?.addEventListener("click", () => {
            browserState.visibleCount += getBrowserBatchSize();
            renderDomainBrowser();
        });

        refs.domainClear?.addEventListener("click", () => {
            browserState.search = "";
            browserState.filter = "all";
            browserState.visibleCount = getBrowserBatchSize();
            if (refs.domainSearch) {
                refs.domainSearch.value = "";
            }

            refs.filterButtons.forEach((item) => item.classList.toggle("is-active", item.dataset.filter === "all"));
            renderDomainBrowser();
        });

        refs.domainGrid.addEventListener("click", (event) => {
            const card = event.target.closest(".domain-card");
            if (!card) {
                return;
            }

            focusLeadFormWithDomain(card.dataset.domain || "");
        });

        window.addEventListener("resize", () => {
            const nextBatchSize = getBrowserBatchSize();
            if (browserState.visibleCount !== nextBatchSize) {
                browserState.visibleCount = Math.max(browserState.visibleCount, nextBatchSize);
                renderDomainBrowser();
            }
        });

        renderDomainBrowser();
    };

    const setLeadState = (leadId) => {
        if (refs.demoLeadId) {
            refs.demoLeadId.value = leadId || "";
        }

        const isUnlocked = Boolean(leadId);
        refs.leadStateCard?.setAttribute("data-locked", String(!isUnlocked));

        if (!isUnlocked) {
            if (refs.leadStateTitle) {
                refs.leadStateTitle.textContent = "Lead capture required";
            }

            if (refs.leadStateCopy) {
                refs.leadStateCopy.textContent = "Complete step one to unlock demo booking with the new Lead ID connected automatically.";
            }

            return;
        }

        if (refs.leadStateTitle) {
            refs.leadStateTitle.textContent = `Lead created: #${leadId}`;
        }

        if (refs.leadStateCopy) {
            refs.leadStateCopy.textContent = "Your demo step is unlocked. Pick a preferred date and time slot to continue the same funnel journey.";
        }
    };

    const showDemoStep = () => {
        refs.leadStep?.classList.add("is-hidden");
        refs.demoStep?.classList.remove("is-hidden");
        refs.demoStep?.setAttribute("aria-hidden", "false");
        refs.leadStep?.setAttribute("aria-hidden", "true");
        document.querySelector('[data-step-chip="lead"]')?.classList.remove("is-active");
        document.querySelector('[data-step-chip="demo"]')?.classList.add("is-active");
    };

    const showLeadStep = () => {
        refs.demoStep?.classList.add("is-hidden");
        refs.leadStep?.classList.remove("is-hidden");
        refs.demoStep?.setAttribute("aria-hidden", "true");
        refs.leadStep?.setAttribute("aria-hidden", "false");
        document.querySelector('[data-step-chip="demo"]')?.classList.remove("is-active");
        document.querySelector('[data-step-chip="lead"]')?.classList.add("is-active");
    };

    const submitLead = async (payload, source = "Website") => {
        const body = {
            ...payload,
            source,
            status: "New"
        };

        const result = await requestJson("/api/leads", {
            method: "POST",
            headers: source === "Chatbot" ? { "X-Client-App": "chatbot" } : {},
            body: JSON.stringify(body)
        });

        return result?.lead?.id || result?.id;
    };

    const submitDemo = async (payload) => {
        return requestJson("/api/demo", {
            method: "POST",
            body: JSON.stringify(payload)
        });
    };

    const setupLeadCapture = () => {
        const savedLeadId = window.localStorage.getItem(leadStorageKey);
        if (savedLeadId) {
            setLeadState(savedLeadId);
            showDemoStep();
        } else {
            setLeadState("");
        }

        refs.leadForm?.addEventListener("submit", async (event) => {
            event.preventDefault();
            if (!refs.leadForm || !validateForm(refs.leadForm)) {
                return;
            }

            const originalLabel = refs.leadSubmit?.textContent || "Create Lead And Continue";
            const payload = Object.fromEntries(new FormData(refs.leadForm).entries());

            try {
                if (refs.leadSubmit) {
                    refs.leadSubmit.disabled = true;
                    refs.leadSubmit.textContent = "Creating lead...";
                }

                setStatus(refs.leadStatus, "", null);
                const leadId = await submitLead(payload);

                if (!leadId) {
                    throw new Error("Lead id was not returned by the API.");
                }

                window.localStorage.setItem(leadStorageKey, String(leadId));
                setLeadState(String(leadId));
                setStatus(refs.leadStatus, `Lead #${leadId} created successfully. Continue to book the demo.`, "success");
                showDemoStep();
                document.getElementById("demo-step")?.scrollIntoView({ behavior: "smooth", block: "nearest" });
            } catch (error) {
                setStatus(refs.leadStatus, error.message || "Lead capture failed.", "error");
            } finally {
                if (refs.leadSubmit) {
                    refs.leadSubmit.disabled = false;
                    refs.leadSubmit.textContent = originalLabel;
                }
            }
        });
    };

    const checkAvailability = async () => {
        const date = refs.demoDate?.value;
        const timeSlot = refs.demoTimeSlot?.value;
        if (!date || !timeSlot || !refs.availabilityStatus) {
            return;
        }

        refs.availabilityStatus.textContent = "Checking slot availability...";
        refs.availabilityStatus.className = "availability-status";

        try {
            const result = await requestJson(`/api/demo/availability?date=${encodeURIComponent(date)}&timeSlot=${encodeURIComponent(timeSlot)}`);
            if (result?.availability?.isAvailable) {
                refs.availabilityStatus.textContent = "Selected slot is available.";
                refs.availabilityStatus.classList.add("success");
            } else {
                refs.availabilityStatus.textContent = "Selected slot is already taken. Please choose another option.";
                refs.availabilityStatus.classList.add("warning");
            }
        } catch {
            refs.availabilityStatus.textContent = "";
            refs.availabilityStatus.className = "availability-status";
        }
    };

    const setupDemoBooking = () => {
        refs.demoDate?.addEventListener("change", checkAvailability);
        refs.demoTimeSlot?.addEventListener("change", checkAvailability);

        refs.demoForm?.addEventListener("submit", async (event) => {
            event.preventDefault();
            if (!refs.demoForm || !validateForm(refs.demoForm)) {
                return;
            }

            const leadId = refs.demoLeadId?.value;
            if (!leadId) {
                setStatus(refs.demoStatus, "Create the lead first, then continue to demo booking.", "error");
                showLeadStep();
                return;
            }

            const originalLabel = refs.demoSubmit?.textContent || "Confirm Demo";
            const payload = {
                leadId: Number(leadId),
                date: refs.demoDate?.value,
                timeSlot: refs.demoTimeSlot?.value
            };

            try {
                if (refs.demoSubmit) {
                    refs.demoSubmit.disabled = true;
                    refs.demoSubmit.textContent = "Booking demo...";
                }

                setStatus(refs.demoStatus, "", null);
                const result = await submitDemo(payload);
                const confirmationCode = result?.confirmation?.confirmationCode || "Created";

                setStatus(refs.demoStatus, `Demo booked successfully. Confirmation: ${confirmationCode}.`, "success");
                refs.demoForm.reset();
                clearFormErrors(refs.demoForm);
                if (refs.availabilityStatus) {
                    refs.availabilityStatus.textContent = "";
                    refs.availabilityStatus.className = "availability-status";
                }
            } catch (error) {
                setStatus(refs.demoStatus, error.message || "Demo booking failed.", "error");
            } finally {
                if (refs.demoSubmit) {
                    refs.demoSubmit.disabled = false;
                    refs.demoSubmit.textContent = originalLabel;
                }
            }
        });
    };

    const appendChatMessage = (text, type = "bot") => {
        if (!refs.chatbotMessages) {
            return;
        }

        const message = document.createElement("div");
        message.className = `msg ${type}`;
        message.textContent = text;
        refs.chatbotMessages.appendChild(message);
        refs.chatbotMessages.scrollTo({ top: refs.chatbotMessages.scrollHeight, behavior: "smooth" });
    };

    const showChatTyping = () => {
        if (!refs.chatbotMessages) {
            return null;
        }

        const typing = document.createElement("div");
        typing.className = "msg bot";
        typing.innerHTML = `
            <span class="chatbot-typing" aria-label="Assistant typing">
                <span></span><span></span><span></span>
            </span>
        `;
        refs.chatbotMessages.appendChild(typing);
        refs.chatbotMessages.scrollTo({ top: refs.chatbotMessages.scrollHeight, behavior: "smooth" });
        return typing;
    };

    const runWithTyping = async (action) => {
        const typingNode = showChatTyping();
        try {
            return await action();
        } finally {
            typingNode?.remove();
        }
    };

    const setChatQuickReplies = (options = []) => {
        if (!refs.chatbotQuickReplies) {
            return;
        }

        refs.chatbotQuickReplies.innerHTML = "";

        options.forEach((option) => {
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = option.label;
            button.addEventListener("click", () => {
                appendChatMessage(option.label, "user");
                handleChatAnswer(option.value);
            });
            refs.chatbotQuickReplies.appendChild(button);
        });
    };

    const isPhoneValid = (value) => /^[+]?[0-9\s\-()]{8,20}$/.test(value.trim());
    const isEmailValid = (value) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
    const isDateValid = (value) => /^\d{4}-\d{2}-\d{2}$/.test(value.trim());

    const validationMessages = {
        name: "Please enter your full name.",
        phone: "Please enter a valid phone number.",
        email: "Please enter a valid email address.",
        location: "Please enter your location.",
        domain: "Please select a domain.",
        date: "Please select a date.",
        timeSlot: "Please select a time slot."
    };

    const ensureFieldError = (field) => {
        const label = field.closest("label");
        if (!label) {
            return null;
        }

        let errorNode = label.querySelector(".field-error");
        if (!errorNode) {
            errorNode = document.createElement("small");
            errorNode.className = "field-error";
            errorNode.setAttribute("aria-live", "polite");
            label.appendChild(errorNode);
        }

        return errorNode;
    };

    const getFieldMessage = (field) => {
        const value = field.value.trim();
        if (field.required && !value) {
            return validationMessages[field.name] || "This field is required.";
        }

        if (field.name === "email" && value && !isEmailValid(value)) {
            return validationMessages.email;
        }

        if (field.name === "phone" && value && !isPhoneValid(value)) {
            return validationMessages.phone;
        }

        if (field.name === "date" && value && !isDateValid(value)) {
            return validationMessages.date;
        }

        return "";
    };

    const validateField = (field) => {
        if (!field || field.disabled || field.type === "hidden") {
            return true;
        }

        const message = getFieldMessage(field);
        const errorNode = ensureFieldError(field);
        field.classList.toggle("is-invalid", Boolean(message));
        if (errorNode) {
            errorNode.textContent = message;
        }

        return !message;
    };

    const validateForm = (form) => {
        const fields = Array.from(form.querySelectorAll("input, select, textarea")).filter((field) => field.type !== "hidden");
        let firstInvalid = null;
        let isValid = true;

        fields.forEach((field) => {
            const fieldValid = validateField(field);
            if (!fieldValid && !firstInvalid) {
                firstInvalid = field;
            }

            isValid = isValid && fieldValid;
        });

        if (firstInvalid instanceof HTMLElement) {
            firstInvalid.focus();
        }

        return isValid;
    };

    const clearFormErrors = (form) => {
        if (!form) {
            return;
        }

        form.querySelectorAll("input.is-invalid, select.is-invalid, textarea.is-invalid")
            .forEach((field) => field.classList.remove("is-invalid"));

        form.querySelectorAll(".field-error").forEach((node) => {
            node.textContent = "";
        });
    };

    const setupFormValidation = () => {
        [refs.leadForm, refs.demoForm].forEach((form) => {
            if (!form) {
                return;
            }

            const fields = form.querySelectorAll("input, select, textarea");
            fields.forEach((field) => {
                if (field.type === "hidden") {
                    return;
                }

                field.addEventListener("blur", () => validateField(field));
                field.addEventListener("input", () => {
                    if (field.classList.contains("is-invalid")) {
                        validateField(field);
                    }
                });
                field.addEventListener("change", () => validateField(field));
            });
        });
    };

    const addFieldHelperText = (selector, text) => {
        const field = document.querySelector(selector);
        const label = field?.closest("label");
        if (!label || label.querySelector(".field-helper")) {
            return;
        }

        const helper = document.createElement("small");
        helper.className = "field-helper";
        helper.textContent = text;
        label.appendChild(helper);
    };

    const setupFormHelperText = () => {
        addFieldHelperText('#lead-capture-form [name="phone"]', "Include country code to speed up callback confirmation.");
        addFieldHelperText('#lead-capture-form [name="email"]', "Demo details and reminders will be sent to this address.");
        addFieldHelperText('#demo-booking-form #demo-date', "Pick your preferred date. We'll instantly check slot availability.");
    };

    const getRelativeDate = (days) => {
        const date = new Date();
        date.setDate(date.getDate() + days);
        return date.toISOString().slice(0, 10);
    };

    const setChatPrompt = () => {
        switch (chatbotState.step) {
            case "welcome":
                appendChatMessage("Hi. I'll capture your lead details and help you book a demo.");
                chatbotState.step = "name";
                appendChatMessage("What is your full name?");
                setChatQuickReplies([]);
                break;
            case "phone":
                appendChatMessage("Please share your phone number.");
                break;
            case "email":
                appendChatMessage("What is your email address?");
                break;
            case "location":
                appendChatMessage("Which city are you based in?");
                break;
            case "domain":
                appendChatMessage("Which domain are you interested in?");
                setChatQuickReplies(
                    domainCatalog.slice(0, 5).map((item) => ({ label: item.name, value: item.name }))
                );
                break;
            case "offerDemo":
                appendChatMessage("Would you like to book a demo now?");
                setChatQuickReplies([
                    { label: "Yes, book demo", value: "yes" },
                    { label: "No, later", value: "no" }
                ]);
                break;
            case "demoDate":
                appendChatMessage("Share your preferred demo date in YYYY-MM-DD format.");
                setChatQuickReplies([
                    { label: "Tomorrow", value: getRelativeDate(1) },
                    { label: "Day after tomorrow", value: getRelativeDate(2) }
                ]);
                break;
            case "demoTimeSlot":
                appendChatMessage("Choose a time slot.");
                setChatQuickReplies([
                    { label: "Morning", value: "Morning" },
                    { label: "Afternoon", value: "Afternoon" },
                    { label: "Evening", value: "Evening" }
                ]);
                break;
            default:
                setChatQuickReplies([]);
                break;
        }
    };

    const handleChatAnswer = async (answer) => {
        const value = answer.trim();
        if (!value) {
            return;
        }

        try {
            switch (chatbotState.step) {
                case "name":
                    chatbotState.draft.name = value;
                    chatbotState.step = "phone";
                    setChatPrompt();
                    break;
                case "phone":
                    if (!isPhoneValid(value)) {
                        appendChatMessage("Please enter a valid phone number.", "bot");
                        return;
                    }

                    chatbotState.draft.phone = value;
                    chatbotState.step = "email";
                    setChatPrompt();
                    break;
                case "email":
                    if (!isEmailValid(value)) {
                        appendChatMessage("Please enter a valid email address.", "bot");
                        return;
                    }

                    chatbotState.draft.email = value;
                    chatbotState.step = "location";
                    setChatPrompt();
                    break;
                case "location":
                    chatbotState.draft.location = value;
                    chatbotState.step = "domain";
                    setChatPrompt();
                    break;
                case "domain":
                    chatbotState.draft.domain = value;
                    appendChatMessage("Saving your lead details...", "system");
                    setChatQuickReplies([]);
                    chatbotState.leadId = await runWithTyping(() => submitLead({
                        name: chatbotState.draft.name,
                        phone: chatbotState.draft.phone,
                        email: chatbotState.draft.email,
                        location: chatbotState.draft.location,
                        domain: chatbotState.draft.domain
                    }, "Chatbot"));

                    if (!chatbotState.leadId) {
                        throw new Error("Lead id was not returned.");
                    }

                    window.localStorage.setItem(leadStorageKey, String(chatbotState.leadId));
                    setLeadState(String(chatbotState.leadId));
                    showDemoStep();
                    appendChatMessage(`Lead #${chatbotState.leadId} captured successfully.`, "system");
                    chatbotState.step = "offerDemo";
                    setChatPrompt();
                    break;
                case "offerDemo":
                    if (value.toLowerCase() === "yes") {
                        chatbotState.step = "demoDate";
                        setChatPrompt();
                    } else {
                        appendChatMessage("No problem. Your lead is saved and our team will reach out shortly.", "bot");
                        setChatQuickReplies([]);
                    }
                    break;
                case "demoDate":
                    if (!isDateValid(value)) {
                        appendChatMessage("Please use the YYYY-MM-DD format for the date.", "bot");
                        return;
                    }

                    chatbotState.draft.demoDate = value;
                    chatbotState.step = "demoTimeSlot";
                    setChatPrompt();
                    break;
                case "demoTimeSlot":
                    chatbotState.draft.demoTimeSlot = value;
                    appendChatMessage("Booking your demo slot...", "system");
                    setChatQuickReplies([]);

                    const booking = await runWithTyping(() => submitDemo({
                        leadId: Number(chatbotState.leadId),
                        date: chatbotState.draft.demoDate,
                        timeSlot: chatbotState.draft.demoTimeSlot
                    }));

                    appendChatMessage(`Demo booked successfully. Confirmation: ${booking?.confirmation?.confirmationCode || "Created"}.`, "system");
                    appendChatMessage("You're all set. We'll send the remaining details shortly.", "bot");
                    break;
                default:
                    break;
            }
        } catch (error) {
            appendChatMessage(error.message || "I couldn't process that just now. Please try again shortly.", "bot");
            setChatQuickReplies([]);
        }
    };

    const setChatPanel = (open) => {
        if (!refs.chatbotPanel || !refs.chatbotToggle) {
            return;
        }

        if (open) {
            refs.chatbotPanel.hidden = false;
            refs.chatbotPanel.setAttribute("aria-hidden", "false");
            refs.chatbotToggle.classList.add("is-open");
            window.requestAnimationFrame(() => refs.chatbotPanel?.classList.add("is-open"));
            refs.chatbotInput?.focus();
        } else {
            refs.chatbotPanel.classList.remove("is-open");
            refs.chatbotPanel.setAttribute("aria-hidden", "true");
            refs.chatbotToggle.classList.remove("is-open");
            window.setTimeout(() => {
                if (refs.chatbotPanel) {
                    refs.chatbotPanel.hidden = true;
                }
            }, 220);
        }

        refs.chatbotToggle.setAttribute("aria-expanded", String(open));
    };

    const setupChatbot = () => {
        refs.chatbotToggle?.addEventListener("click", () => {
            const shouldOpen = refs.chatbotPanel?.hidden ?? true;
            setChatPanel(shouldOpen);
        });

        refs.chatbotClose?.addEventListener("click", () => setChatPanel(false));

        refs.chatbotForm?.addEventListener("submit", async (event) => {
            event.preventDefault();
            const value = refs.chatbotInput?.value.trim();
            if (!value) {
                return;
            }

            appendChatMessage(value, "user");
            if (refs.chatbotInput) {
                refs.chatbotInput.value = "";
            }

            await handleChatAnswer(value);
        });

        document.addEventListener("keydown", (event) => {
            if (event.key === "Escape") {
                setChatPanel(false);
                closeMenu();
            }
        });

        document.addEventListener("click", (event) => {
            if (!refs.chatbotPanel || refs.chatbotPanel.hidden) {
                return;
            }

            const target = event.target;
            if (!(target instanceof Element)) {
                return;
            }

            if (!target.closest(".chatbot")) {
                setChatPanel(false);
            }
        });

        setChatPrompt();
    };

    const setupMobileStickyCta = () => {
        let stickyCta = document.querySelector(".mobile-sticky-cta");
        if (!stickyCta) {
            stickyCta = document.createElement("a");
            stickyCta.className = "mobile-sticky-cta btn btn-primary";
            stickyCta.href = "#demo-cta";
            stickyCta.textContent = "Book Demo";
            document.body.appendChild(stickyCta);
        }

        stickyCta.addEventListener("click", (event) => {
            const target = document.getElementById("demo-cta");
            if (!target) {
                return;
            }

            event.preventDefault();
            const top = target.getBoundingClientRect().top + window.scrollY - 78;
            window.scrollTo({ top, behavior: "smooth" });
        });
    };

    setupNavigation();
    animateOnLoad();
    loadStats();
    setupDomainBrowser();
    setupFormHelperText();
    setupFormValidation();
    setupLeadCapture();
    setupDemoBooking();
    setupChatbot();
    setupMobileStickyCta();
})();
