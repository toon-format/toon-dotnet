using Microsoft.Extensions.Logging;

namespace Toon.Format.SpecGenerator.Util;

internal static class GitTool
{
    public static void CloneRepository(string repositoryUrl, string destinationPath,
        string? branch = null, int? depth = null, ILogger? logger = null)
    {
        var depthArg = depth.HasValue ? $"--depth {depth.Value}" : string.Empty;
        var branchArg = branch is not null ? $"--branch {branch}" : string.Empty;

        using var process = new System.Diagnostics.Process();

        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = $"clone {branchArg} {depthArg} {repositoryUrl} {destinationPath}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        logger?.LogDebug("Executing git with arguments: {Arguments}", process.StartInfo.Arguments);

        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var errorMessage = $"Git clone failed with exit code {process.ExitCode}";
            if (!string.IsNullOrWhiteSpace(error))
            {
                errorMessage += $": {error}";
            }
            logger?.LogError("{ErrorMessage}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        logger?.LogDebug("Git clone output: {Output}", output);
    }
}