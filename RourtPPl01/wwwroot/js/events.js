(function () {
    function byId(id) { return document.getElementById(id); }

    function updateBlocks() {
        var typeSel = byId("TypeName");
        if (!typeSel) return;
        var v = (typeSel.value || "").toLowerCase();
        var isSurvey = (v === "استبيان" || v === "survey");
        var isDiscussionLike = (v === "نقاش" || v === "ورشة" || v === "workshop" || v === "meeting" || v === "اجتماع");

        var survey = byId("survey-block");
        var purpose = byId("purpose-block");
        if (survey) survey.style.display = isSurvey ? "block" : "none";
        if (purpose) purpose.style.display = isDiscussionLike ? "block" : "none";
    }

    function wireTypeChange() {
        var typeSel = byId("TypeName");
        if (!typeSel) return;
        typeSel.addEventListener("change", updateBlocks);
        updateBlocks();
    }

    function wireOptions() {
        var addBtn = document.getElementById("btn-add-option");
        var wrap = document.getElementById("options-wrapper");
        if (!addBtn || !wrap) return;

        addBtn.addEventListener("click", function () {
            var idx = parseInt(wrap.getAttribute("data-next-index") || "0", 10);
            var prefix = wrap.getAttribute("data-prefix") || "Survey";

            var row = document.createElement("div");
            row.className = "input-group opt-row mt-1";
            row.setAttribute("data-index", idx);
            row.innerHTML =
                '<span class="input-group-text">' + (idx + 1) + '</span>' +
                '<input class="form-control" name="' + prefix + '.Options[' + idx + '].Text" placeholder="نص الخيار" />' +
                '<input type="hidden" name="' + prefix + '.Options[' + idx + '].Order" value="' + (idx + 1) + '"/>' +
                '<button type="button" class="btn btn-outline-danger" onclick="removeOptionRow(this)">حذف</button>';

            wrap.appendChild(row);
            wrap.setAttribute("data-next-index", String(idx + 1));
        });
    }

    window.removeOptionRow = function (btn) {
        var wrap = document.getElementById("options-wrapper");
        if (!wrap) return;
        var row = btn.closest(".opt-row");
        if (!row) return;

        var rows = wrap.querySelectorAll(".opt-row");
        if (rows.length <= 2) { // على الأقل خيارين
            alert("لا يمكن حذف أقل من خيارين.");
            return;
        }

        wrap.removeChild(row);

        // Reindex
        var prefix = wrap.getAttribute("data-prefix") || "Survey";
        rows = wrap.querySelectorAll(".opt-row");
        rows.forEach(function (r, i) {
            r.querySelector(".input-group-text").innerText = (i + 1);
            var text = r.querySelector('input.form-control[name*="Options"]');
            var order = r.querySelector('input[type="hidden"][name*="Options"]');
            if (text) text.name = prefix + '.Options[' + i + '].Text';
            if (order) { order.name = prefix + '.Options[' + i + '].Order'; order.value = String(i + 1); }
        });
        wrap.setAttribute("data-next-index", String(rows.length));
    };

    document.addEventListener("DOMContentLoaded", function () {
        wireTypeChange();
        wireOptions();
    });
})();
