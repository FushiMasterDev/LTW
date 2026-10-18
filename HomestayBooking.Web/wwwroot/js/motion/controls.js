// Form focus indicator and room choice are enhancements to the existing GET/booking links.
(() => {
    'use strict';
    const motion = window.FxMotion;
    document.querySelectorAll('.fx-search').forEach(form => {
        const indicator = form.querySelector('.fx-search__indicator');
        if (!indicator) return;
        let frame = 0;
        const move = () => {
            frame = 0;
            const field = document.activeElement.closest('.fx-search > div');
            if (!field || !form.contains(field)) return;
            const parent = form.getBoundingClientRect(), box = field.getBoundingClientRect();
            // Dimensions snap to the current field; only the position is transitioned.
            indicator.style.width = `${box.width}px`;
            indicator.style.height = `${box.height}px`;
            indicator.style.transform = `translate3d(${box.left - parent.left}px,${box.top - parent.top}px,0)`;
            indicator.classList.add('is-positioned');
        };
        const schedule = () => { if (!frame) frame = requestAnimationFrame(move); };
        form.addEventListener('focusin', schedule);
        addEventListener('resize', schedule, { passive: true });
    });
    document.addEventListener('click', event => {
        const button = event.target.closest('[data-room-select]');
        if (!button) return;
        const card = button.closest('.stay-room');
        if (!card) return;
        card.parentElement.querySelectorAll('.stay-room').forEach(room => {
            const selected = room === card;
            room.classList.toggle('is-selected', selected);
            room.querySelector('[data-room-select]')?.setAttribute('aria-pressed', String(selected));
        });
    });
})();
