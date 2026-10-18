// The hero drifts only while visible; the header uses the shared observer, never a scroll event.
(() => {
    'use strict';
    const motion = window.FxMotion;
    const hero = document.querySelector('.home-hero');
    const layer = document.querySelector('[data-hero-parallax]');
    const partner = document.querySelector('.home-partner');
    if (!motion || (!layer && !partner)) return;
    const coarse = matchMedia('(hover: none), (pointer: coarse)');
    let visible = false, partnerVisible = false, frame = 0;
    const update = () => {
        frame = 0;
        const disabled = motion.reduced || document.hidden;
        hero?.classList.toggle('is-playing', visible && !disabled);
        if (disabled) { layer?.style.removeProperty('transform'); partner?.style.removeProperty('transform'); return; }
        // Batch geometry reads before the writes.
        const heroRect = visible && hero ? hero.getBoundingClientRect() : null;
        const partnerRect = partnerVisible && partner ? partner.getBoundingClientRect() : null;
        if (heroRect && layer) {
            const y = Math.min(heroRect.height, Math.max(0, -heroRect.top)) * (coarse.matches ? .06 : .12);
            layer.style.transform = `translate3d(0,${y}px,-40px) scale(1.12)`;
        }
        if (partnerRect) {
            const progress = Math.max(0, Math.min(1, (innerHeight - partnerRect.top) / (innerHeight * .8)));
            partner.style.transform = `perspective(${getComputedStyle(document.documentElement).getPropertyValue("--fx-perspective").trim()}) rotateY(${(1 - progress) * (coarse.matches ? 3 : 10)}deg)`;
        }
    };
    const schedule = () => { if (!frame) frame = requestAnimationFrame(update); };
    if (hero) motion.observe(hero, entry => { visible = entry.isIntersecting; schedule(); });
    if (partner) motion.observe(partner, entry => { partnerVisible = entry.isIntersecting; schedule(); });
    addEventListener('scroll', () => { if (visible || partnerVisible) schedule(); }, { passive: true });
    addEventListener('resize', schedule, { passive: true });
    document.addEventListener('visibilitychange', schedule);
    document.addEventListener('fx:motionchange', schedule);
    schedule();
})();
