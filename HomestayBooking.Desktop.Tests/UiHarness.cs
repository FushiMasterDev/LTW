using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using HomestayBooking.Desktop.Models;

namespace HomestayBooking.Desktop.Tests;

/// <summary>Chạy form WinForms thật trên luồng STA, nối vào DesktopHost thật ở FUJI_API_URL.</summary>
internal static class Ui
{
    public static string ApiUrl => Environment.GetEnvironmentVariable("FUJI_API_URL") ?? "http://127.0.0.1:5080";

    public static void Run(Action body, [System.Runtime.CompilerServices.CallerMemberName] string test = "")
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            // Như Application.Run: continuation của handler async quay về luồng giao diện.
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            using var dialogs = new DialogWatcher();
            try
            {
                Properties.Settings.Default.ApiBaseUrl = ApiUrl;
                body();
            }
            catch (Exception ex) { error = ex; }
            finally
            {
                foreach (var f in Application.OpenForms.Cast<Form>().ToList())
                {
                    try { Snapshot(f, test); f.Dispose(); }
                    catch (Exception ex) { Console.WriteLine($"[{test}] không đóng được {f.GetType().Name}: {ex.Message}"); }
                }
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        // Một bước chờ quá 25 giây mà luồng giao diện không trả lại (vòng lặp modal lạ): chụp màn hình lúc đang kẹt.
        var stuck = new Thread(() =>
        {
            var shot = false;
            while (thread.IsAlive && !shot)
            {
                Thread.Sleep(1000);
                if (Volatile.Read(ref _stepStarted) is var t && t > 0 && Environment.TickCount64 - t > 25000)
                { ScreenSnapshot($"{test}-KET-{LastStep}"); shot = true; }
            }
        }) { IsBackground = true };
        stuck.Start();
        // Có gì chặn luồng giao diện (hộp thoại lạ, vòng lặp modal) thì chụp toàn màn hình rồi báo lỗi, không treo cả lượt chạy.
        if (!thread.Join(TimeSpan.FromMinutes(2)))
        {
            ScreenSnapshot(test);
            throw new TimeoutException($"{test} treo quá 2 phút. Bước cuối: {LastStep}. Hộp thoại: {string.Join(" | ", DialogWatcher.Seen)}");
        }
        if (error is not null) ExceptionDispatchInfo.Throw(error);
    }

    /// <summary>Ảnh chụp form lúc kết thúc test, để xem giao diện thật trên Windows (thư mục SCREENSHOT_DIR).</summary>
    private static void Snapshot(Form form, string test)
    {
        var dir = Environment.GetEnvironmentVariable("SCREENSHOT_DIR");
        if (dir is null || !form.Visible || form.Width <= 0 || form.Height <= 0) return;
        Directory.CreateDirectory(dir);
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(Path.Combine(dir, $"{test}-{form.GetType().Name}-{Environment.TickCount64}.png"));
    }

    public static string LastStep { get; private set; } = "";
    private static long _stepStarted;

    private static void ScreenSnapshot(string test)
    {
        var dir = Environment.GetEnvironmentVariable("SCREENSHOT_DIR");
        if (dir is null) return;
        Directory.CreateDirectory(dir);
        var bounds = Screen.PrimaryScreen!.Bounds;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        var name = string.Concat($"{test}-man-hinh".Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        bitmap.Save(Path.Combine(dir, name + ".png"));
        File.WriteAllLines(Path.Combine(dir, name + ".txt"), DialogWatcher.DescribeWindows());
    }

    /// <summary>Bơm message loop để các handler async của form chạy tiếp, tới khi điều kiện đúng.</summary>
    public static void Until(Func<bool> condition, string what, int timeoutMs = 20000)
    {
        LastStep = what;
        Console.WriteLine($"  … {what}");
        var watch = Stopwatch.StartNew();
        Volatile.Write(ref _stepStarted, Environment.TickCount64);
        while (!condition())
        {
            if (watch.ElapsedMilliseconds > timeoutMs)
                throw new TimeoutException($"Hết {timeoutMs} ms vẫn chưa thấy: {what}. Hộp thoại đã hiện: {string.Join(" | ", DialogWatcher.Seen)}");
            Application.DoEvents();
            Thread.Sleep(20);
        }
        Volatile.Write(ref _stepStarted, 0);
    }

    public static void Settle(int ms = 500)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < ms) { Application.DoEvents(); Thread.Sleep(20); }
    }

    public static int ApiTotal(string path, EmployeeSession session)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        var page = http.GetFromJsonAsync<System.Text.Json.JsonElement>($"{ApiUrl}/api/desktop/{path}").GetAwaiter().GetResult();
        return page.GetProperty("totalCount").GetInt32();
    }

    public static T Field<T>(object form, string name) =>
        (T)(form.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(form)
            ?? throw new MissingFieldException(form.GetType().Name, name));

    /// <summary>Bấm như người dùng: nút phải đang bật.</summary>
    public static void Click(Control control)
    {
        Assert.True(control.Enabled, $"{control.Name} đang bị tắt");
        LastStep = $"bấm {control.Name} ({control.Text})";
        Console.WriteLine($"  … {LastStep}");
        typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, [EventArgs.Empty]);
    }

    public static EmployeeSession Login(string code)
    {
        using var http = new HttpClient();
        var response = http.PostAsJsonAsync($"{ApiUrl}/api/desktop/auth/login", new { employeeCode = code, password = "Fuji@2026" }).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var login = response.Content.ReadFromJsonAsync<View.LoginResponse>().GetAwaiter().GetResult()!;
        var session = new EmployeeSession(login.Employee.Code, login.Employee.FullName ?? "", login.Employee.Position, "", login.AccessToken, login.ExpiresAt);
        AppSession.Current = session;
        return session;
    }

    public static T Show<T>(T form) where T : Form
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(0, 0);
        form.Show();
        return form;
    }
}

/// <summary>Tự bấm Yes/OK ở MessageBox của tiến trình test và ghi lại nội dung để kiểm.</summary>
internal sealed class DialogWatcher : IDisposable
{
    public static readonly ConcurrentQueue<string> Seen = new();
    private readonly CancellationTokenSource _stop = new();

    public DialogWatcher()
    {
        Seen.Clear();
        var pid = (uint)Environment.ProcessId;
        new Thread(() =>
        {
            while (!_stop.IsCancellationRequested)
            {
                EnumWindows((hwnd, _) =>
                {
                    GetWindowThreadProcessId(hwnd, out var owner);
                    if (owner != pid || !IsWindowVisible(hwnd) || ClassName(hwnd) != "#32770") return true;
                    Seen.Enqueue(Text(GetDlgItem(hwnd, 0xFFFF)));
                    var button = GetDlgItem(hwnd, 6); // IDYES
                    if (button == IntPtr.Zero) button = GetDlgItem(hwnd, 1); // IDOK
                    if (button != IntPtr.Zero) PostMessage(button, 0x00F5, IntPtr.Zero, IntPtr.Zero); // BM_CLICK
                    else PostMessage(hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
                    return true;
                }, IntPtr.Zero);
                Thread.Sleep(150);
            }
        }) { IsBackground = true }.Start();
    }

    public static IReadOnlyList<string> Messages => Seen.Distinct().ToList();

    /// <summary>Các cửa sổ đang hiện của tiến trình test (lớp, tiêu đề, kích thước) để biết cái gì đang kẹt.</summary>
    public static List<string> DescribeWindows()
    {
        var pid = (uint)Environment.ProcessId;
        var rows = new List<string>();
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out var owner);
            if (owner == pid && IsWindowVisible(hwnd) && GetWindowRect(hwnd, out var r))
                rows.Add($"{ClassName(hwnd)} | {Text(hwnd)} | {r.Left},{r.Top} {r.Right - r.Left}x{r.Bottom - r.Top}");
            return true;
        }, IntPtr.Zero);
        return rows;
    }

    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

    public void Dispose() => _stop.Cancel();

    private static string ClassName(IntPtr hwnd) { var sb = new StringBuilder(64); GetClassName(hwnd, sb, sb.Capacity); return sb.ToString(); }
    private static string Text(IntPtr hwnd) { var sb = new StringBuilder(1024); GetWindowText(hwnd, sb, sb.Capacity); return sb.ToString(); }

    private delegate bool EnumProc(IntPtr hwnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);
    [DllImport("user32.dll")] private static extern IntPtr GetDlgItem(IntPtr hwnd, int id);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
}
