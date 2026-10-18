(() => {
    'use strict';
    if (!window.FxAutoplay) return;
    document.querySelectorAll('[data-carousel-3d]').forEach(root => {
        const cards = [...root.querySelectorAll('.fx-area-card')];
        if (cards.length < 3) return;
        let index = 0, turn = 0, startX = null, suspended = [];
        const mobile = matchMedia('(max-width: 767.98px)');
        const draw = () => {
            const radius = Math.max(220, cards.length * 43);
            cards.forEach((card, i) => {
                let distance = (i - index + cards.length) % cards.length;
                if (distance > cards.length / 2) distance -= cards.length;
                card.style.transform = mobile.matches
                    ? `translate3d(${distance * 77}%,0,${-Math.abs(distance) * 65}px) rotateY(${distance === 0 ? 0 : distance > 0 ? -35 : 35}deg)`
                    : `rotateY(${i * 360 / cards.length + turn}deg) translateZ(${radius}px) rotateY(0deg)`;
                card.style.opacity = String(distance === 0 ? 1 : Math.abs(distance) === 1 ? .72 : .3);
                card.style.zIndex = String(cards.length - Math.abs(distance));
                if (i === index) card.setAttribute('aria-current', 'true'); else card.removeAttribute('aria-current');
            });
        };
        const select = (next, direction = 0) => {
            suspended.forEach(a => a.cancel()); suspended = [];
            const normalized = (next + cards.length) % cards.length;
            let delta = direction || normalized - index;
            if (!direction && Math.abs(delta) > cards.length / 2) delta -= Math.sign(delta) * cards.length;
            turn -= delta * 360 / cards.length;
            index = normalized;
            draw(); control?.reset();
        };
        let control;
        control = FxAutoplay.register(root, { advance: delta => select(index + delta, delta), onState: running => { if (running) { suspended.forEach(a => a.play()); suspended = []; } else { suspended = cards.flatMap(card => card.getAnimations()).filter(a => a.playState === 'running'); suspended.forEach(a => a.pause()); } } });
        root.classList.add('fx-carousel--ready');
        root.style.setProperty('--fx-ring-depth', `${Math.max(220, cards.length * 43)}px`);
        cards.forEach((card, i) => card.addEventListener('focus', () => select(i)));
        root.querySelectorAll('[data-motion-step]').forEach(button => button.addEventListener('click', () => select(index + Number(button.dataset.motionStep), Number(button.dataset.motionStep))));
        root.addEventListener('keydown', event => {
            if (!['ArrowLeft', 'ArrowRight'].includes(event.key)) return;
            event.preventDefault(); select(index + (event.key === 'ArrowRight' ? 1 : -1), event.key === 'ArrowRight' ? 1 : -1);
        });
        root.addEventListener('touchstart', event => { startX = event.touches[0].clientX; }, { passive: true });
        root.addEventListener('touchend', event => {
            if (startX === null) return;
            const delta = startX - event.changedTouches[0].clientX;
            if (Math.abs(delta) > 35) select(index + Math.sign(delta), Math.sign(delta));
            startX = null;
        }, { passive: true });
        mobile.addEventListener('change', draw);
        draw();
    });
})();
