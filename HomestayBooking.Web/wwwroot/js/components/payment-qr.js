// Clipboard, QR dự phòng và timer dùng chung cho cọc đầu/cọc bổ sung; không ghi dữ liệu.
document.querySelectorAll('[data-payment-qr]').forEach(root => {
  const timers = new WeakMap();
  root.querySelectorAll('[data-copy]').forEach(button => button.addEventListener('click', async () => {
    try {
      await navigator.clipboard.writeText(button.dataset.copy);
      button.dataset.copyLabel ??= button.textContent;
      clearTimeout(timers.get(button)); button.textContent = root.dataset.copied;
      timers.set(button, setTimeout(() => { button.textContent = button.dataset.copyLabel; }, 1500));
    } catch { window.fxToast?.(root.dataset.copyError, 'warning'); }
  }));
  const img = root.querySelector('img[data-qr-fallback]');
  const fallback = () => {
    root.querySelector('.fx-payment-qr__fallback').hidden = false;
    if (img.dataset.fallbackUsed) { img.hidden = true; return; }
    img.dataset.fallbackUsed = 'true'; img.src = img.dataset.qrFallback;
  };
  if (img) { img.addEventListener('error', fallback); if (img.complete && !img.naturalWidth) fallback(); }
  root.querySelectorAll('[data-payment-countdown]').forEach(counter => {
    const expires = new Date(counter.dataset.expires).getTime();
    if (!Number.isFinite(expires)) return;
    let timer;
    const tick = () => {
      const left = Math.max(0, Math.floor((expires - Date.now()) / 1000));
      counter.textContent = String(Math.floor(left / 60)).padStart(2, '0') + ':' + String(left % 60).padStart(2, '0');
      counter.closest('.countdown')?.classList.toggle('is-urgent', left < 120);
      if (!left) { clearInterval(timer); if (root.dataset.reloadOnExpiry === 'true') setTimeout(() => location.reload(), 3000); }
    };
    timer = setInterval(tick, 1000); tick();
    window.addEventListener('pagehide', () => clearInterval(timer), { once: true });
  });
});
