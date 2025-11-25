using CommandLine;

namespace ToonFormat.SpecGenerator;

public class SpecGeneratorOptions
{
    /// <summary>
    /// The git repo of the toon spec
    /// </summary>
    [Option('u', "url", Required = true, HelpText = "The git repo of the toon spec")]
    public required string SpecRepoUrl { get; set; }

    /// <summary>
    /// The relative path of the output directory
    /// </summary>
    [Option('o', "output", Required = true, HelpText = "The relative path of the output directory")]
    public required string OutputPath { get; set; }

    /// <summary>
    /// The relative path to a .specignore file, to explicitly ignore generating tests based on a test name
    /// </summary>
    [Option('i', "ignore", Required = false, HelpText = "The relative path to a .specignore file, to explicitly ignore generating tests based on a test name")]
    public string? IgnorePath { get; set; }

    /// <summary>
    /// The branch version or tag name to reference
    /// </summary>
    [Option('b', "branch", Required = false, HelpText = "The branch version or tag name to reference")]
    public string? Branch { get; set; }

    /// <summary>
    /// The log level
    /// </summary>
    [Option('l', "loglevel", Required = false, HelpText = "The log level")]
    public string? LogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Information.ToString();

    internal string AbsoluteOutputPath => Path.GetFullPath(
                        Path.Combine(Environment.GetEnvironmentVariable("PWD") ?? Environment.CurrentDirectory, OutputPath));

    internal string AbsoluteSpecIgnorePath => Path.GetFullPath(
                        Path.Combine(Environment.GetEnvironmentVariable("PWD") ?? Environment.CurrentDirectory, IgnorePath ?? ""));
}
