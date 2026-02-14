using System.Diagnostics;
using System.Text;

namespace HomeCenterBackup;

public class DockerService
{
    public async Task<List<string>> GetRunningContainersAsync()
    {
        var result = await ExecuteDockerCommandAsync("ps --format \"{{.Names}}\"");
        return result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }

    public async Task<bool> IsContainerRunningAsync(string containerName)
    {
        var containers = await GetRunningContainersAsync();
        return containers.Contains(containerName);
    }

    public async Task<string> CopyFileFromContainerAsync(string containerName, string sourcePath, string destinationPath)
    {
        var command = $"cp {containerName}:{sourcePath} \"{destinationPath}\"";
        return await ExecuteDockerCommandAsync(command);
    }

    public async Task<string> CopyFileToContainerAsync(string containerName, string sourcePath, string destinationPath)
    {
        var command = $"cp \"{sourcePath}\" {containerName}:{destinationPath}";
        return await ExecuteDockerCommandAsync(command);
    }

    public async Task<string> StopContainerAsync(string containerName, string? workingDirectory = null)
    {
        return await ExecuteDockerComposeCommandAsync("stop", workingDirectory ?? Environment.CurrentDirectory);
    }

    public async Task<string> StartContainerAsync(string containerName, string? workingDirectory = null)
    {
        return await ExecuteDockerComposeCommandAsync("start", workingDirectory ?? Environment.CurrentDirectory);
    }

    public async Task<string> GetContainerLogsAsync(string containerName, int lines = 50)
    {
        var command = $"logs --tail {lines} {containerName}";
        return await ExecuteDockerCommandAsync(command);
    }

    public async Task<string> ExecuteDockerComposeCommandAsync(string arguments, string workingDirectory)
    {
        return await ExecuteCommandAsync("docker-compose", arguments, workingDirectory);
    }

    public async Task<string> ExecuteDockerCommandAsync(string arguments)
    {
        return await ExecuteCommandAsync("docker", arguments, null);
    }

    public async Task<long> GetFileSizeInContainerAsync(string containerName, string filePath)
    {
        try
        {
            var result = await ExecuteDockerCommandAsync($"exec {containerName} stat -c %s {filePath}");
            if (long.TryParse(result.Trim(), out var size))
            {
                return size;
            }
        }
        catch
        {
            // File doesn't exist or error
        }
        return 0;
    }

    private async Task<string> ExecuteCommandAsync(string command, string arguments, string? workingDirectory)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };

        using var process = new Process { StartInfo = processInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0 && error.Length > 0)
        {
            throw new Exception($"Command failed: {error}");
        }

        return output.ToString();
    }
}
