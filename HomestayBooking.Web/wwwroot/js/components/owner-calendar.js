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
  block.addEventListener('submit', event => {
    if (start.value && end.value && end.value > start.value) return;
    event.preventDefault();
    error.textContent = block.dataset.dateRequired;
    (enhanced?._flatpickr ? enhanced : start).focus();
  });
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
