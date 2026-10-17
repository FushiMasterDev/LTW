// Đóng modal bằng Esc/Hủy kể cả trong transition; callback khôi phục focus đúng nơi mở.
const fxBindModalDismiss = (element, onHidden) => {
    if (!element) return;
    let cancelOnShown = false;
    const cancel = () => { cancelOnShown = true; bootstrap.Modal.getOrCreateInstance(element).hide(); };
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && element.classList.contains('show')) { event.preventDefault(); cancel(); }
    }, true);
    element.addEventListener('click', event => { if (event.target.closest('[data-bs-dismiss="modal"]')) cancel(); });
    element.addEventListener('shown.bs.modal', () => { if (cancelOnShown) bootstrap.Modal.getOrCreateInstance(element).hide(); });
    element.addEventListener('hidden.bs.modal', () => { cancelOnShown = false; onHidden?.(); });
};

// Modal xác nhận dùng chung, giữ data-confirm và giữ nguyên nút submit/name/value gốc.
(() => {
    const element = document.getElementById('fx-confirm-dialog');
    let pending = null;
    $(document).on('submit', 'form', function (event) {
        if (this.dataset.fxConfirmed === 'true') { delete this.dataset.fxConfirmed; return; }
        const submitter = event.originalEvent?.submitter;
        const message = submitter?.dataset.confirm || this.dataset.confirm;
        if (!message) return;
        const title = submitter?.dataset.confirmTitle || this.dataset.confirmTitle;
        const variant = submitter?.dataset.confirmVariant || this.dataset.confirmVariant;
        if (event.isDefaultPrevented() || ($.fn.valid && $(this).data('validator') && !$(this).valid())) return;
        if (!element || !window.bootstrap) { if (!window.confirm(message)) event.preventDefault(); return; }
        event.preventDefault();
        pending = { form: this, submitter };
        element.querySelector('#fx-confirm-title').textContent = title || element.dataset.defaultTitle;
        element.querySelector('#fx-confirm-message').textContent = message;
        element.querySelector('[data-confirm-accept]').className = 'btn ' + (variant === 'danger' ? 'btn-outline-danger' : 'btn-brand');
        bootstrap.Modal.getOrCreateInstance(element).show(pending.submitter);
    });
    element?.querySelector('[data-confirm-accept]').addEventListener('click', () => {
        const action = pending; if (!action) return; pending = null;
        action.form.dataset.fxConfirmed = 'true'; bootstrap.Modal.getOrCreateInstance(element).hide();
        const button = action.submitter?.form === action.form && action.submitter.type === 'submit' ? action.submitter : undefined;
        action.form.requestSubmit(button);
    });
    fxBindModalDismiss(element, () => { pending?.submitter?.focus(); pending = null; });
})();

// Toast chỉ dành cho phản hồi AJAX/background; mọi nội dung được gán bằng textContent.
window.fxToast = (message, tone = 'info') => {
    const host = document.getElementById('fx-toast-host'); if (!host || !window.bootstrap) return;
    const colors = { success: 'tone-emerald', danger: 'tone-rose', info: 'tone-sky', warning: 'tone-amber' };
    const toast = document.createElement('div'); toast.className = 'toast ' + (colors[tone] || colors.info); toast.setAttribute('role', 'status');
    const header = document.createElement('div'); header.className = 'toast-header';
    const title = document.createElement('strong'); title.className = 'me-auto'; title.textContent = host.dataset.title;
    const close = document.createElement('button'); close.type = 'button'; close.className = 'btn-close'; close.setAttribute('data-bs-dismiss', 'toast'); close.setAttribute('aria-label', host.dataset.closeLabel);
    const body = document.createElement('div'); body.className = 'toast-body'; body.textContent = String(message);
    header.append(title, close); toast.append(header, body); host.append(toast);
    toast.addEventListener('hidden.bs.toast', () => toast.remove(), { once: true });
    bootstrap.Toast.getOrCreateInstance(toast, { delay: 5000 }).show();
};

// Trạng thái "đang xử lý" cho nút submit có data-loading-text, chặn bấm hai lần.
// Chỉ bật khi form hợp lệ (jQuery Validation) và submit không bị hủy.
$(document).on('submit', 'form', function (e) {
    const $form = $(this);
    if (e.isDefaultPrevented() || ($.fn.valid && $form.data('validator') && !$form.valid())) {
        return;
    }
    $form.find('button[type="submit"][data-loading-text]').each(function () {
        const $btn = $(this);
        $btn.prop('disabled', true)
            .attr('aria-busy', 'true')
            .html('<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> ' + $('<span>').text($btn.data('loadingText')).html());
    });
});

// Hiện/ẩn mật khẩu: <button data-toggle-password="#Password" data-label-show="..." data-label-hide="...">
$(document).on('click', '[data-toggle-password]', function () {
    const $btn = $(this);
    const $input = $($btn.data('togglePassword'));
    const show = $input.attr('type') === 'password';
    $input.attr('type', show ? 'text' : 'password');
    $btn.attr('aria-label', show ? $btn.data('labelHide') : $btn.data('labelShow'))
        .attr('aria-pressed', show ? 'true' : 'false')
        .find('.bi').toggleClass('bi-eye', !show).toggleClass('bi-eye-slash', show);
});

// Chế độ sáng/tối: <button data-theme-toggle data-label-dark="..." data-label-light="...">.
// Lựa chọn lưu ở localStorage "fx-theme"; chưa chọn thì theo hệ điều hành. Đoạn script trong <head>
// (_ThemeInit.cshtml) đặt data-bs-theme trước khi vẽ trang để không bị nháy màu.
(() => {
    const key = 'fx-theme';
    const root = document.documentElement;
    const saved = () => { try { return localStorage.getItem(key); } catch { return null; } };
    const sync = () => {
        const dark = root.getAttribute('data-bs-theme') === 'dark';
        document.querySelectorAll('[data-theme-toggle]').forEach(btn => {
            const label = dark ? btn.dataset.labelLight : btn.dataset.labelDark;
            btn.setAttribute('aria-pressed', dark ? 'true' : 'false');
            if (label) {
                btn.setAttribute('aria-label', label);
                btn.title = label;
            }
        });
    };
    const apply = theme => { root.setAttribute('data-bs-theme', theme); sync(); };

    document.addEventListener('click', e => {
        if (!e.target.closest('[data-theme-toggle]')) return;
        const next = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        try { localStorage.setItem(key, next); } catch { /* chế độ riêng tư: chỉ đổi cho trang hiện tại */ }
        apply(next);
    });
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
        if (!saved()) apply(e.matches ? 'dark' : 'light');
    });
    sync();
})();

// Thu gọn sidebar dashboard trên desktop, nhớ lựa chọn ở localStorage "fx-sidebar"
$(document).on('click', '[data-sidebar-collapse]', function () {
    const collapsed = document.documentElement.classList.toggle('sidebar-collapsed');
    try { localStorage.setItem('fx-sidebar', collapsed ? 'collapsed' : 'expanded'); } catch { /* bỏ qua */ }
    this.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
});
document.querySelectorAll('[data-sidebar-collapse]').forEach(btn =>
    btn.setAttribute('aria-expanded', document.documentElement.classList.contains('sidebar-collapsed') ? 'false' : 'true'));

// Ảnh homestay không tải được: ẩn ảnh để hiện khung thay thế bên dưới
(() => {
    const markBroken = img => img.closest('.fx-homestay-card__media, .fx-gallery__item')?.classList.add('is-broken');
    document.querySelectorAll('.fx-homestay-card__media > img, .fx-gallery__item img').forEach(img => {
        if (img.complete && img.naturalWidth === 0 && img.getAttribute('src')) markBroken(img);
        else img.addEventListener('error', () => markBroken(img), { once: true });
    });
})();

// Xem ảnh lớn: nút [data-lightbox-src][data-lightbox-index] + modal #lightbox
(() => {
    const modalEl = document.getElementById('lightbox');
    if (!modalEl || !window.bootstrap) return;
    const items = [...document.querySelectorAll('[data-lightbox-src]')];
    const view = modalEl.querySelector('[data-lightbox-view]');
    const counter = modalEl.querySelector('[data-lightbox-counter]');
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    let index = 0, opener = null;
    fxBindModalDismiss(modalEl, () => opener?.focus());
    const show = n => {
        index = (n + items.length) % items.length;
        view.src = items[index].dataset.lightboxSrc;
        view.alt = items[index].dataset.lightboxAlt || '';
        counter.textContent = `${index + 1} / ${items.length}`;
    };
    document.addEventListener('click', e => {
        const item = e.target.closest('[data-lightbox-src]');
        if (item) {
            opener = item;
            show(Number(item.dataset.lightboxIndex));
            modal.show(item);
            return;
        }
        const step = e.target.closest('[data-lightbox-step]');
        if (step) show(index + Number(step.dataset.lightboxStep));
    });
    modalEl.addEventListener('keydown', e => {
        if (e.key === 'ArrowLeft') show(index - 1);
        if (e.key === 'ArrowRight') show(index + 1);
    });
})();

// Form tìm kiếm có data-date-range: ngày trả phòng không được sớm hơn ngày sau ngày nhận phòng
$(document).on('change', 'form[data-date-range] input[name="CheckIn"]', function () {
    const checkOut = this.form.querySelector('input[name="CheckOut"]');
    if (!checkOut || !this.value) return;
    const next = new Date(this.value + 'T00:00:00');
    next.setDate(next.getDate() + 1);
    const min = `${next.getFullYear()}-${String(next.getMonth() + 1).padStart(2, '0')}-${String(next.getDate()).padStart(2, '0')}`;
    checkOut.min = min;
    if (checkOut.value && checkOut.value < min) checkOut.value = min;
});

// [motion] Hỗ trợ motion.css. Bỏ animation thì xóa cả block này; không block nào ở trên phụ thuộc vào nó.
(() => {
    const root = document.documentElement;

    // Đổi sáng/tối: chạy ở pha capture (trước handler đổi theme phía trên) để tắt transition
    // đúng khung hình đổi màu, rồi bật lại ở khung hình kế tiếp
    document.addEventListener('click', e => {
        if (!e.target.closest('[data-theme-toggle]')) return;
        root.setAttribute('data-theme-switching', '');
        requestAnimationFrame(() => requestAnimationFrame(() => root.removeAttribute('data-theme-switching')));
    }, true);

    // Ảnh lazy chưa tải: ẩn tới khi tải xong rồi mờ dần vào (lỗi tải thì hiện khung thay thế như cũ)
    document.querySelectorAll('.fx-homestay-card__media > img[loading="lazy"], .fx-gallery__item img[loading="lazy"]').forEach(img => {
        if (img.complete) return;
        img.classList.add('is-loading');
        const done = () => img.classList.remove('is-loading');
        img.addEventListener('load', done, { once: true });
        img.addEventListener('error', done, { once: true });
    });

    // Xem ảnh lớn: ẩn ảnh cũ khi chuyển ảnh, hiện lại khi ảnh mới tải xong (tối đa 400ms)
    const lightbox = document.getElementById('lightbox');
    const view = lightbox?.querySelector('[data-lightbox-view]');
    if (view && document.querySelectorAll('[data-lightbox-src]').length > 1) {
        let revealTimer;
        const reveal = () => { clearTimeout(revealTimer); view.classList.remove('is-swapping'); };
        const hide = () => {
            view.classList.add('is-swapping');
            clearTimeout(revealTimer);
            revealTimer = setTimeout(reveal, 400);
        };
        lightbox.addEventListener('hidden.bs.modal', reveal);
        view.addEventListener('load', reveal);
        view.addEventListener('error', reveal);
        lightbox.addEventListener('click', e => { if (e.target.closest('[data-lightbox-step]')) hide(); }, true);
        lightbox.addEventListener('keydown', e => { if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') hide(); }, true);
    }
})();
// [/motion]

// Chỉ các trang có gallery mới nhận hành vi này.
document.querySelectorAll('[data-gallery]').forEach(gallery => gallery.addEventListener('keydown', event => {
    if (!['ArrowLeft', 'ArrowRight'].includes(event.key)) return;
    const buttons = [...gallery.querySelectorAll('button[data-lightbox-src]')], index = buttons.indexOf(document.activeElement);
    if (index < 0) return; event.preventDefault();
    buttons[(index + (event.key === 'ArrowRight' ? 1 : -1) + buttons.length) % buttons.length].focus();
}));
