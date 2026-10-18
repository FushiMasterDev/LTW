(() => {
    'use strict';
    if (!window.FxAutoplay) return;
    document.querySelectorAll('[data-card-deck]').forEach(root => {
        const cards = [...root.querySelectorAll('[data-deck-card]')];
        const choices = [...root.querySelectorAll('[data-deck-select]')];
        if (cards.length < 2) return;
        let index = 0, control, animation;
        const select = (next, animate = true) => {
            animation?.cancel();
            const old = cards[index];
            index = (next + cards.length) % cards.length;
            cards.forEach((card, i) => {
                const depth = (i - index + cards.length) % cards.length;
                card.style.setProperty('--fx-depth', depth);
                card.style.zIndex = String(cards.length - depth);
                if (i === index) card.setAttribute('aria-current', 'step'); else card.removeAttribute('aria-current');
            });
            choices.forEach((button, i) => { if (i === index) button.setAttribute('aria-current', 'step'); else button.removeAttribute('aria-current'); });
            if (animate && !FxMotion.reduced && old !== cards[index]) {
                animation = old.animate([{ transform: 'translate3d(0,0,0) rotateX(0)' }, { transform: 'translate3d(0,-28px,60px) rotateX(-65deg)', offset: .45 }, { transform: 'translate3d(0,28px,-100px) rotateX(0)' }], { duration: FxMotion.duration('--fx-dur-5'), easing: getComputedStyle(root).getPropertyValue('--fx-ease-out').trim() });
            }
            control?.reset();
        };
        control = FxAutoplay.register(root, { advance: () => select(index + 1), onState: running => { if (animation && animation.playState !== 'finished') running ? animation.play() : animation.pause(); } });
        root.querySelectorAll('[data-motion-step]').forEach(b => b.addEventListener('click', () => select(index + Number(b.dataset.motionStep))));
        choices.forEach((b,i) => b.addEventListener('click', () => select(i)));
        cards.forEach((card,i) => card.addEventListener('focusin', () => select(i, false)));
        root.addEventListener('keydown', event => {
            if (!['ArrowLeft','ArrowRight'].includes(event.key)) return;
            event.preventDefault(); select(index + (event.key === 'ArrowRight' ? 1 : -1));
        });
        document.addEventListener('fx:motionchange', () => { if (FxMotion.reduced) animation?.cancel(); });
        select(0, false);
    });
})();
