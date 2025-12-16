using Microsoft.Extensions.Logging;

namespace ToonFormat.SpecGenerator.Util;

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

        process.WaitForExit();
    }
}