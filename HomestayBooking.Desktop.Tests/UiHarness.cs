using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using HomestayBooking.Desktop.Models;

namespace HomestayBooking.Desktop.Tests;

/// <summary>Chạy form thật trong worker STA có Application.Run, nối DesktopHost ở FUJI_API_URL.</summary>
internal static class Ui
{
    public static string ApiUrl => Environment.GetEnvironmentVariable("FUJI_API_URL") ?? "http://127.0.0.1:5080";

    public static void Run(Action body, [System.Runtime.CompilerServices.CallerMemberName] string test = "", object[]? arguments = null)
    {
        if (!UiWorker.IsWorker)
        {
            UiWorker.Run(test, arguments);
            return;
        }

        Exception? error = null;
        using var dialogs = new DialogWatcher();
        using var stop = new ManualResetEvent(false);
        var stuck = new Thread(() =>
        {
            while (!stop.WaitOne(1000))
            {
                if (Volatile.Read(ref _stepStarted) is var t && t > 0 && Environment.TickCount64 - t > 25000)
                {
                    ScreenSnapshot($"{test}-KET-{LastStep}", Environment.ProcessId);
                    return;
                }
            }
        }) { IsBackground = true, Name = "UI watchdog" };
        stuck.Start();

        using var loop = new ApplicationContext();
        using var dispatcher = new Control();
        dispatcher.CreateControl();
        dispatcher.BeginInvoke((Action)(() =>
        {
            try
            {
                Step("bắt đầu thân test");
                Assert.True(Application.MessageLoop);
                Assert.IsType<WindowsFormsSynchronizationContext>(SynchronizationContext.Current);
                Properties.Settings.Default.ApiBaseUrl = ApiUrl;
                body();
            }
            catch (Exception ex) { error = ex; }
            finally
            {
                Console.WriteLine($"[{test}] thân test: {(error is null ? "PASS" : error.ToString())}");
                // Worker chỉ có một UI thread; cross-thread access là lỗi, không bỏ qua form đó.
                var forms = Application.OpenForms.Cast<Form>().ToList();
                foreach (var form in forms)
                {
                    try
                    {
                        Step($"chụp {form.GetType().Name}");
                        Snapshot(form, test);
                    }
                    catch (Exception ex) { Console.WriteLine($"[{test}] không chụp được form: {ex.Message}"); }
                    try
                    {
                        Step($"Close {form.GetType().Name}");
                        form.Close();
                    }
                    catch (Exception ex) { error ??= ex; }
                }

                // Trả quyền cho message loop sau Close trước khi Dispose form còn lại và thoát loop.
                dispatcher.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        Step("dọn form còn lại");
                        foreach (var form in forms.Where(f => !f.IsDisposed)) form.Dispose();
                        Assert.Empty(Application.OpenForms.Cast<Form>());
                    }
                    catch (Exception ex) { error ??= ex; }
                    finally { loop.ExitThread(); }
                }));
            }
        }));
        try
        {
            // DoEvents/ShowDialog trong thân test luôn là loop lồng nhau; không gỡ UI context giữa các bước.
            Application.Run(loop);
        }
        finally
        {
            stop.Set();
            stuck.Join();
        }
        if (error is not null) ExceptionDispatchInfo.Throw(error);
    }

    /// <summary>Ảnh chụp form lúc kết thúc test, để xem giao diện thật trên Windows (thư mục SCREENSHOT_DIR).</summary>
    private static void Snapshot(Form form, string test)
    {
        var dir = Environment.GetEnvironmentVariable("SCREENSHOT_DIR");
        if (dir is null || !form.Visible || form.Width <= 0 || form.Height <= 0) return;
        Directory.CreateDirectory(dir);
        // Đọc pixel màn hình, không gửi WM_PRINT đệ quy vào control native/Guna2.
        var bounds = Rectangle.Intersect(form.RectangleToScreen(form.ClientRectangle), SystemInformation.VirtualScreen);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        bitmap.Save(Path.Combine(dir, $"{test}-{form.GetType().Name}-{Environment.TickCount64}.png"));
    }

    private static string _lastStep = "";
    public static string LastStep => Volatile.Read(ref _lastStep);
    private static long _stepStarted;

    private static void Step(string what)
    {
        Volatile.Write(ref _lastStep, what);
        Volatile.Write(ref _stepStarted, Environment.TickCount64);
        Console.WriteLine($"  … {what} (PID={Environment.ProcessId}, TID=0x{DialogWatcher.CurrentThreadId:X}, {DateTime.UtcNow:HH:mm:ss})");
        if (UiWorker.IsWorker)
        {
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
            Assert.IsType<WindowsFormsSynchronizationContext>(SynchronizationContext.Current);
        }
    }

    internal static void ScreenSnapshot(string test, int processId)
    {
        var dir = Environment.GetEnvironmentVariable("SCREENSHOT_DIR");
        if (dir is null) return;
        var name = string.Concat($"{test}-{processId}-man-hinh".Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        try
        {
            Directory.CreateDirectory(dir);
            var bounds = SystemInformation.VirtualScreen;
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            bitmap.Save(Path.Combine(dir, name + ".png"));
            File.WriteAllLines(Path.Combine(dir, name + ".txt"), DialogWatcher.DescribeWindows(processId));
        }
        catch (Exception ex) { Console.WriteLine($"Không chụp được {name}: {ex}"); }
        // Stack managed của mọi luồng lúc đang kẹt (cần dotnet-stack trên PATH, CI cài sẵn).
        try
        {
            using var stack = Process.Start(new ProcessStartInfo("dotnet-stack", $"report --process-id {processId}")
                { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true })!;
            var stdout = stack.StandardOutput.ReadToEndAsync();
            var stderr = stack.StandardError.ReadToEndAsync();
            if (!stack.WaitForExit(10000))
            {
                stack.Kill(entireProcessTree: true);
                if (!stack.WaitForExit(5000)) throw new TimeoutException("dotnet-stack không thoát sau Kill.");
            }
            var output = Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            File.WriteAllText(Path.Combine(dir, name + "-stack.txt"), string.Join("\n", output));
        }
        catch (Exception ex) { Console.WriteLine($"Không lấy được stack {name}: {ex}"); }
    }

    /// <summary>Bơm message loop để các handler async của form chạy tiếp, tới khi điều kiện đúng.</summary>
    public static void Until(Func<bool> condition, string what, int timeoutMs = 20000)
    {
        Step(what);
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.ElapsedMilliseconds > timeoutMs)
                throw new TimeoutException($"Hết {timeoutMs} ms vẫn chưa thấy: {what}. Hộp thoại đã hiện: {string.Join(" | ", DialogWatcher.Seen)}");
            var pump = Stopwatch.StartNew();
            Application.DoEvents();
            if (pump.ElapsedMilliseconds > 2000) Console.WriteLine($"  !! DoEvents mất {pump.ElapsedMilliseconds} ms lúc chờ: {what} ({DateTime.UtcNow:HH:mm:ss})");
            Thread.Sleep(20);
        }
        Volatile.Write(ref _stepStarted, 0);
    }

    public static void Settle(int ms = 500)
    {
        Step($"bơm giao diện {ms} ms");
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < ms) { Application.DoEvents(); Thread.Sleep(20); }
        Volatile.Write(ref _stepStarted, 0);
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
        Step($"bấm {control.Name} ({control.Text})");
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
    private readonly ManualResetEvent _stop = new(false);
    private readonly Thread _thread;
    internal static uint CurrentThreadId => GetCurrentThreadId();

    public DialogWatcher()
    {
        Seen.Clear();
        var pid = (uint)Environment.ProcessId;
        _thread = new Thread(() =>
        {
            while (!_stop.WaitOne(150))
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
            }
        }) { IsBackground = true, Name = "MessageBox watcher" };
        _thread.Start();
    }

    public static IReadOnlyList<string> Messages => Seen.Distinct().ToList();

    /// <summary>Các cửa sổ đang hiện của tiến trình test (lớp, tiêu đề, kích thước) để biết cái gì đang kẹt.</summary>
    public static List<string> DescribeWindows(int processId)
    {
        var pid = (uint)processId;
        var rows = new List<string>();
        EnumWindows((hwnd, ignored) =>
        {
            var tid = GetWindowThreadProcessId(hwnd, out var owner);
            if (owner != pid) return true;
            Add(hwnd, tid, Text(hwnd));
            EnumChildWindows(hwnd, (child, unused) =>
            {
                var childTid = GetWindowThreadProcessId(child, out _);
                Add(child, childTid, "");
                return true;
            }, IntPtr.Zero);
            return true;
        }, IntPtr.Zero);
        return rows;

        void Add(IntPtr hwnd, uint tid, string text)
        {
            GetWindowRect(hwnd, out var r);
            rows.Add($"PID={pid} TID=0x{tid:X} HWND=0x{hwnd:X} parent=0x{GetParent(hwnd):X} visible={IsWindowVisible(hwnd)} | {ClassName(hwnd)} | {text} | {r.Left},{r.Top} {r.Right - r.Left}x{r.Bottom - r.Top}");
        }
    }

    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

    public void Dispose()
    {
        _stop.Set();
        _thread.Join();
        _stop.Dispose();
    }

    private static string ClassName(IntPtr hwnd) { var sb = new StringBuilder(64); GetClassName(hwnd, sb, sb.Capacity); return sb.ToString(); }
    private static string Text(IntPtr hwnd)
    {
        // GetWindowText cùng tiến trình gửi WM_GETTEXT đồng bộ; watcher không được chờ UI vô hạn.
        var sb = new StringBuilder(1024);
        SendMessageTimeout(hwnd, 0x000D, (IntPtr)sb.Capacity, sb, 0x0003, 200, out _); // WM_GETTEXT, BLOCK | ABORTIFHUNG
        return sb.ToString();
    }

    private delegate bool EnumProc(IntPtr hwnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, StringBuilder text, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] private static extern IntPtr GetDlgItem(IntPtr hwnd, int id);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
}
