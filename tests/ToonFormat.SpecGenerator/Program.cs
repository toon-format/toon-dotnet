using CommandLine;
using Microsoft.Extensions.Logging;

namespace ToonFormat.SpecGenerator;


public static class Program
{
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<SpecGeneratorOptions>(args)
            .MapResult((opts) =>
            {
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.SetMinimumLevel(Enum.TryParse<LogLevel>(opts.LogLevel, true, out var level) ? level : LogLevel.Information);
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = false;
                        options.SingleLine = true;
                    });
                });

                var logger = loggerFactory.CreateLogger<SpecGenerator>();

                var specGenerator = new SpecGenerator(logger);

                specGenerator.GenerateSpecs(opts);

                return 0;
            }, HandleParseError);
    }

    static int HandleParseError(IEnumerable<Error> errs)
    {
        var result = -2;
        if (errs.Any(x => x is HelpRequestedError || x is VersionRequestedError))
            result = -1;
        return result;
    }
}
