namespace Cinemadle.Datamodel.DTO;

public record FeatureFlagDto
{
    public required string Name { get; init; }
    public required bool Value { get; init; }
}