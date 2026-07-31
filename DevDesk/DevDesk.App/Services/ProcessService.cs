using System.Diagnostics;
using DevDesk.App.Models;

namespace DevDesk.App.Services;

public class ProcessService
{
    public List<ProcessInfoModel> GetProcesses()
    {
        var processes = new List<ProcessInfoModel>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                processes.Add(new ProcessInfoModel
                {
                    Id = process.Id,
                    Name = process.ProcessName,
                    MemoryMb = Math.Round(process.WorkingSet64 / 1024d / 1024d, 1),
                    StartTime = GetStartTime(process)
                });
            }
            catch
            {
                // 종료 중이거나 접근 권한이 없는 프로세스는 제외한다.
            }
            finally
            {
                process.Dispose();
            }
        }

        return processes
            .OrderBy(process => process.Name)
            .ThenBy(process => process.Id)
            .ToList();
    }

    public void StartProcess(string filePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    public void KillProcess(int processId)
    {
        using var process = Process.GetProcessById(processId);
        process.Kill(true);
        process.WaitForExit(3000);
    }

    private static string GetStartTime(Process process)
    {
        try
        {
            return process.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch
        {
            return "-";
        }
    }
}