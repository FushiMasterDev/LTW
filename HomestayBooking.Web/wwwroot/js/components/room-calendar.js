// Lịch chỉ chọn ô và phát sự kiện để màn nghiệp vụ ghép lại ở bước 05; không tự khóa/đổi phòng.
document.querySelectorAll('[data-room-calendar]').forEach(root => {
  root.querySelectorAll('[data-calendar-room]').forEach(room => {
    const cells = [...room.querySelectorAll('[data-calendar-night]:not(:disabled)')];
    const focus = index => { cells.forEach((cell, i) => { cell.tabIndex = i === index ? 0 : -1; }); cells[index]?.focus(); };
    cells.forEach((cell, index) => {
      cell.tabIndex = index ? -1 : 0;
      cell.addEventListener('keydown', event => {
        const offsets = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 };
        if (!(event.key in offsets) && !['Home', 'End'].includes(event.key)) return;
        event.preventDefault();
        const next = event.key === 'Home' ? 0 : event.key === 'End' ? cells.length - 1 : Math.min(cells.length - 1, Math.max(0, index + offsets[event.key]));
        focus(next);
      });
      cell.addEventListener('click', () => {
        root.querySelectorAll('[data-calendar-night]').forEach(other => other.setAttribute('aria-pressed', 'false'));
        cell.setAttribute('aria-pressed', 'true'); root.querySelector('[data-calendar-selection]').textContent = cell.getAttribute('aria-label');
        root.dispatchEvent(new CustomEvent('fx:room-night-selected', { bubbles: true, detail: { roomId: Number(cell.dataset.roomId), date: cell.dataset.date, state: cell.dataset.state } }));
      });
    });
  });
});
