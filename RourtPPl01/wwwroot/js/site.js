(function () {
    // Theme toggle with localStorage
    const body = document.body;
    const key = "app-theme";
    const saved = localStorage.getItem(key);
    if (saved === "dark") body.classList.add("theme-dark");

    const btn = document.getElementById("themeToggle");
    if (btn) {
        btn.addEventListener("click", () => {
            body.classList.toggle("theme-dark");
            localStorage.setItem(key, body.classList.contains("theme-dark") ? "dark" : "light");
        });
    }

    // Auto-init Bootstrap toasts (if not visible)
    const toastEls = document.querySelectorAll(".toast:not(.show)");
    toastEls.forEach(el => {
        try {
            const t = new bootstrap.Toast(el, { delay: 3500 });
            t.show();
        } catch { /* ignore */ }
    });
})();
