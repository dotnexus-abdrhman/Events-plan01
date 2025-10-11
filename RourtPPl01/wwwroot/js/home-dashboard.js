document.addEventListener("DOMContentLoaded", function () {
    // عدادات
    document.querySelectorAll(".count").forEach(el => {
        const target = parseInt(el.getAttribute("data-target") || "0", 10);
        let current = 0;
        const step = Math.max(1, Math.ceil(target / 40));
        const timer = setInterval(() => {
            current += step;
            if (current >= target) { current = target; clearInterval(timer); }
            el.textContent = current.toString();
        }, 30);
    });

    // Chart.js
    const ctx = document.getElementById("homeChart");
    if (ctx && window.Chart) {
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو"],
                datasets: [{ label: "نمو افتراضي", data: [3, 5, 4, 7, 8, 12], tension: .3, borderWidth: 2, fill: false }]
            },
            options: {
                responsive: true,
                plugins: { legend: { display: true } },
                scales: { x: { grid: { display: false } }, y: { beginAtZero: true, grid: { color: "#eee" } } }
            }
        });
    }
});
