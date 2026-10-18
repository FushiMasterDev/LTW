// Two real faces rotate around the image's centre. Only transform is animated.
(() => {
    'use strict';
    if (!window.FxMotion || !window.FxAutoplay) return;
    const active = new WeakMap();
    const turn = (view, src, alt, direction = 1) => {
        active.get(view)?.cancel();
        if (FxMotion.reduced || !view.getAttribute('src') || view.getAttribute('src') === src) { view.src = src; view.alt = alt; return null; }
        const box = view.getBoundingClientRect();
        const parent = view.parentElement, width = box.width;
        if (!width) { view.src = src; view.alt = alt; return null; }
        const scene = document.createElement('span'), cube = document.createElement('span');
        scene.className = 'fx-cube-scene'; scene.setAttribute('aria-hidden', 'true'); cube.className = 'fx-cube';
        scene.append(cube);
        for (const [url, rotation] of [[view.src, 0], [src, direction * 90]]) {
            const face = document.createElement('img'); face.src = url; face.alt = ''; face.decoding = 'async'; face.width = Math.round(width); face.height = Math.round(box.height);
            face.style.transform = `rotateY(${rotation}deg) translateZ(${width / 2}px)`; cube.append(face);
        }
        parent.append(scene); view.classList.add('is-cube-source');
        view.src = src; view.alt = alt;
        const animation = cube.animate([{ transform: `translateZ(${-width / 2}px) rotateY(0deg)` }, { transform: `translateZ(${-width / 2}px) rotateY(${-direction * 90}deg)` }], { duration: FxMotion.duration('--fx-dur-5'), easing: getComputedStyle(parent).getPropertyValue('--fx-ease-out').trim() });
        active.set(view, animation);
        const clean = () => { scene.remove(); if (active.get(view) === animation) { view.classList.remove('is-cube-source'); active.delete(view); } };
        animation.finished.then(clean, clean);
        return animation;
    };
    window.FxCube = { turn };
    document.querySelectorAll('[data-cube-gallery]').forEach(root => {
        const buttons = [...root.querySelectorAll('button[data-lightbox-src]')];
        const images = buttons.map(button => ({ src: button.dataset.lightboxSrc, alt: button.dataset.lightboxAlt }));
        if (images.length < 2) return;
        let index = 0, control, animation;
        const select = (next, direction = 1) => {
            index = (next + images.length) % images.length;
            buttons.forEach((button, i) => {
                const sourceIndex = (index + i) % images.length, source = images[sourceIndex];
                const view = button.querySelector('img');
                if (i === 0) animation = turn(view, source.src, source.alt, direction);
                else { view.src = source.src; view.alt = source.alt; }
                button.dataset.lightboxSrc = source.src; button.dataset.lightboxIndex = String(sourceIndex); button.dataset.lightboxAlt = source.alt; button.setAttribute('aria-label', source.alt);
                if (i === 0) button.setAttribute('aria-current', 'true'); else button.removeAttribute('aria-current');
            });
            control?.reset();
        };
        control = FxAutoplay.register(root, { advance: () => select(index + 1), onState: running => { if (animation && animation.playState !== 'finished') running ? animation.play() : animation.pause(); } });
        root.querySelectorAll('[data-motion-step]').forEach(button => button.addEventListener('click', () => select(index + Number(button.dataset.motionStep), Number(button.dataset.motionStep))));
        root.addEventListener('keydown', event => {
            if (!['ArrowLeft','ArrowRight'].includes(event.key)) return;
            event.preventDefault(); event.stopPropagation(); select(index + (event.key === 'ArrowRight' ? 1 : -1), event.key === 'ArrowRight' ? 1 : -1);
        });
        root.addEventListener('focusin', event => {
            const button = event.target.closest('button[data-lightbox-index]');
            if (button && button !== buttons[0]) select(Number(button.dataset.lightboxIndex));
        });
        document.addEventListener('fx:motionchange', () => { if (FxMotion.reduced) animation?.cancel(); });
        buttons[0].setAttribute('aria-current', 'true');
    });
})();
