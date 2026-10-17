// Báo giá trực tiếp ở form đặt phòng: gọi Bookings/Quote khi đổi ngày hoặc voucher, vẽ lại bằng markup của _PriceBreakdown.
(() => {
  const box = document.querySelector('[data-booking-quote]');
  if (!box) return;
  const form = box.closest('form');
  const checkIn = form.querySelector('[name="CheckIn"]'), checkOut = form.querySelector('[name="CheckOut"]');
  const voucher = form.querySelector('[name="VoucherId"]');
  const text = JSON.parse(box.dataset.text);
  const money = value => Number(value).toLocaleString('vi-VN') + 'đ';
  const el = (tag, className, content) => {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (content != null) node.textContent = content;
    return node;
  };
  const line = (label, amount) => {
    const row = el('div'), value = el('dd');
    value.append(el('span', 'fx-money', money(amount)));
    row.append(el('dt', null, label), value);
    return row;
  };
  const note = (className, message) => box.replaceChildren(el('p', className, message));
  let sequence = 0;

  async function refresh() {
    if (!checkIn.value || !checkOut.value) return;
    const current = ++sequence;
    box.setAttribute('aria-busy', 'true');
    try {
      const params = new URLSearchParams({ roomTypeId: box.dataset.roomType, checkIn: checkIn.value, checkOut: checkOut.value });
      if (voucher && voucher.value) params.set('voucherId', voucher.value);
      const response = await fetch(`${box.dataset.quoteUrl}?${params}`, { headers: { Accept: 'application/json' } });
      const quote = await response.json();
      if (current !== sequence) return;
      if (!quote.ok) { note('text-danger mb-0', quote.error); return; }
      const available = quote.freeRooms > 0;
      const badge = el('p', 'mb-3');
      badge.append(el('span', `badge-tone ${available ? 'tone-emerald' : 'tone-rose'}`,
        available ? text.free.replace('{0}', quote.freeRooms) : text.soldOut));
      const list = el('dl', 'fx-price-breakdown');
      quote.nights.forEach(night => list.append(line(`${night.date} · ${night.dayType}`, night.price)));
      list.append(line(text.total, quote.total), line(text.deposit, quote.deposit));
      if (quote.discount > 0) list.append(line(text.discount, -quote.discount));
      list.append(line(text.due, quote.amountDue));
      box.replaceChildren(badge, list);
    } catch {
      if (current === sequence) note('text-danger mb-0', text.failed);
    } finally {
      if (current === sequence) box.removeAttribute('aria-busy');
    }
  }

  checkIn.addEventListener('change', refresh);
  checkOut.addEventListener('change', refresh);
  voucher?.addEventListener('change', refresh);
  refresh();
})();
