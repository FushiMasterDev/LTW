"""Đăng nhập từng vai trò seed, duyệt link nội bộ (GET) từ các trang bắt đầu, báo trang lỗi 4xx/5xx. Chỉ gửi GET, trừ POST đăng nhập. Dùng: python3 -I tools/ui-crawl.py http://localhost:5012"""
import http.cookiejar, re, sys, urllib.parse, urllib.request
from html import unescape

BASE = sys.argv[1].rstrip('/')
PASSWORD = 'Fuji@2026'
ROLES = {
    'anon': (None, None, ['/', '/Homestays', '/Bookings/Lookup', '/Bookings/Create', '/Account/Login', '/Account/Register',
                          '/Account/EmployeeLogin', '/Home/StatusCode/404', '/Home/Error']),
    'customer': ('/Account/Login', ('PhoneNumber', '0987000001'), ['/Bookings/Mine', '/Account/ChangePassword', '/Account/AccessDenied']),
    'owner': ('/Account/Login', ('PhoneNumber', '0912000001'), ['/Owner/Dashboard', '/Owner/Bookings']),
    'NV001': ('/Account/EmployeeLogin', ('EmployeeCode', 'NV001'), ['/Staff/Dashboard', '/Staff/ChangeRequests']),
    'KT001': ('/Account/EmployeeLogin', ('EmployeeCode', 'KT001'), ['/Accounting/Dashboard']),
    'QL001': ('/Account/EmployeeLogin', ('EmployeeCode', 'QL001'), ['/Admin/Dashboard', '/Staff/ChangeApprovals', '/Dev/UiKit',
                                                                     '/Dev/UiKit?errors=true', '/Dev/SimulateBank']),
    'AD001': ('/Account/EmployeeLogin', ('EmployeeCode', 'AD001'), ['/Admin/Dashboard']),
}
SKIP = re.compile(r'/Account/Logout|/Language/|/Bookings/Receipt|/uploads/|\.(css|js|png|jpe?g|webp|svg|ico|pdf)$', re.I)
MAX_PAGES = 120


def opener():
    jar = http.cookiejar.CookieJar()
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))


def fetch(op, path, data=None):
    req = urllib.request.Request(BASE + path, data=urllib.parse.urlencode(data).encode() if data else None)
    try:
        with op.open(req, timeout=60) as r:
            return r.status, r.geturl(), r.read().decode('utf-8', 'replace')
    except urllib.error.HTTPError as e:
        return e.code, BASE + path, e.read().decode('utf-8', 'replace')


def login(op, path, field):
    _, _, html = fetch(op, path)
    token = re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', html).group(1)
    status, url, _ = fetch(op, path, {field[0]: field[1], 'Password': PASSWORD, '__RequestVerificationToken': token})
    return '/Account/' not in url


problems, total = [], 0
for role, (login_path, field, starts) in ROLES.items():
    op = opener()
    if login_path and not login(op, login_path, field):
        problems.append((role, login_path, 'login failed')); continue
    seen, queue, pages = set(), list(starts), 0
    while queue and pages < MAX_PAGES:
        path = queue.pop(0)
        key = re.sub(r'([?&](page|pageIndex)=\d+)', '', path)
        if key in seen or SKIP.search(path): continue
        seen.add(key); pages += 1
        status, url, html = fetch(op, path)
        expected_error = path.startswith('/Home/StatusCode/') or path == '/Account/AccessDenied'
        if status >= 500 or (status >= 400 and not expected_error):
            problems.append((role, path, status))
        for href in re.findall(r'href="([^"#]+)"', html):
            href = unescape(href)
            if href.startswith(BASE): href = href[len(BASE):]
            if href.startswith('/') and not href.startswith('//'): queue.append(href)
    total += pages
    print(f'{role}: {pages} trang', flush=True)

print(f'Tổng {total} trang; lỗi: {len(problems)}')
for p in problems: print('  ', *p)
