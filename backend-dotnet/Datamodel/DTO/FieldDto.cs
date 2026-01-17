namespace Cinemadle.Datamodel.DTO;

public class HintsDto
{
    /// <summary>
    /// For numeric ranges (year, box office): the minimum known value
    /// </summary>
    public string? Min { get; set; }

    /// <summary>
    /// For numeric ranges (year, box office): the maximum known value
    /// </summary>
    public string? Max { get; set; }

    /// <summary>
    /// For list-based fields (genre, cast, creatives): known matching values
    /// </summary>
    public List<string>? KnownValues { get; set; }

    /// <summary>
    /// For rating: list of possible ratings that haven't been eliminated
    /// </summary>
    public List<string>? PossibleValues { get; set; }
}

public class FieldDto
{
    public required string Color { get; set; }
    public required int Direction { get; set; }
    public required IEnumerable<string> Values { get; set; }
    public required Dictionary<string, List<string>> Modifiers { get; set; }
    public HintsDto? Hints { get; set; }
}
