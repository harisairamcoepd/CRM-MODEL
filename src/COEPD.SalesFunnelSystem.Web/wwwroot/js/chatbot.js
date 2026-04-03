(function () {
    const widget = document.getElementById("chatbot-widget");
    const toggle = document.getElementById("chatbot-toggle");
    const panel = document.getElementById("chatbot-panel");
    const close = document.getElementById("chatbot-close");
    const form = document.getElementById("chatbot-form");
    const input = document.getElementById("chatbot-input");
    const messages = document.getElementById("chatbot-messages");
    const quickReplies = document.getElementById("chatbot-quick-replies");
    if (!toggle || !panel || !form || !input || !messages || !quickReplies) return;

    let sessionId = localStorage.getItem("coepd-chat-session") || null;
    let leadId = localStorage.getItem("coepd-lead-id") || null;
    let isOpen = false;
    let greeted = false;

    /* ── Open / Close ── */
    function openChat() {
        isOpen = true;
        widget.classList.add("open");
        panel.classList.remove("chatbot-panel--closed");
        panel.classList.add("chatbot-panel--open");
        dismissGreeting();
        if (!messages.childElementCount) sendMessage("Hi");
        setTimeout(() => input.focus(), 350);
    }

    function closeChat() {
        isOpen = false;
        widget.classList.remove("open");
        panel.classList.remove("chatbot-panel--open");
        panel.classList.add("chatbot-panel--closed");
    }

    toggle.addEventListener("click", () => isOpen ? closeChat() : openChat());
    close?.addEventListener("click", closeChat);

    /* ── Auto Greeting ── */
    function showGreeting() {
        if (greeted || isOpen) return;
        greeted = true;
        const tip = document.createElement("div");
        tip.className = "chatbot-greeting";
        tip.id = "chatbot-greeting";
        tip.innerHTML = `👋 Hi there! Need help choosing a course? <button class="chatbot-greeting-close" aria-label="Dismiss">&times;</button>`;
        tip.addEventListener("click", (e) => {
            if (e.target.classList.contains("chatbot-greeting-close")) { dismissGreeting(); return; }
            dismissGreeting();
            openChat();
        });
        widget.appendChild(tip);
    }

    function dismissGreeting() {
        const g = document.getElementById("chatbot-greeting");
        if (g) g.remove();
    }

    setTimeout(showGreeting, 3000);

    /* ── Messaging ── */
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        const text = input.value.trim();
        if (!text) return;
        input.value = "";
        await sendMessage(text);
    });

    async function sendMessage(message) {
        appendBubble(message, "user");
        showTyping();
        try {
            const response = await fetch("/api/chat", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ sessionId, message, source: "Website" })
            });
            hideTyping();
            if (!response.ok) return appendBubble("Something went wrong. Please try again.", "bot");
            const data = await response.json();
            sessionId = data.sessionId;
            localStorage.setItem("coepd-chat-session", sessionId);
            if (data.leadId) {
                leadId = data.leadId;
                localStorage.setItem("coepd-lead-id", leadId);
            }
            appendBubble(data.reply, "bot");
            renderQuickReplies(data.quickReplies || []);
            if ((data.quickReplies || []).includes("Book Free Demo")) {
                appendBubble("Your lead id is " + (leadId || "generated") + ". Use it in the demo booking form below.", "bot");
            }
        } catch {
            hideTyping();
            appendBubble("Connection error. Please try again.", "bot");
        }
    }

    /* ── Typing Indicator ── */
    function showTyping() {
        hideTyping();
        const wrapper = document.createElement("div");
        wrapper.className = "typing-indicator";
        wrapper.id = "chatbot-typing";
        wrapper.innerHTML =
            '<div class="bubble-bot-avatar"><svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M12 2a5 5 0 015 5v1a5 5 0 01-10 0V7a5 5 0 015-5zm-7 18a7 7 0 0114 0H5z" fill="#fff"/></svg></div>' +
            '<div class="typing-dots"><span></span><span></span><span></span></div>';
        messages.appendChild(wrapper);
        messages.scrollTop = messages.scrollHeight;
    }

    function hideTyping() {
        const t = document.getElementById("chatbot-typing");
        if (t) t.remove();
    }

    /* ── Bubble Renderer ── */
    function appendBubble(text, type) {
        if (type === "bot") {
            const wrapper = document.createElement("div");
            wrapper.className = "bubble-wrapper";
            wrapper.innerHTML =
                '<div class="bubble-bot-avatar"><svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M12 2a5 5 0 015 5v1a5 5 0 01-10 0V7a5 5 0 015-5zm-7 18a7 7 0 0114 0H5z" fill="#fff"/></svg></div>';
            const bubble = document.createElement("div");
            bubble.className = "bubble bot";
            bubble.textContent = text;
            wrapper.appendChild(bubble);
            messages.appendChild(wrapper);
        } else {
            const bubble = document.createElement("div");
            bubble.className = "bubble user";
            bubble.textContent = text;
            messages.appendChild(bubble);
        }
        messages.scrollTop = messages.scrollHeight;
    }

    /* ── Quick Replies ── */
    function renderQuickReplies(items) {
        quickReplies.innerHTML = "";
        items.forEach((item, i) => {
            const chip = document.createElement("button");
            chip.type = "button";
            chip.className = "chip";
            chip.textContent = item;
            chip.style.animationDelay = (i * 0.05) + "s";
            chip.style.animation = "bubbleIn 0.3s cubic-bezier(0.34,1.56,0.64,1) both";
            chip.addEventListener("click", () => sendMessage(item));
            quickReplies.appendChild(chip);
        });
    }
})();
