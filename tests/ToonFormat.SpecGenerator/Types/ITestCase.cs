namespace ToonFormat.SpecGenerator.Types;

public interface ITestCase<TInput, TExpected>
{
    public string Name { get; init; }
    public TInput Input { get; init; }
    public TExpected Expected { get; init; }
    public bool ShouldError { get; init; }
    public string? SpecSection { get; init; }
    public string? Note { get; init; }
    public string? MinSpecVersion { get; init; }
}

