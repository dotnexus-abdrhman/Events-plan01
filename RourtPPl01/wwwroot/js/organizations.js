// بحث فوري (debounce)
(function () {
    const form = document.getElementById("orgSearchForm");
    const q = document.getElementById("q");
    if (form && q) {
        let t;
        q.addEventListener("input", () => {
            clearTimeout(t);
            t = setTimeout(() => form.submit(), 500);
        });
    }

    // تأكيد حذف
    document.querySelectorAll(".js-del").forEach(btn => {
        btn.addEventListener("click", (e) => {
            e.preventDefault();
            const id = btn.getAttribute("data-id");
            const name = btn.getAttribute("data-name") || "";
            if (!id) return;

            const ok = confirm(`هل تريد حذف الجهة: "${name}" ؟`);
            if (!ok) return;

            // إنشاء فورم POST وحذف
            const form = document.createElement("form");
            form.method = "post";
            form.action = `/Organizations/Delete/${id}`;
            const token = document.querySelector("input[name='__RequestVerificationToken']");
            if (token) {
                const clone = token.cloneNode();
                clone.value = token.value;
                form.appendChild(clone);
            }
            document.body.appendChild(form);
            form.submit();
        });
    });

    // عدّادات بسيطة للبطاقات (لو حبيت تعدّ بطاقات لاحقًا)
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

    // مزامنة ألوان (Color inputs) + معاينة شعار
    const pcPick = document.getElementById("pcPick");
    const pcText = document.getElementById("pcText");
    if (pcPick && pcText) {
        pcPick.addEventListener("input", () => pcText.value = pcPick.value);
        pcText.addEventListener("input", () => pcPick.value = pcText.value);
    }
    const scPick = document.getElementById("scPick");
    const scText = document.getElementById("scText");
    if (scPick && scText) {
        scPick.addEventListener("input", () => scText.value = scPick.value);
        scText.addEventListener("input", () => scPick.value = scText.value);
    }

    const logoUrl = document.getElementById("logoUrl");
    const logoPreview = document.getElementById("logoPreview");
    if (logoUrl && logoPreview) {
        const showPreview = () => {
            const url = logoUrl.value?.trim();
            if (!url) { logoPreview.style.display = "none"; return; }
            logoPreview.src = url;
            logoPreview.style.display = "block";
        };
        logoUrl.addEventListener("change", showPreview);
        logoUrl.addEventListener("blur", showPreview);
        showPreview();
    }
})();
