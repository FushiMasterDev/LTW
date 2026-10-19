// Trình soạn thảo cho textarea có data-rich-text (TinyMCE 6, MIT, tự host ở lib/tinymce).
// Không có JS hoặc không nạp được TinyMCE thì textarea vẫn dùng bình thường; HTML luôn được server lọc bằng HtmlSanitizer.
// Đổi sáng/tối: khởi tạo lại với skin tương ứng, giữ nguyên nội dung đang soạn.
(() => {
  'use strict';
  if (!window.tinymce) return;
  const areas = [...document.querySelectorAll('textarea[data-rich-text]')];
  if (!areas.length) return;

  const root = document.documentElement;
  const dark = () => root.getAttribute('data-bs-theme') === 'dark';
  const vi = (root.lang || '').toLowerCase().startsWith('vi');
  const labelOf = area => document.querySelector(`label[for="${CSS.escape(area.id)}"]`)?.textContent.trim() || '';
  // Vùng soạn nằm trong iframe nên không thấy biến CSS của trang: đọc token rồi truyền vào content_style
  const token = name => getComputedStyle(root).getPropertyValue(name).trim();
  const contentStyle = () => `body { background: ${token('--fx-surface')}; color: ${token('--fx-ink')};
    font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; font-size: 16px; line-height: 1.6; margin: 12px; }
    a { color: ${token('--fx-primary')}; }`;

  const init = area => tinymce.init({
    target: area,
    promotion: false,
    branding: false,
    menubar: false,
    statusbar: false,
    plugins: 'lists link autolink',
    toolbar: 'undo redo | bold italic | bullist numlist | link | removeformat',
    // Giữ chữ có dấu nguyên dạng trong HTML lưu xuống (không đổi "â" thành "&acirc;")
    entity_encoding: 'raw',
    link_default_target: '_blank',
    link_assume_external_targets: 'https',
    min_height: 240,
    height: 280,
    skin: dark() ? 'oxide-dark' : 'oxide',
    content_css: dark() ? 'dark' : 'default',
    content_style: contentStyle(),
    language: vi ? 'vi' : undefined,
    language_url: vi ? `${tinymce.baseURL}/langs/vi.js` : undefined,
    iframe_aria_text: labelOf(area),
    setup: editor => {
      // Lưu ngược vào textarea mỗi lần đổi để form, validation và hộp xác nhận rời trang thấy nội dung mới
      editor.on('change input undo redo', () => editor.save());
    },
  });

  areas.forEach(init);

  let current = dark();
  new MutationObserver(() => {
    if (dark() === current) return;
    current = dark();
    areas.forEach(area => {
      const editor = tinymce.get(area.id);
      if (editor) { editor.save(); editor.remove(); }
      init(area);
    });
  }).observe(root, { attributes: true, attributeFilter: ['data-bs-theme'] });
})();
