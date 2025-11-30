using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cognition.Clients.Tools.Sandbox;

/// <summary>
/// Minimal out-of-process runner. On Windows, assigns a Job Object; on Unix, spawns a process with no extra namespaces (stub).
/// This is a pre-alpha OOPS lane placeholder.
/// </summary>
public sealed class ProcessSandboxWorker : IToolSandboxWorker
{
    public async Task<ToolSandboxResult> ExecuteAsync(ToolSandboxWorkRequest request, CancellationToken ct)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = BuildStartInfo();
            if (!process.Start())
            {
                return new ToolSandboxResult(false, null, "Failed to start sandbox process.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsJobObject.Assign(process);
            }

            using var registration = ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(true); } catch { }
            });

            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            var success = process.ExitCode == 0;
            return new ToolSandboxResult(success, new { exitCode = process.ExitCode }, success ? null : "Sandboxed process failed.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ToolSandboxResult(false, null, $"Sandbox worker error: {ex.Message}");
        }
    }

    private static ProcessStartInfo BuildStartInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c timeout /t 1 >nul",
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        return new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = "-c \"sleep 1\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    private static class WindowsJobObject
    {
        private const int JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public long Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        private enum JobObjectInfoType
        {
            ExtendedLimitInformation = 9
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? name);

        [DllImport("kernel32.dll")]
        private static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        public static void Assign(Process process)
        {
            try
            {
                var handle = CreateJobObject(IntPtr.Zero, null);
                if (handle == IntPtr.Zero) return;

                var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
                {
                    BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                    {
                        LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                    }
                };

                var length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                var ptr = Marshal.AllocHGlobal(length);
                Marshal.StructureToPtr(info, ptr, false);
                SetInformationJobObject(handle, JobObjectInfoType.ExtendedLimitInformation, ptr, (uint)length);
                Marshal.FreeHGlobal(ptr);

                AssignProcessToJobObject(handle, process.Handle);
            }
            catch
            {
                // best-effort
            }
        }
    }
}
