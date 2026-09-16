namespace FileProcessing.Api.Models;

public sealed class RecordFilterQuery
{
    public bool? Active { get; init; }

    public ActiveComparison? ActiveComparison { get; init; }

    public string? Name { get; init; }

    public NameComparison? NameComparison { get; init; }

    public double? Value { get; init; }

    public ValueComparison? ValueComparison { get; init; }
}

public enum ActiveComparison
{
    Equal,
    NotEqual
}

public enum NameComparison
{
    Equal,
    Contains,
    StartsWith
}

public enum ValueComparison
{
    Equal,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}
