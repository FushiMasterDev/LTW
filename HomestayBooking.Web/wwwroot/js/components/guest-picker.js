// Nút +/- chỉ điều khiển input Guests thật, giữ ràng buộc min/max và model binding.
document.querySelectorAll('[data-guest-picker]').forEach(root => {
  const input = root.querySelector('input');
  const buttons = [...root.querySelectorAll('[data-guest-step]')];
  const sync = () => buttons.forEach(button => {
    const step = Number(button.dataset.guestStep), value = Number(input.value);
    button.disabled = input.disabled || (step < 0 ? value <= Number(input.min) : value >= Number(input.max));
  });
  root.addEventListener('click', event => {
    const button = event.target.closest('[data-guest-step]');
    if (!button || button.disabled) return;
    input.value = Math.min(Number(input.max), Math.max(Number(input.min), Number(input.value || input.min) + Number(button.dataset.guestStep)));
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new Event('change', { bubbles: true }));
  });
  input.addEventListener('input', sync); sync();
});
