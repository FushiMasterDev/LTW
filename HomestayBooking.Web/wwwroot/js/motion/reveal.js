// One observer owns entrances and persistent visibility subscriptions across the layout.
(() => {
    'use strict';
    const root = document.documentElement;
    const preference = matchMedia('(prefers-reduced-motion: reduce)');
    const listeners = new Map();
    const observer = 'IntersectionObserver' in window ? new IntersectionObserver(entries => {
        for (const entry of entries) {
            const records = listeners.get(entry.target);
            if (!records) continue;
            for (const record of [...records]) {
                record.callback(entry);
                if (record.once && entry.isIntersecting) records.delete(record);
            }
            if (!records.size) { observer.unobserve(entry.target); listeners.delete(entry.target); }
        }
    }, { threshold: [0, .12] }) : null;
    const observe = (element, callback, once = false) => {
        if (!element) return;
        if (!observer) { callback({ target: element, isIntersecting: true, boundingClientRect: element.getBoundingClientRect() }); return; }
        if (!listeners.has(element)) listeners.set(element, new Set());
        listeners.get(element).add({ callback, once });
        observer.observe(element);
    };
    const duration = name => parseFloat(getComputedStyle(root).getPropertyValue(name)) || 0;
    window.FxMotion = { observe, duration, get reduced() { return preference.matches || !root.classList.contains('fx-motion'); } };
    const reveal = entry => {
        if (!entry.isIntersecting) return;
        entry.target.classList.add('is-in');
        setTimeout(() => { entry.target.dataset.fxSettled = ''; }, duration('--fx-dur-5') + 5 * duration('--fx-stagger'));
    };
    const publicPage = document.body.classList.contains('site-public');
    const selector = publicPage
        ? '.home-section__head > *, .home-areas__intro > *, .fx-area-card, .fx-homestay-card, .home-steps > h2, .home-steps__list > li, .home-promises > h2, .home-promises__list > li, .home-partner > *, .stay-section > h2, .stay-room, .fx-review-card, .fx-stepper, .booking-head, .partner-intro > h1, .partner-intro > p'
        : '.fx-stat';
    const elements = [...document.querySelectorAll(selector)];
    for (const element of elements) {
        element.dataset.fxReveal = '';
        const siblings = [...element.parentElement.children].filter(sibling => sibling.matches(selector));
        element.style.setProperty('--fx-i', Math.min(siblings.indexOf(element), 5));
        observe(element, reveal, true);
    }
    // An observer is ready before CSS can hide anything; a failed/missing file leaves a readable page.
    if (observer) root.classList.add('fx-motion-ready');
    const update = () => {
        if (preference.matches) root.classList.remove('fx-motion');
        else if (!root.hasAttribute('data-motion-paused')) root.classList.add('fx-motion');
        if (window.FxMotion.reduced) elements.forEach(element => element.classList.add('is-in'));
        document.dispatchEvent(new Event('fx:motionchange'));
    };
    preference.addEventListener('change', update);
    document.addEventListener('click', event => {
        const button = event.target.closest('[data-motion-toggle]');
        if (!button) return;
        const paused = root.toggleAttribute('data-motion-paused');
        button.setAttribute('aria-pressed', String(paused));
        root.classList.toggle('fx-motion', !paused && !preference.matches);
        button.querySelector('[data-motion-label]').textContent = paused ? button.dataset.resume : button.dataset.pause;
        update();
    });
    // Keyboard navigation must never wait for an entrance.
    document.addEventListener('focusin', event => {
        event.target.closest('[data-fx-reveal]')?.classList.add('is-in');
    });
    const sentinel = document.querySelector('.home-hero__sentinel');
    const header = document.querySelector('.site-header--home');
    if (header && sentinel) observe(sentinel, entry => header.classList.toggle('is-past-hero', entry.boundingClientRect.top < 0));
    // Old homepage still works during the component-to-page rollout.
    else if (header) header.classList.add('is-past-hero');
    update();
})();
