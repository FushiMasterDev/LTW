// Pointer input is coalesced into one frame; no pointer tracking on touch or dashboard.
(() => {
    'use strict';
    const motion = window.FxMotion;
    if (!motion || !document.body.classList.contains('site-public')) return;
    const fine = matchMedia('(hover: hover) and (pointer: fine)');
    const targets = [...document.querySelectorAll('[data-tilt], .fx-homestay-card')];
    let frame = 0, pending = null;
    const reset = element => {
        element.classList.remove('is-tilting');
        element.style.removeProperty('transform');
        element.style.removeProperty('will-change');
    };
    const apply = () => {
        frame = 0;
        if (!pending || motion.reduced || !fine.matches) return;
        const { element, x, y } = pending;
        const rect = element.getBoundingClientRect();
        const px = Math.max(-1, Math.min(1, (x - rect.left) / rect.width * 2 - 1));
        const py = Math.max(-1, Math.min(1, (y - rect.top) / rect.height * 2 - 1));
        const kind = element.dataset.tilt;
        const max = kind === 'hero' ? 3 : kind === 'area' ? 8 : 4;
        const perspective = getComputedStyle(document.documentElement).getPropertyValue(kind === 'area' ? '--fx-perspective-area' : '--fx-perspective').trim();
        element.style.transform = `perspective(${perspective}) rotateX(${-py * max}deg) rotateY(${px * max}deg)`;
        element.classList.add('is-tilting');
        element.style.willChange = 'transform';
        const sheen = element.querySelector('.fx-area-card__sheen');
        if (sheen) { sheen.style.setProperty('--fx-x', `${(px + 1) * 50}%`); sheen.style.setProperty('--fx-y', `${(py + 1) * 50}%`); }
    };
    for (const element of targets) {
        element.addEventListener('pointermove', event => {
            if (motion.reduced || !fine.matches || event.pointerType === 'touch') return;
            pending = { element, x: event.clientX, y: event.clientY };
            if (!frame) frame = requestAnimationFrame(apply);
        }, { passive: true });
        element.addEventListener('pointerleave', () => {
            pending = null; cancelAnimationFrame(frame); frame = 0; reset(element);
        });
    }
    const clear = () => { pending = null; cancelAnimationFrame(frame); frame = 0; targets.forEach(reset); };
    fine.addEventListener('change', clear);
    document.addEventListener('fx:motionchange', clear);
    document.addEventListener('visibilitychange', () => { if (document.hidden) clear(); });
})();
