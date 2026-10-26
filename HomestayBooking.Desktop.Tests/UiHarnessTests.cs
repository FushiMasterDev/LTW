using System.Diagnostics;

namespace HomestayBooking.Desktop.Tests;

public sealed class UiHarnessTests
{
    [Fact]
    public void MessageLoopsKeepAsyncContinuationsOnSta() => Ui.Run(() =>
    {
        var thread = Environment.CurrentManagedThreadId;
        var context = Assert.IsType<WindowsFormsSynchronizationContext>(SynchronizationContext.Current);
        var form = Ui.Show(new Form());
        Ui.Settle(100);
        Assert.True(Application.MessageLoop);
        Assert.Same(context, SynchronizationContext.Current);

        // Ngoài Application.Run, DoEvents/ShowDialog có thể gỡ context khi loop ngoài cùng kết thúc.
        using (var modal = new Form())
        using (var timer = new System.Windows.Forms.Timer { Interval = 50 })
        {
            timer.Tick += (_, _) => { timer.Stop(); modal.Close(); };
            timer.Start();
            modal.ShowDialog(form);
        }
        Ui.Settle(100);
        Assert.Same(context, SynchronizationContext.Current);

        using var grid = new DataGridView { Dock = DockStyle.Fill };
        form.Controls.Add(grid);
        var continuation = BindAfterAwaitAsync();
        Ui.Until(() => continuation.IsCompleted, "continuation bind bảng quay lại STA");
        continuation.GetAwaiter().GetResult();
        Assert.Equal(thread, continuation.Result);
        Assert.Single(grid.Rows.Cast<DataGridViewRow>(), r => !r.IsNewRow);

        async Task<int> BindAfterAwaitAsync()
        {
            await Task.Delay(50);
            Assert.Same(context, SynchronizationContext.Current);
            grid.DataSource = new[] { new { Name = "STA" } };
            return Environment.CurrentManagedThreadId;
        }
    });

    [Fact]
    public void TimedOutWorkerExitsBeforeAnotherWorkerStarts()
    {
        // Cố ý kẹt UI có cửa sổ thật; testhost phải kill worker rồi vẫn chạy được worker kế tiếp.
        var error = Assert.Throws<TimeoutException>(() => UiWorker.Run(nameof(BlockWithWindow), timeout: TimeSpan.FromSeconds(10)));
        Assert.Contains("timeout-probe-ready", error.Message);
        var pid = UiWorker.LastProcessId;
        Assert.False(IsAlive(pid), $"worker PID={pid} vẫn sống");
        Assert.Empty(DialogWatcher.DescribeWindows(pid));
        UiWorker.Run(nameof(CheckFreshWorker));
    }

    // Probe được worker gọi bằng reflection, không phải test để VSTest chạy trực tiếp.
    internal static void BlockWithWindow() => Ui.Run(() =>
    {
        Ui.Show(new Form { Text = "timeout-probe" });
        Console.WriteLine("timeout-probe-ready");
        Thread.Sleep(Timeout.Infinite);
    });

    internal static void CheckFreshWorker() => Ui.Run(() =>
    {
        Assert.Empty(Application.OpenForms.Cast<Form>());
        Assert.Null(Models.AppSession.Current);
        Ui.Show(new Form());
        Ui.Settle(50);
    });

    private static bool IsAlive(int pid)
    {
        try { using var process = Process.GetProcessById(pid); return !process.HasExited; }
        catch (ArgumentException) { return false; }
    }
}
