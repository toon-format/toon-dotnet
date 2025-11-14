using CommandLine;

namespace ToonFormat.SpecGenerator;


public static class Program
{
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<SpecGeneratorOptions>(args)
            .MapResult((opts) =>
            {
                SpecGenerator.GenerateSpecs(opts);

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
