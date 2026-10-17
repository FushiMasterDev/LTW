// Chỉ nạp trên Dev/UiKit, không thêm hành vi demo vào trang thật.
document.querySelector('[data-toast-demo]')?.addEventListener('click', event => window.fxToast(event.currentTarget.dataset.toastMessage, 'success'));
