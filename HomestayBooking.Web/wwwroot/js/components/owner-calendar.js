(() => {
  const form = document.querySelector('[data-owner-room-select]');
  if (!form) return;
  form.querySelector('[name="homestayId"]').addEventListener('change', () => {
    form.querySelector('[name="roomId"]').disabled = true;
    form.requestSubmit();
  });
  form.querySelector('[name="roomId"]').addEventListener('change', () => form.requestSubmit());
  const block = document.querySelector('[data-owner-block]');
  if (!block) return;
  const start = block.querySelector('[name="start"]'), end = block.querySelector('[name="end"]');
  // Retain the native date controls as the no-JS/keyboard fallback.
  const enhanced = block.querySelector('[data-date-range-input]');
  const error = block.querySelector('[data-date-error]');
  // Khóa/mở ngày qua Web API (POST/DELETE /api/rooms/{roomId}/blocks) rồi thay lịch và danh sách khóa mới
  // mà không tải lại trang. Không có fetch thì form vẫn gửi thường tới Owner/Calendar như trước.
  const token = () => block.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  const busy = (button, on) => {
    if (!button) return;
    if (on) { button.dataset.label = button.innerHTML; button.disabled = true; button.setAttribute('aria-busy', 'true');
      button.textContent = button.dataset.loadingText || button.textContent; }
    else { button.disabled = false; button.removeAttribute('aria-busy'); if (button.dataset.label) button.innerHTML = button.dataset.label; }
  };
  const problem = async response => {
    try { const body = await response.json(); return body.detail || body.title || ''; } catch { return ''; }
  };
  const refresh = async () => {
    const response = await fetch(location.href, { headers: { Accept: 'text/html' }, credentials: 'same-origin' });
    const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
    for (const name of ['data-owner-calendar-slot', 'data-owner-unlock-slot']) {
      const target = document.querySelector(`[${name}]`), source = doc.querySelector(`[${name}]`);
      if (target && source) target.innerHTML = source.innerHTML;
    }
    document.querySelectorAll('[data-owner-calendar-slot] [data-room-calendar]').forEach(root => window.fxRoomCalendar?.init(root));
  };
  const call = async (form, url, method, body) => {
    const button = form.querySelector('[type="submit"]');
    busy(button, true);
    try {
      const response = await fetch(url, { method, credentials: 'same-origin',
        headers: { Accept: 'application/json', 'Content-Type': 'application/json', RequestVerificationToken: token() },
        body: body ? JSON.stringify(body) : undefined });
      if (!response.ok) { window.fxToast?.(await problem(response) || form.dataset.failed, 'danger'); return false; }
      window.fxToast?.(form.dataset.done, 'success');
      await refresh();
      return true;
    } catch {
      window.fxToast?.(form.dataset.failed, 'danger');
      return false;
    } finally {
      busy(button, false);
    }
  };

  block.addEventListener('submit', async event => {
    if (!(start.value && end.value && end.value > start.value)) {
      event.preventDefault();
      error.textContent = block.dataset.dateRequired;
      (enhanced?._flatpickr ? enhanced : start).focus();
      return;
    }
    if (!window.fetch || !block.dataset.blockApi) return;
    event.preventDefault();
    const ok = await call(block, block.dataset.blockApi, 'POST', {
      start: start.value, end: end.value,
      reason: block.querySelector('[name="reason"]')?.value || 'OtherChannel',
      note: block.querySelector('[name="note"]')?.value || null,
    });
    if (ok) { start.value = ''; end.value = ''; enhanced?._flatpickr?.clear(); error.textContent = ''; }
  });

  // Pha capture: chạy trước bộ xử lý "đang lưu" của site.js (jQuery, pha bubble) để nút không bị khóa khi gọi API thất bại.
  // Form mở khóa được thay mới sau mỗi lần làm mới nên bắt ở document.
  document.addEventListener('submit', async event => {
    const form = event.target.closest('form[data-unlock-api]');
    if (!form || !window.fetch) return;
    const blockId = form.querySelector('[name="blockId"]')?.value;
    if (!blockId) return;
    event.preventDefault();
    await call(form, form.dataset.unlockApi + encodeURIComponent(blockId), 'DELETE');
  }, true);
  document.addEventListener('fx:room-range-selected', event => {
    start.value = event.detail.start; end.value = event.detail.end;
    enhanced?._flatpickr?.setDate([start.value,end.value], false, 'Y-m-d');
    error.textContent = '';
    block.querySelector('[type="submit"]').scrollIntoView({block:'nearest'});
  });
  let first;
  document.addEventListener('fx:room-night-selected', event => {
    if (event.detail.state !== 'Free') return;
    if (!first) { first = event.detail.date; start.value = first; end.value = ''; }
    else {
      if (event.detail.date <= first) { first = event.detail.date; start.value = first; return; }
      end.value = event.detail.date;
      const picker = block.querySelector('[data-date-range-input]')?._flatpickr;
      picker?.setDate([start.value, end.value], false, 'Y-m-d');
      first = null;
      block.querySelector('[type="submit"]').focus({preventScroll:true});
    }
  });
})();
