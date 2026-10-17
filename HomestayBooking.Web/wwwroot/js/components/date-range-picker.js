// Nâng cấp hai input ngày thật bằng Flatpickr; không có JS vẫn gửi form như cũ.
(() => {
  if (!window.flatpickr) return;
  document.querySelectorAll('[data-date-picker]').forEach(root => {
    const start = root.querySelector('[data-date-in]'), end = root.querySelector('[data-date-out]');
    const input = root.querySelector('[data-date-range-input]'), error = root.querySelector('[data-date-error]');
    const blocked = new Set();
    let picker;
    const key = date => flatpickr.formatDate(date, 'Y-m-d');
    const crossesBlocked = (from, to) => {
      for (const night = new Date(from); night < to; night.setDate(night.getDate() + 1))
        if (blocked.has(key(night))) return true;
      return false;
    };
    picker = flatpickr(input, {
      mode: 'range', dateFormat: 'd/m/Y', minDate: flatpickr.parseDate(root.dataset.minDate, 'Y-m-d'),
      locale: root.dataset.locale === 'vi' ? flatpickr.l10ns.vn : 'default', disableMobile: true,
      defaultDate: [start.value, end.value].filter(Boolean).map(value => flatpickr.parseDate(value, 'Y-m-d')),
      disable: [date => {
        const from = picker?.selectedDates[0];
        // Ngày trả phòng không chiếm đêm: được trả đúng ngày có đêm bị khóa, nhưng không đi xuyên qua đêm đó.
        return from && picker.selectedDates.length === 1 && date > from ? crossesBlocked(from, date) : blocked.has(key(date));
      }],
      onChange: dates => {
        error.textContent = '';
        if (dates.length === 2 && (dates[1] <= dates[0] || crossesBlocked(dates[0], dates[1]))) {
          picker.clear(false); start.value = ''; end.value = ''; error.textContent = root.dataset.rangeError; return;
        }
        start.value = dates[0] ? key(dates[0]) : '';
        end.value = dates[1] ? key(dates[1]) : '';
        start.dispatchEvent(new Event('change', { bubbles: true }));
        end.dispatchEvent(new Event('change', { bubbles: true }));
        // Handler ngày cũ có thể điều chỉnh min/ngày trả; giữ nguyên trạng thái chọn dở của range picker.
        if (dates.length === 1) end.value = '';
      },
      onOpen: () => input.setAttribute('aria-expanded', 'true'),
      onClose: () => { input.setAttribute('aria-expanded', 'false'); },
    });
    picker.calendarContainer.id = input.id + '-calendar';
    input.setAttribute('aria-controls', picker.calendarContainer.id);
    input.setAttribute('aria-expanded', 'false'); input.setAttribute('aria-haspopup', 'dialog');
    root.querySelector('[data-date-native]').hidden = true;
    root.querySelector('[data-date-enhanced]').hidden = false;
    if (root.dataset.blockedUrl) {
      fetch(root.dataset.blockedUrl, { headers: { Accept: 'application/json' } })
        .then(response => { if (!response.ok) throw new Error(); return response.json(); })
        .then(dates => { if (!Array.isArray(dates)) throw new Error(); dates.forEach(date => blocked.add(String(date).slice(0, 10))); picker.redraw(); })
        .catch(() => { error.textContent = root.dataset.loadError; });
    }
  });
})();
