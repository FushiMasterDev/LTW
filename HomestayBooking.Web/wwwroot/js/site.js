// Xác nhận trước khi gửi form có data-confirm (ví dụ nút Xóa)
$(document).on('submit', 'form[data-confirm]', function (e) {
    if (!window.confirm($(this).data('confirm'))) {
        e.preventDefault();
    }
});

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
