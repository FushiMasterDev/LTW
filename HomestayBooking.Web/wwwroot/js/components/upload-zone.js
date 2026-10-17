// Kiểm tra phía client để phản hồi sớm; server vẫn kiểm tra kích thước và magic bytes.
document.querySelectorAll('[data-upload-zone]').forEach(root => {
  const input = root.querySelector('input[type=file]'), preview = root.querySelector('[data-upload-preview]');
  const error = root.querySelector('[data-upload-error]');
  const allowed = new Set(input.accept.split(','));
  const mimes = { '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.png': 'image/png', '.webp': 'image/webp', '.pdf': 'application/pdf' };
  let urls = [];
  const render = () => {
    urls.forEach(url => URL.revokeObjectURL(url)); urls = []; preview.replaceChildren(); error.textContent = '';
    const files = [...input.files];
    const invalid = files.find(file => {
      const extension = '.' + file.name.split('.').pop().toLowerCase();
      return !allowed.has(extension) || (file.type && file.type !== mimes[extension]);
    });
    const size = files.some(file => file.size <= 0 || file.size > Number(root.dataset.maxBytes));
    const message = invalid ? root.dataset.typeError : size ? root.dataset.sizeError : '';
    error.textContent = message; input.setCustomValidity(message);
    if (message) return;
    files.forEach((file, index) => {
      const item = document.createElement('div'); item.className = 'fx-upload__file';
      if (file.type.startsWith('image/')) {
        const img = document.createElement('img'); img.src = URL.createObjectURL(file); urls.push(img.src); img.alt = file.name; item.append(img);
      }
      const label = document.createElement('span'); label.textContent = file.name;
      const remove = document.createElement('button'); remove.type = 'button'; remove.className = 'btn btn-outline-secondary';
      remove.textContent = root.dataset.removeLabel; remove.setAttribute('aria-label', root.dataset.removeLabel + ': ' + file.name);
      remove.addEventListener('click', () => {
        const transfer = new DataTransfer(); files.filter((_, i) => i !== index).forEach(file => transfer.items.add(file));
        input.files = transfer.files; input.dispatchEvent(new Event('change', { bubbles: true }));
      });
      item.append(label, remove); preview.append(item);
    });
  };
  input.addEventListener('change', render);
  ['dragenter', 'dragover'].forEach(name => root.addEventListener(name, event => { event.preventDefault(); root.classList.add('is-dragging'); }));
  ['dragleave', 'drop'].forEach(name => root.addEventListener(name, event => { event.preventDefault(); root.classList.remove('is-dragging'); }));
  root.addEventListener('drop', event => {
    const transfer = new DataTransfer();
    [...event.dataTransfer.files].slice(0, input.multiple ? undefined : 1).forEach(file => transfer.items.add(file));
    input.files = transfer.files; input.dispatchEvent(new Event('change', { bubbles: true }));
  });
  window.addEventListener('pagehide', () => urls.forEach(url => URL.revokeObjectURL(url)), { once: true });
});
