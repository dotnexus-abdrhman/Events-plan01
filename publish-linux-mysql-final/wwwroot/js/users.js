(function () {
    // إغلاق التنبيهات تلقائياً بعد 4 ثوانٍ
    const alerts = document.querySelectorAll('.alert');
    if (alerts.length) {
        setTimeout(() => alerts.forEach(a => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(a);
            bsAlert.close();
        }), 4000);
    }

    // تأكيد الحذف في روابط/أزرار لها الكلاس js-delete
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.js-delete');
        if (!btn) return;
        if (!confirm('هل أنت متأكد من الحذف؟')) {
            e.preventDefault();
        }
    });
})();
