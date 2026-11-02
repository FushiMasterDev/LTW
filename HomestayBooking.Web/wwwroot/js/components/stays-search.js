// Tìm homestay không tải lại trang: thanh tìm kiếm, bộ lọc và phân trang gọi AJAX, Homestays/Index trả về partial _Results.
// Không có JS hoặc gọi lỗi thì vẫn là form GET và link phân trang như cũ.
(() => {
  const results = document.querySelector('[data-stays-results]');
  const search = document.getElementById('stays-search-area')?.form;
  const filterPanel = document.getElementById('stays-filter');
  const filter = filterPanel?.closest('form');
  if (!results || !search || !filter) return;

  // Mỗi form mang bản sao ẩn giá trị của form kia lúc tải trang, sau một lần AJAX bản sao đó đã cũ.
  // Vì vậy ghép từ cả hai: tên có ô nhập (không ẩn) trong bộ lọc lấy từ bộ lọc, còn lại lấy từ thanh tìm kiếm.
  const filterNames = new Set([...filter.elements].filter(e => e.name && e.type !== 'hidden').map(e => e.name));
  const query = () => {
    const params = new URLSearchParams();
    for (const [name, value] of new FormData(search)) if (!filterNames.has(name)) params.append(name, value);
    for (const [name, value] of new FormData(filter)) if (filterNames.has(name)) params.append(name, value);
    return params;
  };

  let sequence = 0, pushed = false;
  async function load(url, focus) {
    const current = ++sequence;
    results.setAttribute('aria-busy', 'true');
    try {
      const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' }, cache: 'no-store', credentials: 'same-origin' });
      if (!response.ok) throw new Error(String(response.status));
      const html = await response.text();
      if (current !== sequence) return; // đã có lần lọc mới hơn
      results.innerHTML = html;
      history.pushState({ stays: true }, '', url);
      pushed = true;
      if (focus) {
        results.focus({ preventScroll: true });
        if (results.getBoundingClientRect().top < 0) results.scrollIntoView({ block: 'start' });
      }
    } catch {
      if (current === sequence) location.assign(url); // lỗi mạng/máy chủ: tải cả trang như khi chưa có AJAX
    } finally {
      if (current === sequence) results.removeAttribute('aria-busy');
    }
  }

  const submit = e => {
    if (e.defaultPrevented) return;
    e.preventDefault();
    window.bootstrap?.Offcanvas.getInstance(filterPanel)?.hide(); // màn hình nhỏ: đóng bảng lọc
    load(`${location.pathname}?${query()}`, false);
  };
  search.addEventListener('submit', submit);
  filter.addEventListener('submit', submit);

  results.addEventListener('click', e => {
    const link = e.target.closest('.pagination a[href]');
    if (!link || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return; // mở tab mới: để trình duyệt xử lý
    e.preventDefault();
    load(link.href, true);
  });

  // shortcut: Back/Forward tải lại cả trang để form và kết quả khớp URL; nếu cần mượt hơn thì đổ lại giá trị form từ URL.
  window.addEventListener('popstate', () => { if (pushed) location.reload(); });
})();
