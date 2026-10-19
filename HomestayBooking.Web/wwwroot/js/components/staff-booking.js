// An edited lookup invalidates its displayed confirmation; booking still rechecks price and inventory on the server.
(() => {
    const form = document.querySelector('[data-staff-quote-form]');
    const confirmation = document.querySelector('[data-staff-confirmation]');
    if (!form || !confirmation) return;
    const invalidate = () => {
        confirmation.hidden = true;
        document.querySelector('[data-staff-quote-changed]').hidden = false;
    };
    form.addEventListener('input', invalidate);
    form.addEventListener('change', invalidate);
})();
