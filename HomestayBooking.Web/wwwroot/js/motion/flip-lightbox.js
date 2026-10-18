// FLIP retains the thumbnail's position on open and close; Bootstrap still owns focus and dismissal.
(() => {
    'use strict';
    const modal = document.getElementById('lightbox');
    const motion = window.FxMotion;
    if (!modal || !motion) return;
    const view = modal.querySelector('[data-lightbox-view]');
    let opener = null, animation = null, closing = false, generation = 0;
    const timing = () => ({ duration: motion.duration('--fx-dur-3'), easing: getComputedStyle(document.documentElement).getPropertyValue('--fx-ease-out').trim() });
    const delta = source => {
        const start = source.getBoundingClientRect(), end = view.getBoundingClientRect();
        if (!start.width || !end.width || start.bottom <= 0 || start.top >= innerHeight) return null;
        return `translate3d(${start.left - end.left}px,${start.top - end.top}px,0) scale(${start.width / end.width},${start.height / end.height})`;
    };
    modal.addEventListener('show.bs.modal', event => {
        opener = event.relatedTarget?.querySelector('img') || event.relatedTarget || document.activeElement;
        closing = false; generation++;
    });
    modal.addEventListener('shown.bs.modal', async () => {
        const current = generation;
        await view.decode().catch(() => {});
        if (current !== generation || motion.reduced || !modal.classList.contains('show') || !opener) return;
        const from = delta(opener);
        if (!from) return;
        view.style.willChange = 'transform';
        animation = view.animate([{ transform: from, opacity: .8 }, { transform: 'none', opacity: 1 }], timing());
        animation.finished.catch(() => {}).finally(() => view.style.removeProperty('will-change'));
    });
    modal.addEventListener('hide.bs.modal', event => {
        generation++;
        animation?.cancel();
        if (motion.reduced || closing || !opener) return;
        const source = [...document.querySelectorAll('button[data-lightbox-src]')].find(button => button.dataset.lightboxSrc === view.getAttribute('src'))?.querySelector('img') || opener;
        const to = delta(source);
        if (!to) return;
        event.preventDefault(); closing = true;
        view.style.willChange = 'transform';
        animation = view.animate([{ transform: 'none', opacity: 1 }, { transform: to, opacity: .8 }], timing());
        animation.finished.catch(() => {}).finally(() => {
            view.style.removeProperty('will-change');
            bootstrap.Modal.getInstance(modal)?.hide();
        });
    });
    modal.addEventListener('hidden.bs.modal', () => { animation?.cancel(); closing = false; });
    document.addEventListener('fx:motionchange', () => { if (motion.reduced) animation?.cancel(); });
})();
