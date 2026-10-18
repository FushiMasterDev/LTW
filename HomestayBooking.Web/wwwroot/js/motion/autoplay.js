// One visibility observer (FxMotion) and one clock for every public autoplay block.
(() => {
    'use strict';
    const motion = window.FxMotion;
    if (!motion) return;
    const blocks = new Map();
    let frame = 0, previous = 0;
    const fine = matchMedia('(hover: hover) and (pointer: fine)');
    const tick = now => {
        frame = 0;
        const elapsed = previous ? Math.min(now - previous, 100) : 0;
        previous = now;
        for (const state of blocks.values()) {
            if (!state.running || !state.advance) continue;
            state.elapsed += elapsed;
            if (state.elapsed >= motion.duration('--fx-dwell')) {
                state.elapsed = 0;
                state.advance(1);
            }
        }
        if ([...blocks.values()].some(s => s.running && s.advance)) frame = requestAnimationFrame(tick);
        else previous = 0;
    };
    const reconcile = () => {
        let count = 0;
        for (const [root, state] of blocks) {
            const running = !motion.reduced && !document.hidden && state.visible && !state.paused && !state.hover && !state.focus && count < 2;
            if (running) count++;
            root.classList.toggle('is-running', running);
            root.classList.toggle('is-enhanced', !motion.reduced);
            if (state.running !== running) state.onState?.(running);
            state.running = running;
        }
        if (!frame && [...blocks.values()].some(s => s.running && s.advance)) frame = requestAnimationFrame(tick);
        if (![...blocks.values()].some(s => s.running && s.advance)) { cancelAnimationFrame(frame); frame = 0; previous = 0; }
    };
    const register = (root, options = {}) => {
        if (blocks.has(root)) return blocks.get(root).control;
        const state = { visible: false, hover: false, focus: root.contains(document.activeElement), paused: false, running: false, elapsed: 0, ...options };
        const control = { reset: () => { state.elapsed = 0; }, get running() { return state.running; } };
        state.control = control;
        blocks.set(root, state);
        root.classList.add('has-autoplay');
        root.addEventListener('mouseenter', () => { state.hover = fine.matches; reconcile(); });
        root.addEventListener('mouseleave', () => { state.hover = false; reconcile(); });
        root.addEventListener('focusin', () => { state.focus = true; reconcile(); });
        root.addEventListener('focusout', () => queueMicrotask(() => { state.focus = root.contains(document.activeElement); reconcile(); }));
        const button = root.querySelector('[data-motion-toggle]');
        button?.addEventListener('click', () => {
            state.paused = !state.paused;
            const label = state.paused ? button.dataset.resume : button.dataset.pause;
            button.setAttribute('aria-pressed', String(state.paused));
            button.setAttribute('aria-label', label);
            button.querySelector('[data-motion-label]').textContent = label;
            reconcile();
        });
        motion.observe(root, entry => { state.visible = entry.isIntersecting; reconcile(); });
        reconcile();
        return control;
    };
    window.FxAutoplay = { register };
    document.addEventListener('visibilitychange', reconcile);
    document.addEventListener('fx:motionchange', reconcile);
    fine.addEventListener('change', () => { for (const s of blocks.values()) s.hover = false; reconcile(); });
    document.querySelectorAll('[data-autoplay="ambient"]').forEach(root => register(root));
})();
