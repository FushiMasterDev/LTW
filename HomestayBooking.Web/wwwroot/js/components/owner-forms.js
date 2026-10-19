(() => {
  const forms = [...document.querySelectorAll('[data-owner-form]')];
  forms.forEach(form => {
    let dirty = false;
    form.addEventListener('input', () => { dirty = true; });
    form.addEventListener('change', () => { dirty = true; });
    form.addEventListener('submit', event => queueMicrotask(() => { if (!event.defaultPrevented) dirty = false; }));
    window.addEventListener('beforeunload', event => {
      if (dirty) { event.preventDefault(); event.returnValue = ''; }
    });
  });
  document.querySelectorAll('[data-policy-rules]').forEach(root => {
    const form = root.closest('form');
    let serial = root.children.length;
    form.querySelector('[data-add-rule]').addEventListener('click', () => {
      const row = root.querySelector('[data-policy-rule]').cloneNode(true);
      row.querySelectorAll('input').forEach(input => {
        const oldId = input.id;
        input.id = oldId + '-' + (++serial);
        row.querySelector(`label[for="${oldId}"]`).htmlFor = input.id;
        input.value = '';
      });
      root.append(row); row.querySelector('input').focus();
      form.dispatchEvent(new Event('input'));
    });
    root.addEventListener('click', event => {
      if (!event.target.closest('[data-remove-rule]') || root.children.length < 2) return;
      event.target.closest('[data-policy-rule]').remove();
      form.dispatchEvent(new Event('input'));
    });
  });
})();
