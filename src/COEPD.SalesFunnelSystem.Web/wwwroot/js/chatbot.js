(function () {
    const toggle = document.getElementById("chatbot-toggle");
    const panel = document.getElementById("chatbot-panel");
    const close = document.getElementById("chatbot-close");
    const form = document.getElementById("chat-input-form");
    const messages = document.getElementById("chat-messages");
    const input = form?.querySelector("input");

    if (!toggle || !panel || !form || !messages || !input) return;

    let isOpen = false;

    const toggleChat = () => {
        isOpen = !isOpen;
        panel.classList.toggle("active", isOpen);
        if (isOpen) {
            setTimeout(() => input.focus(), 300);
        }
    };

    toggle.addEventListener("click", toggleChat);
    close?.addEventListener("click", () => {
        isOpen = false;
        panel.classList.remove("active");
    });

    const appendMessage = (text, role) => {
        const div = document.createElement("div");
        div.className = `msg is-${role}`;
        div.textContent = text;
        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
    };

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const text = input.value.trim();
        if (!text) return;

        appendMessage(text, "user");
        input.value = "";

        // Add typing indicator
        const typing = document.createElement("div");
        typing.className = "msg is-bot typing";
        typing.innerHTML = '<i class="ph ph-dots-three-outline ph-fill"></i>';
        messages.appendChild(typing);
        messages.scrollTop = messages.scrollHeight;

        try {
            const response = await fetch("/api/chat", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ message: text, source: "LandingPage" })
            });

            typing.remove();

            if (response.ok) {
                const data = await response.json();
                appendMessage(data.reply || "I'm here to help with your admissions journey.", "bot");
            } else {
                appendMessage("I couldn't reach the advisor. Please try again or book a demo directly.", "bot");
            }
        } catch (err) {
            typing.remove();
            appendMessage("Something went wrong. Let me try again in a bit.", "bot");
        }
    });

    // Handle floating chat triggers
    document.addEventListener("coepd:open-chat", (e) => {
        if (!isOpen) toggleChat();
        if (e.detail?.message) {
            input.value = e.detail.message;
            input.focus();
        }
    });
})();
