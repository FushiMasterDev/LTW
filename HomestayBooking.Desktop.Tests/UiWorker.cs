using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace HomestayBooking.Desktop.Tests;

/// <summary>Biên tiến trình: một STA bị kẹt trong native code không được sống sang ca tiếp theo.</summary>
internal static class UiWorker
{
    private static readonly object Gate = new();
    private static string? _isolationFailure;
    internal static bool IsWorker { get; private set; }
    internal static int LastProcessId { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length != 3 || args[0] != "--ui-worker")
                throw new ArgumentException("Worker cần --ui-worker, tên method và mảng tham số JSON.");
            IsWorker = true;
            Control.CheckForIllegalCrossThreadCalls = true;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            var method = FindMethod(args[1]);
            var values = JsonSerializer.Deserialize<JsonElement[]>(args[2])!;
            var parameters = method.GetParameters();
            if (values.Length != parameters.Length) throw new ArgumentException("Sai số tham số của ca giao diện.");
            var arguments = parameters.Select((p, i) => p.ParameterType == typeof(Type)
                ? Type.GetType(values[i].GetString()!, throwOnError: true)
                : JsonSerializer.Deserialize(values[i].GetRawText(), p.ParameterType)).ToArray();
            method.Invoke(method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType!), arguments);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex is TargetInvocationException { InnerException: { } inner } ? inner : ex);
            return 1;
        }
    }

    internal static void Run(string test, object[]? arguments = null, TimeSpan? timeout = null)
    {
        // Kể cả runner khác bật parallel, chỉ một worker được dùng desktop tại một thời điểm.
        lock (Gate)
        {
            if (_isolationFailure is not null) throw new InvalidOperationException(_isolationFailure);
            var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.ArgumentList.Add(typeof(UiWorker).Assembly.Location);
            start.ArgumentList.Add("--ui-worker");
            start.ArgumentList.Add(test);
            start.ArgumentList.Add(JsonSerializer.Serialize((arguments ?? []).Select(a => a is Type t ? t.AssemblyQualifiedName : a)));
            using var worker = Process.Start(start) ?? throw new InvalidOperationException("Không khởi động được UI worker.");
            LastProcessId = worker.Id;
            Console.WriteLine($"[{test}] worker PID={worker.Id}");
            var stdout = worker.StandardOutput.ReadToEndAsync();
            var stderr = worker.StandardError.ReadToEndAsync();
            var limit = timeout ?? TimeSpan.FromMinutes(2);
            var timedOut = false;
            try
            {
                timedOut = !worker.WaitForExit((int)limit.TotalMilliseconds);
                if (timedOut) Ui.ScreenSnapshot($"{test}-TIMEOUT", worker.Id);
            }
            finally
            {
                // Không Thread.Abort, không để một luồng STA/cửa sổ mồ côi trong testhost.
                try
                {
                    if (!worker.HasExited)
                    {
                        try { worker.Kill(entireProcessTree: true); }
                        catch (InvalidOperationException) when (worker.HasExited) { }
                        catch (System.ComponentModel.Win32Exception) when (worker.HasExited) { }
                    }
                    if (!worker.WaitForExit(10000)) throw new TimeoutException("Worker chưa thoát sau Kill.");
                }
                catch (Exception ex)
                {
                    _isolationFailure = $"Không thu hồi được UI worker PID={worker.Id}; dừng mở ca mới: {ex}";
                    throw new InvalidOperationException(_isolationFailure, ex);
                }
            }

            var output = Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            foreach (var text in output) Console.WriteLine(text);
            if (timedOut)
                throw new TimeoutException($"{test} treo quá {limit.TotalSeconds:N0} giây; worker PID={worker.Id} đã thoát.\n{string.Join("\n", output)}");
            Assert.True(worker.ExitCode == 0, $"{test}: worker PID={worker.Id}, exit={worker.ExitCode}.\n{string.Join("\n", output)}");
        }
    }

    private static MethodInfo FindMethod(string name) => typeof(UiWorker).Assembly.GetTypes()
        .Where(t => t.Namespace == typeof(UiWorker).Namespace)
        .SelectMany(t => t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        .Single(m => m.Name == name);
}
