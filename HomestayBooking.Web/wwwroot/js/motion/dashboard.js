// Data stays in the HTML. The count overlay is aria-hidden and ends at the exact server string.
(() => {
    'use strict';
    const motion = window.FxMotion;
    if (!motion) return;
    const counters = new Map();
    const finish = element => {
        const state = counters.get(element);
        if (!state) return;
        cancelAnimationFrame(state.frame);
        element.textContent = state.final;
        counters.delete(element);
    };
    document.querySelectorAll('.fx-stat__value').forEach(element => {
        const final = element.textContent.trim();
        const match = final.match(/^([^\d]*)([\d.,\s]+)([^\d]*)$/);
        if (!match) return;
        const vi = document.documentElement.lang.startsWith('vi');
        const numeric = match[2].replace(/\s/g, '');
        const decimal = vi ? numeric.includes(',') : numeric.includes('.');
        const value = Number(vi ? numeric.replace(/\./g, '').replace(',', '.') : numeric.replace(/,/g, ''));
        if (!Number.isFinite(value) || value <= 0) return;
        const formatter = new Intl.NumberFormat(vi ? 'vi-VN' : 'en-US', { maximumFractionDigits: decimal ? 1 : 0 });
        motion.observe(element, entry => {
            if (!entry.isIntersecting || motion.reduced) return;
            const semantic = document.createElement('span'), visual = document.createElement('span');
            semantic.textContent = final; semantic.className = 'fx-count-final';
            visual.setAttribute('aria-hidden', 'true'); visual.className = 'fx-count-visual';
            element.replaceChildren(semantic, visual);
            const state = { final, frame: 0 }, start = performance.now(), duration = motion.duration('--fx-dur-5');
            counters.set(element, state);
            const tick = now => {
                const p = Math.min(1, (now - start) / duration);
                visual.textContent = match[1] + formatter.format(value * (1 - Math.pow(1 - p, 3))) + match[3];
                if (p < 1 && !motion.reduced && !document.hidden) state.frame = requestAnimationFrame(tick);
                else finish(element);
            };
            state.frame = requestAnimationFrame(tick);
        }, true);
    });
    document.addEventListener('fx:motionchange', () => { if (motion.reduced) [...counters.keys()].forEach(finish); });
    document.addEventListener('visibilitychange', () => { if (document.hidden) [...counters.keys()].forEach(finish); });
    // Store the old active link rectangle within the sidebar's scrolling content, then animate a single pill.
    const nav = document.querySelector('.app-sidebar-nav');
    if (nav) {
        const pill = document.createElement('span'); pill.className = 'fx-sidebar-pill'; pill.setAttribute('aria-hidden', 'true'); nav.prepend(pill);
        const geometry = link => ({ x: link.offsetLeft, y: link.offsetTop, w: link.offsetWidth, h: link.offsetHeight });
        const transform = box => `translate3d(${box.x}px,${box.y}px,0)`;
        const active = nav.querySelector('.sidebar-link.active');
        if (active) {
            const target = geometry(active); pill.style.width = `${target.w}px`; pill.style.height = `${target.h}px`; pill.style.transform = transform(target); nav.classList.add('has-motion-pill');
            let previous = null;
            try { previous = JSON.parse(sessionStorage.getItem('fx-sidebar-from')); sessionStorage.removeItem('fx-sidebar-from'); } catch (_) { }
            if (previous && Number.isFinite(previous.y) && !motion.reduced) pill.animate([{ transform: transform(previous) }, { transform: transform(target) }], { duration: motion.duration('--fx-dur-3'), easing: getComputedStyle(document.documentElement).getPropertyValue('--fx-ease-out').trim() });
        }
        nav.addEventListener('click', event => {
            const link = event.target.closest('.sidebar-link');
            if (!link || !active || event.metaKey || event.ctrlKey) return;
            try { sessionStorage.setItem('fx-sidebar-from', JSON.stringify(geometry(active))); } catch (_) { }
        });
        addEventListener('resize', () => { if (active) { const box = geometry(active); pill.style.width = `${box.w}px`; pill.style.height = `${box.h}px`; pill.style.transform = transform(box); } }, { passive: true });
        document.querySelector('[data-sidebar-collapse]')?.addEventListener('click', () => { if (active) requestAnimationFrame(() => { const box = geometry(active); pill.style.width = `${box.w}px`; pill.style.height = `${box.h}px`; pill.style.transform = transform(box); }); });
    }
    // Chart.js is optional today; configure its defaults before future page-specific chart construction.
    if (window.Chart) window.Chart.defaults.animation = motion.reduced ? false : { duration: motion.duration('--fx-dur-5') };
    document.addEventListener('fx:motionchange', () => {
        if (!window.Chart) return;
        window.Chart.defaults.animation = motion.reduced ? false : { duration: motion.duration('--fx-dur-5') };
        Object.values(window.Chart.instances).forEach(chart => { chart.options.animation = window.Chart.defaults.animation; if (motion.reduced) chart.stop(); });
    });
})();
