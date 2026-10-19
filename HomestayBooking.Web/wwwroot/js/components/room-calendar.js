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

// Tùy chọn kéo chọn khoảng đêm cho Owner/UI Kit; không ghi dữ liệu.
document.querySelectorAll('[data-room-calendar][data-range-selection="true"]').forEach(root => {
  let first, last, pointer, dragged = false;
  const selectable = cell => cell?.matches('[data-calendar-night]:not(:disabled)[data-state="Free"]');
  const clear = () => root.querySelectorAll('[data-calendar-night]').forEach(cell => cell.setAttribute('aria-pressed', 'false'));
  root.addEventListener('pointerdown', event => {
    const cell = event.target.closest('[data-calendar-night]');
    if (!selectable(cell) || event.button !== 0) return;
    first = last = cell; pointer = event.pointerId; dragged = false;
    cell.setPointerCapture(pointer);
  });
  root.addEventListener('pointermove', event => {
    if (!first || event.pointerId !== pointer) return;
    const cell = document.elementFromPoint(event.clientX, event.clientY)?.closest('[data-calendar-night]');
    if (!selectable(cell) || cell.dataset.roomId !== first.dataset.roomId) return;
    last = cell; dragged ||= first !== last;
    const dates = [first.dataset.date,last.dataset.date].sort();
    clear();
    root.querySelectorAll('[data-calendar-night]').forEach(n => {
      if (n.dataset.roomId === first.dataset.roomId && n.dataset.date >= dates[0] && n.dataset.date <= dates[1]) n.setAttribute('aria-pressed','true');
    });
  });
  root.addEventListener('pointerup', event => {
    if (!first || event.pointerId !== pointer) return;
    if (dragged) {
      const dates = [first.dataset.date,last.dataset.date].sort();
      const cells = [...root.querySelectorAll('[data-calendar-night]')].filter(n => n.dataset.roomId === first.dataset.roomId && n.dataset.date >= dates[0] && n.dataset.date <= dates[1]);
      if (cells.every(selectable)) {
        const end = new Date(dates[1] + 'T12:00:00'); end.setDate(end.getDate()+1);
        const endKey = `${end.getFullYear()}-${String(end.getMonth()+1).padStart(2,'0')}-${String(end.getDate()).padStart(2,'0')}`;
        root.querySelector('[data-calendar-selection]').textContent = root.dataset.rangeSelected.replace('{0}',cells.length);
        root.dispatchEvent(new CustomEvent('fx:room-range-selected',{bubbles:true,detail:{roomId:Number(first.dataset.roomId),start:dates[0],end:endKey}}));
      } else clear();
    }
    first = last = null; pointer = null;
  });
  root.addEventListener('pointercancel', () => { first = last = null; pointer = null; clear(); });
  root.addEventListener('click', event => { if (dragged) { event.preventDefault(); event.stopImmediatePropagation(); dragged=false; } },true);
});
