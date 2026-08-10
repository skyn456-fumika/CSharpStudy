using GameServerManager.App.Models;
using System.Diagnostics;
using System.IO;

namespace GameServerManager.App.Services;

public class ServerProcessService : IDisposable
{
    private readonly Dictionary<Guid, Process> _processes = [];
    private readonly Dictionary<Guid, TimeSpan> _previousCpuTimes = [];
    private readonly Dictionary<Guid, DateTime> _previousCpuCheckedAt = [];
    public event EventHandler<ServerProcessExitedEventArgs>? ProcessExited;
    public event EventHandler<ServerLogEventArgs>? LogReceived;

    public Process Start(GameServerModel server)
    {
        if (_processes.TryGetValue(server.Id, out var existingProcess))
        {
            if (!existingProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"{server.ServerName} 서버는 이미 실행 중입니다.");
            }

            RemoveProcess(server.Id);
        }

        if (string.IsNullOrWhiteSpace(server.ExecutablePath))
        {
            throw new InvalidOperationException(
                "실행 파일 경로가 비어 있습니다.");
        }

        if (!File.Exists(server.ExecutablePath))
        {
            throw new FileNotFoundException(
                "서버 실행 파일을 찾을 수 없습니다.",
                server.ExecutablePath);
        }

        var workingDirectory = server.WorkingDirectory;

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            workingDirectory =
                Path.GetDirectoryName(server.ExecutablePath)
                ?? AppContext.BaseDirectory;
        }

        if (!Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException(
                $"작업 폴더를 찾을 수 없습니다: {workingDirectory}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = server.ExecutablePath,
            Arguments = server.Arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true
        };

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            LogReceived?.Invoke(
                this,
                new ServerLogEventArgs
                {
                    ServerId = server.Id,
                    ServerName = server.ServerName,
                    Level = "INFO",
                    Message = e.Data,
                    CreatedAt = DateTime.Now
                });
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            LogReceived?.Invoke(
                this,
                new ServerLogEventArgs
                {
                    ServerId = server.Id,
                    ServerName = server.ServerName,
                    Level = "ERROR",
                    Message = e.Data,
                    CreatedAt = DateTime.Now
                });
        };

        if (!process.Start())
        {
            process.Dispose();

            throw new InvalidOperationException(
                $"{server.ServerName} 서버를 시작하지 못했습니다.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.Exited += (_, _) =>
        {
            var exitCode = 0;

            try
            {
                exitCode = process.ExitCode;
            }
            catch
            {
                // 종료 코드를 읽을 수 없는 경우 기본값 사용
            }

            ProcessExited?.Invoke(
                this,
                new ServerProcessExitedEventArgs
                {
                    ServerId = server.Id,
                    ExitCode = exitCode,
                    ExitedAt = DateTime.Now
                });
        };

        _processes[server.Id] = process;

        return process;
    }

    public async Task StopAsync(
        GameServerModel server,
        TimeSpan gracefulTimeout)
    {
        if (!_processes.TryGetValue(server.Id, out var process))
        {
            TryStopByProcessId(server.ProcessId);
            return;
        }

        try
        {
            if (process.HasExited)
            {
                return;
            }

            var closeRequested = false;

            try
            {
                closeRequested = process.CloseMainWindow();
            }
            catch (InvalidOperationException)
            {
                closeRequested = false;
            }

            if (closeRequested)
            {
                using var cancellationTokenSource =
                    new CancellationTokenSource(gracefulTimeout);

                try
                {
                    await process.WaitForExitAsync(
                        cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // 정상 종료 제한 시간을 초과하면 아래에서 강제 종료한다.
                }
            }

            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        finally
        {
            RemoveProcess(server.Id);
        }
    }

    public bool IsRunning(GameServerModel server)
    {
        if (!_processes.TryGetValue(server.Id, out var process))
        {
            return false;
        }

        try
        {
            return !process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static void TryStopByProcessId(int? processId)
    {
        if (processId is null)
        {
            return;
        }

        try
        {
            using var process =
                Process.GetProcessById(processId.Value);

            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
        }
        catch (ArgumentException)
        {
            // 이미 종료된 프로세스다.
        }
        catch (InvalidOperationException)
        {
            // 종료 중이거나 프로세스 정보를 읽을 수 없는 상태다.
        }
    }

    private void RemoveProcess(Guid serverId)
    {
        _previousCpuTimes.Remove(serverId);
        _previousCpuCheckedAt.Remove(serverId);

        if (!_processes.Remove(serverId, out var process))
        {
            return;
        }

        process.Dispose();
    }

    public void Dispose()
    {
        _previousCpuTimes.Clear();
        _previousCpuCheckedAt.Clear();

        foreach (var process in _processes.Values)
        {
            process.Dispose();
        }

        _processes.Clear();
    }

    public async Task SendCommandAsync(
        GameServerModel server,
        string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException(
                "전송할 명령이 비어 있습니다.",
                nameof(command));
        }

        if (!_processes.TryGetValue(
                server.Id,
                out var process))
        {
            throw new InvalidOperationException(
                $"{server.ServerName} 서버 프로세스를 찾을 수 없습니다.");
        }

        if (process.HasExited)
        {
            throw new InvalidOperationException(
                $"{server.ServerName} 서버는 실행 중이 아닙니다.");
        }

        await process.StandardInput.WriteLineAsync(
            command.Trim());

        await process.StandardInput.FlushAsync();
    }

    public ServerResourceSnapshot? GetResourceSnapshot(
        GameServerModel server)
    {
        if (!_processes.TryGetValue(
                server.Id,
                out var process))
        {
            return null;
        }

        try
        {
            if (process.HasExited)
            {
                return null;
            }

            process.Refresh();

            var now = DateTime.UtcNow;
            var totalCpuTime = process.TotalProcessorTime;

            var cpuUsage = 0d;

            if (_previousCpuTimes.TryGetValue(
                    server.Id,
                    out var previousCpuTime) &&
                _previousCpuCheckedAt.TryGetValue(
                    server.Id,
                    out var previousCheckedAt))
            {
                var cpuTimeDifference =
                    totalCpuTime - previousCpuTime;

                var elapsed =
                    now - previousCheckedAt;

                if (elapsed.TotalMilliseconds > 0)
                {
                    cpuUsage =
                        cpuTimeDifference.TotalMilliseconds /
                        (elapsed.TotalMilliseconds *
                         Environment.ProcessorCount) *
                        100d;

                    cpuUsage =
                        Math.Clamp(cpuUsage, 0d, 100d);
                }
            }

            _previousCpuTimes[server.Id] =
                totalCpuTime;

            _previousCpuCheckedAt[server.Id] =
                now;

            var uptime =
                DateTime.Now - process.StartTime;

            return new ServerResourceSnapshot
            {
                CpuUsagePercent = cpuUsage,
                MemoryUsageBytes =
                    process.WorkingSet64,
                Uptime = uptime
            };
        }
        catch
        {
            return null;
        }
    }
}