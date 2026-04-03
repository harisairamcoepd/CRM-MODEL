/* ============================================
   COEPD CRM Dashboard - Premium Interactions
   ============================================ */

(function () {
    'use strict';

    // ---- Toast Notification System ----
    function showToast(message, type = 'success') {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.style.cssText = 'position:fixed;top:1.25rem;right:1.25rem;z-index:9999;display:flex;flex-direction:column;gap:0.5rem;pointer-events:none;';
            document.body.appendChild(container);
        }
        const toast = document.createElement('div');
        const icons = { success: 'fa-check-circle', error: 'fa-exclamation-circle', info: 'fa-info-circle' };
        const colors = { success: '#10B981', error: '#EF4444', info: '#2563EB' };
        toast.innerHTML = `<i class="fas ${icons[type] || icons.info}" style="color:${colors[type]}"></i> <span>${message}</span>`;
        toast.style.cssText = `
            display:flex;align-items:center;gap:0.625rem;padding:0.875rem 1.25rem;
            background:rgba(255,255,255,0.97);backdrop-filter:blur(16px);-webkit-backdrop-filter:blur(16px);
            border-radius:12px;box-shadow:0 8px 32px rgba(0,0,0,0.1),0 0 0 1px rgba(226,232,240,0.6);
            font-size:0.8125rem;font-weight:600;color:#0F172A;font-family:'Inter',sans-serif;
            pointer-events:auto;transform:translateX(120%);transition:transform 0.45s cubic-bezier(0.34,1.56,0.64,1),opacity 0.3s ease;
        `;
        container.appendChild(toast);
        requestAnimationFrame(() => { toast.style.transform = 'translateX(0)'; });
        setTimeout(() => {
            toast.style.transform = 'translateX(120%)';
            toast.style.opacity = '0';
            setTimeout(() => toast.remove(), 400);
        }, 3000);
    }

    // ---- Sidebar Collapse (Desktop) ----
    const sidebar = document.getElementById('sidebar');
    const collapseBtn = document.getElementById('sidebar-collapse');

    if (collapseBtn) {
        collapseBtn.addEventListener('click', () => {
            sidebar.classList.toggle('collapsed');
            localStorage.setItem('sidebar-collapsed', sidebar.classList.contains('collapsed'));
        });

        if (localStorage.getItem('sidebar-collapsed') === 'true') {
            sidebar.classList.add('collapsed');
        }
    }

    // ---- Sidebar Mobile Toggle ----
    const mobileBtn = document.getElementById('sidebar-mobile-btn');

    if (mobileBtn) {
        mobileBtn.addEventListener('click', () => {
            sidebar.classList.toggle('mobile-open');
        });
    }

    document.addEventListener('click', (e) => {
        if (
            sidebar.classList.contains('mobile-open') &&
            !sidebar.contains(e.target) &&
            e.target !== mobileBtn
        ) {
            sidebar.classList.remove('mobile-open');
        }
    });

    // ---- Search Interactivity ----
    const searchInput = document.querySelector('.topbar-search input');
    const tableRows = document.querySelectorAll('.lead-table-card tbody tr');

    if (searchInput) {
        // Typing indicator
        let searchTimeout;
        searchInput.addEventListener('input', () => {
            const q = searchInput.value.toLowerCase().trim();
            const wrap = searchInput.closest('.topbar-search');
            
            // Add searching state
            if (wrap) wrap.classList.add('searching');
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                if (wrap) wrap.classList.remove('searching');
            }, 300);

            tableRows.forEach(row => {
                const text = row.textContent.toLowerCase();
                const match = text.includes(q);
                row.style.display = match ? '' : 'none';
                // Flash matching rows
                if (match && q.length > 0) {
                    row.style.animation = 'none';
                    requestAnimationFrame(() => { row.style.animation = 'rowFlash 0.4s ease'; });
                }
            });
        });

        document.addEventListener('keydown', (e) => {
            if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
                e.preventDefault();
                searchInput.focus();
                const wrap = searchInput.closest('.topbar-search');
                if (wrap) {
                    wrap.style.animation = 'searchPop 0.3s cubic-bezier(0.34,1.56,0.64,1)';
                    setTimeout(() => { wrap.style.animation = ''; }, 300);
                }
            }
        });
    }

    // ---- Filter Selects ----
    const filterSelects = document.querySelectorAll('.filter-select');

    filterSelects.forEach(sel => {
        sel.addEventListener('change', () => {
            const domainSel = filterSelects[0];
            const statusSel = filterSelects[1];
            const domainVal = domainSel ? domainSel.value : '';
            const statusVal = statusSel ? statusSel.value : '';

            tableRows.forEach(row => {
                const domainPill = row.querySelector('.domain-pill');
                const statusPill = row.querySelector('.status-pill');
                const domainText = domainPill ? domainPill.textContent.trim() : '';
                const statusText = statusPill ? statusPill.textContent.trim() : '';

                const showDomain = domainVal.startsWith('All') || domainText.toLowerCase().includes(domainVal.toLowerCase());
                const showStatus = statusVal.startsWith('All') || statusText.toLowerCase().includes(statusVal.toLowerCase());

                row.style.display = (showDomain && showStatus) ? '' : 'none';
            });
            showToast('Filters applied', 'info');
        });
    });

    // ---- Stat Counter Animation ----
    const statValues = document.querySelectorAll('.stat-value[data-value]');

    function easeOutCubic(t) { return 1 - Math.pow(1 - t, 3); }

    function animateValue(el) {
        const raw = el.getAttribute('data-value');
        const isDecimal = raw.includes('.');
        const target = parseFloat(raw);
        const suffix = el.textContent.replace(/[\d,.\s]/g, '').trim();
        const duration = 1500;
        const start = performance.now();

        function tick(now) {
            const elapsed = now - start;
            const progress = Math.min(elapsed / duration, 1);
            const eased = easeOutCubic(progress);
            const current = eased * target;

            if (isDecimal) {
                el.textContent = current.toFixed(1) + suffix;
            } else {
                el.textContent = Math.round(current).toLocaleString() + suffix;
            }

            if (progress < 1) requestAnimationFrame(tick);
        }

        requestAnimationFrame(tick);
    }

    const statObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                const card = entry.target.closest('.stat-card');
                if (card) {
                    setTimeout(() => { card.classList.add('revealed'); }, index * 100);
                }
                animateValue(entry.target);
                statObserver.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    statValues.forEach(sv => statObserver.observe(sv));

    const cardObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                const index = Array.from(document.querySelectorAll('.card, .stat-card')).indexOf(entry.target);
                setTimeout(() => { 
                    entry.target.classList.add('revealed'); 
                }, index * 100);
                cardObserver.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll('.card, .stat-card').forEach(c => cardObserver.observe(c));

    // ---- Status Pill Click-to-Cycle ----
    const statusOrder = ['new', 'contacted', 'demo', 'converted'];
    const statusLabels = { new: 'New', contacted: 'Contacted', demo: 'Demo Booked', converted: 'Converted' };

    document.querySelectorAll('.status-pill').forEach(pill => {
        pill.style.cursor = 'pointer';
        pill.title = 'Click to change status';

        pill.addEventListener('click', (e) => {
            e.stopPropagation();
            const currentClass = statusOrder.find(s => pill.classList.contains(s));
            if (!currentClass) return;

            const nextIdx = (statusOrder.indexOf(currentClass) + 1) % statusOrder.length;
            const nextClass = statusOrder[nextIdx];

            // Animate the status change
            pill.style.transform = 'scale(0.8)';
            pill.style.opacity = '0.5';
            setTimeout(() => {
                pill.classList.remove(currentClass);
                pill.classList.add(nextClass);
                pill.textContent = statusLabels[nextClass];
                pill.style.transform = 'scale(1.15)';
                pill.style.opacity = '1';
                setTimeout(() => { pill.style.transform = 'scale(1)'; }, 150);
            }, 150);

            showToast(`Status updated to ${statusLabels[nextClass]}`, 'success');
        });
    });

    // ---- Lead Drawer ----
    const drawer = document.getElementById('lead-drawer');
    const drawerOverlay = document.getElementById('drawer-overlay');
    const closeDrawerBtn = document.getElementById('close-drawer');
    const drawerName = document.getElementById('drawer-name');
    const drawerEmail = document.getElementById('drawer-email');
    const drawerAvatar = drawer ? drawer.querySelector('.drawer-avatar') : null;

    function openDrawer(leadData) {
        if (!drawer) return;

        if (leadData) {
            if (drawerName) drawerName.textContent = leadData.name;
            if (drawerEmail) drawerEmail.textContent = leadData.email;
            if (drawerAvatar) drawerAvatar.textContent = leadData.initials;
        }

        drawer.classList.add('open');
        if (drawerOverlay) drawerOverlay.classList.add('active');
        document.body.style.overflow = 'hidden';

        // Stagger animate drawer content
        setTimeout(() => {
            drawer.querySelectorAll('.drawer-profile > *, .info-item, .timeline-item').forEach((el, i) => {
                el.style.opacity = '0';
                el.style.transform = 'translateY(12px)';
                el.style.transition = 'none';
                setTimeout(() => {
                    el.style.transition = 'all 0.4s cubic-bezier(0.34, 1.56, 0.64, 1)';
                    el.style.opacity = '1';
                    el.style.transform = 'translateY(0)';
                }, 60 + i * 40);
            });
        }, 100);
    }

    function closeDrawer() {
        if (!drawer) return;
        drawer.classList.remove('open');
        if (drawerOverlay) drawerOverlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    if (closeDrawerBtn) closeDrawerBtn.addEventListener('click', closeDrawer);
    if (drawerOverlay) drawerOverlay.addEventListener('click', closeDrawer);

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') closeDrawer();
    });

    // Lead name click → open drawer
    document.querySelectorAll('.lead-name').forEach(nameEl => {
        nameEl.addEventListener('click', () => {
            const row = nameEl.closest('tr');
            if (!row) return;
            openDrawer({
                name: nameEl.textContent.trim(),
                email: (row.querySelector('.lead-email') || {}).textContent?.trim() || '',
                initials: (row.querySelector('.avatar-sm') || {}).textContent?.trim() || ''
            });
        });
    });

    // Arrow button click → open drawer
    document.querySelectorAll('.btn-icon-table').forEach(btn => {
        btn.addEventListener('click', () => {
            const row = btn.closest('tr');
            if (!row) return;
            openDrawer({
                name: (row.querySelector('.lead-name') || {}).textContent?.trim() || '',
                email: (row.querySelector('.lead-email') || {}).textContent?.trim() || '',
                initials: (row.querySelector('.avatar-sm') || {}).textContent?.trim() || ''
            });
        });
    });

    // ---- Domain Bar Animation ----
    const domainBars = document.querySelectorAll('.domain-bar-fill');
    const barObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const target = entry.target;
                const width = target.style.width;
                target.style.width = '0%';
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => { target.style.width = width; });
                });
                barObserver.unobserve(target);
            }
        });
    }, { threshold: 0.2 });

    domainBars.forEach(bar => barObserver.observe(bar));

    // ---- Funnel Hover Lift ----
    document.querySelectorAll('.funnel-step').forEach(step => {
        step.addEventListener('mouseenter', () => {
            step.style.transform = 'scale(1.02) translateY(-2px)';
            step.style.transition = 'transform 0.35s cubic-bezier(0.34, 1.56, 0.64, 1)';
        });
        step.addEventListener('mouseleave', () => {
            step.style.transform = '';
        });
    });

    // ---- Ripple Effect on Buttons ----
    document.querySelectorAll('.btn-primary, .page-btn, .btn-icon-table, .btn-outline, .nav-item').forEach(btn => {
        btn.style.position = 'relative';
        btn.style.overflow = 'hidden';
        btn.addEventListener('click', function (e) {
            const ripple = document.createElement('span');
            const rect = this.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            ripple.style.cssText = `
                position:absolute; border-radius:50%; pointer-events:none;
                width:${size}px; height:${size}px;
                left:${e.clientX - rect.left - size / 2}px;
                top:${e.clientY - rect.top - size / 2}px;
                background:rgba(255,255,255,0.3);
                transform:scale(0); opacity:1;
                animation:ripple-wave 0.55s ease-out forwards;
            `;
            this.appendChild(ripple);
            setTimeout(() => ripple.remove(), 600);
        });
    });

    // ---- Checkbox Select All with animation ----
    const selectAllCheckbox = document.querySelector('thead .checkbox');
    const rowCheckboxes = document.querySelectorAll('tbody .checkbox');

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', () => {
            rowCheckboxes.forEach((cb, i) => {
                setTimeout(() => {
                    cb.checked = selectAllCheckbox.checked;
                    const row = cb.closest('tr');
                    if (row) {
                        row.style.background = selectAllCheckbox.checked ? 'rgba(37, 99, 235, 0.03)' : '';
                    }
                }, i * 30);
            });
        });
    }

    // ---- Nav Active State ----
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', (e) => {
            e.preventDefault();
            document.querySelectorAll('.nav-item').forEach(i => i.classList.remove('active'));
            item.classList.add('active');
        });
    });

    // ---- Keyboard navigation for table rows ----
    let focusedRow = -1;
    const allRows = Array.from(document.querySelectorAll('.lead-table-card tbody tr'));
    document.addEventListener('keydown', (e) => {
        if (document.activeElement === searchInput && (e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
            e.preventDefault();
            const visible = allRows.filter(r => r.style.display !== 'none');
            if (e.key === 'ArrowDown') focusedRow = Math.min(focusedRow + 1, visible.length - 1);
            if (e.key === 'ArrowUp') focusedRow = Math.max(focusedRow - 1, 0);
            visible.forEach((r, i) => r.style.background = i === focusedRow ? 'rgba(37, 99, 235, 0.04)' : '');
            if (visible[focusedRow]) visible[focusedRow].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            if (e.key === 'Enter' && visible[focusedRow]) {
                const nameEl = visible[focusedRow].querySelector('.lead-name');
                if (nameEl) nameEl.click();
            }
        }
    });

    // ---- Skeleton Loading System ----
    function showSkeleton(container, count, type) {
        container.querySelectorAll('.skeleton-item').forEach(s => s.remove());
        for (let i = 0; i < count; i++) {
            const skel = document.createElement('div');
            skel.className = 'skeleton-item';
            if (type === 'stat') {
                skel.innerHTML = `
                    <div class="skeleton-line" style="width:40px;height:40px;border-radius:8px;margin-bottom:12px"></div>
                    <div class="skeleton-line" style="width:60%;height:24px;margin-bottom:6px"></div>
                    <div class="skeleton-line" style="width:40%;height:14px"></div>
                `;
                skel.style.cssText = 'padding:1.25rem;border-radius:16px;background:white;border:1px solid rgba(226,232,240,0.6);';
            } else if (type === 'row') {
                skel.innerHTML = `
                    <div style="display:flex;align-items:center;gap:12px;padding:0.875rem 1.5rem;">
                        <div class="skeleton-line" style="width:36px;height:36px;border-radius:8px;flex-shrink:0"></div>
                        <div style="flex:1"><div class="skeleton-line" style="width:60%;height:14px;margin-bottom:6px"></div><div class="skeleton-line" style="width:40%;height:10px"></div></div>
                        <div class="skeleton-line" style="width:60px;height:24px;border-radius:20px"></div>
                    </div>
                `;
                skel.style.cssText = 'border-bottom:1px solid #F1F5F9;';
            }
            container.appendChild(skel);
        }
    }

    function hideSkeleton(container) {
        container.querySelectorAll('.skeleton-item').forEach(s => {
            s.style.opacity = '0';
            s.style.transform = 'scale(0.97)';
            setTimeout(() => s.remove(), 300);
        });
    }

    // ---- Notification bell animation ----
    const bellBtn = document.querySelector('.btn-icon-top[title="Notifications"]');
    if (bellBtn) {

    // ---- Initial Skeleton on Page Load ----
    (function initSkeletons() {
        const statsGrid = document.querySelector('.stats-grid');
        if (statsGrid && statsGrid.querySelectorAll('.stat-card').length) {
            const statCards = statsGrid.querySelectorAll('.stat-card');
            statCards.forEach(card => {
                card.style.opacity = '0';
                card.style.transform = 'translateY(20px)';
            });
        }
        const tableBody = document.querySelector('.lead-table-card tbody');
        if (tableBody && !tableBody.querySelector('.skeleton-item')) {
            showSkeleton(tableBody, 5, 'row');
            setTimeout(() => hideSkeleton(tableBody), 1200);
        }
    })();
        bellBtn.addEventListener('click', () => {
            const icon = bellBtn.querySelector('i');
            if (icon) {
                icon.style.animation = 'bellShake 0.5s ease';
                setTimeout(() => { icon.style.animation = ''; }, 500);
            }
            const dot = bellBtn.querySelector('.notification-dot');
            if (dot) {
                dot.style.transition = 'all 0.3s ease';
                dot.style.transform = 'scale(0)';
                setTimeout(() => dot.remove(), 300);
            }
            showToast('No new notifications', 'info');
        });
    }

    // ---- "New Lead" button feedback ----
    const newLeadBtn = document.querySelector('.topbar-actions .btn-primary');
    if (newLeadBtn) {
        newLeadBtn.addEventListener('click', () => {
            showToast('Lead creation form would open here', 'info');
        });
    }

    // ---- Export button feedback ----
    const exportBtn = document.querySelector('.header-filters .btn-outline');
    if (exportBtn) {
        exportBtn.addEventListener('click', () => {
            const icon = exportBtn.querySelector('i');
            if (icon) {
                icon.className = 'fas fa-spinner fa-spin';
                exportBtn.disabled = true;
                exportBtn.style.opacity = '0.7';
                setTimeout(() => {
                    icon.className = 'fas fa-check';
                    icon.style.color = '#10B981';
                    exportBtn.disabled = false;
                    exportBtn.style.opacity = '1';
                    showToast('Export ready — CSV downloaded', 'success');
                    setTimeout(() => {
                        icon.className = 'fas fa-cloud-download-alt';
                        icon.style.color = '';
                    }, 1500);
                }, 1800);
            }
        });
    }

    // ---- Inject micro-interaction keyframes ----
    if (!document.getElementById('micro-style')) {
        const style = document.createElement('style');
        style.id = 'micro-style';
        style.textContent = `
            @keyframes ripple-wave{to{transform:scale(2.5);opacity:0;}}
            @keyframes bellShake{0%{transform:rotate(0)}15%{transform:rotate(14deg)}30%{transform:rotate(-12deg)}45%{transform:rotate(8deg)}60%{transform:rotate(-4deg)}75%{transform:rotate(2deg)}100%{transform:rotate(0)}}
            @keyframes rowFlash{0%{background:rgba(37,99,235,0.06)}100%{background:transparent}}
            @keyframes searchPop{0%{transform:scale(1)}50%{transform:scale(1.02)}100%{transform:scale(1)}}
            @keyframes skeletonPulse{0%{background-position:200% 0}100%{background-position:-200% 0}}
            .skeleton-line{background:linear-gradient(90deg,#F1F5F9 25%,#E2E8F0 50%,#F1F5F9 75%);background-size:200% 100%;animation:skeletonPulse 1.8s ease-in-out infinite;border-radius:6px;}
            .topbar-search.searching{box-shadow:0 0 0 3px rgba(37,99,235,0.08);}
            .topbar-search.searching i{animation:searchPop 0.3s ease;}
        `;
        document.head.appendChild(style);
    }

})();
